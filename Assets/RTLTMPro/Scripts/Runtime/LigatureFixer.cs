using System.Globalization;
using System.Runtime.CompilerServices;

namespace RTLTMPro
{
    public static class LigatureFixer
    {
        private static bool TryGetMirroredCharacter(int character, out int mirrored)
        {
            switch (character)
            {
                case '(': mirrored = ')'; return true;
                case ')': mirrored = '('; return true;
                case '»': mirrored = '«'; return true;
                case '«': mirrored = '»'; return true;
                case '{': mirrored = '}'; return true;
                case '}': mirrored = '{'; return true;
                case '[': mirrored = ']'; return true;
                case ']': mirrored = '['; return true;
                default: mirrored = character; return false;
            }
        }

        // Nearly every character this fixer sees is ASCII, Arabic (U+0600..U+06FF) or an Arabic presentation form
        // (U+FB50..U+FEFF), so the punctuation/symbol answer for those ranges is precomputed from the same API.
        private const int LowRangeEnd = 0x0700;
        private const int PresentationFormsStart = 0xFB50;
        private const int PresentationFormsEnd = 0xFF00;
        private static readonly bool[] LowRangePunctuationOrSymbol = BuildPunctuationOrSymbolTable(0, LowRangeEnd);
        private static readonly bool[] PresentationFormsPunctuationOrSymbol =
            BuildPunctuationOrSymbolTable(PresentationFormsStart, PresentationFormsEnd);

        private static bool[] BuildPunctuationOrSymbolTable(int start, int end)
        {
            var table = new bool[end - start];
            for (int i = 0; i < table.Length; i++)
                table[i] = IsPunctuationOrSymbolCategory((char)(start + i));
            return table;
        }

        /// <summary>
        ///     Same as <c>char.IsPunctuation(ch) || char.IsSymbol(ch)</c> with a single category lookup.
        /// </summary>
        private static bool IsPunctuationOrSymbolCategory(char character)
        {
            UnicodeCategory category = char.GetUnicodeCategory(character);
            return category >= UnicodeCategory.ConnectorPunctuation && category <= UnicodeCategory.OtherSymbol;
        }

        /// <summary>
        ///     Same as <c>Char32Utils.IsPunctuation(ch) || Char32Utils.IsSymbol(ch)</c>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPunctuationOrSymbol(int character)
        {
            if ((uint)character < LowRangeEnd) return LowRangePunctuationOrSymbol[character];

            uint presentationIndex = (uint)(character - PresentationFormsStart);
            if (presentationIndex < PresentationFormsEnd - PresentationFormsStart)
                return PresentationFormsPunctuationOrSymbol[presentationIndex];

            return Char32Utils.IsUnicode16Char(character) && IsPunctuationOrSymbolCategory((char)character);
        }

        /// <summary>
        ///     Same as <see cref="Char32Utils.IsEnglishLetter"/>: setting bit 5 folds A-Z onto a-z.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsEnglishLetter(int character)
        {
            return (uint)((character | 0x20) - 'a') <= 'z' - 'a';
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNumber(int character, int zero)
        {
            return (uint)(character - zero) <= 9;
        }

        private static bool IsLtrNeighbour(int character, int zero)
        {
            return IsEnglishLetter(character) || IsNumber(character, zero) || Char32Utils.IsSymbol(character);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Reverse(int[] array, int start, int end)
        {
            while (start < end)
            {
                int temp = array[start];
                array[start] = array[end];
                array[end] = temp;
                start++;
                end--;
            }
        }

        /// <summary>
        ///     Fixes the flow of the text.
        /// </summary>
        public static void Fix(FastStringBuilder input, FastStringBuilder output, AramaicScript aramaicScript, bool fixTextTags, bool preserveNumbers)
        {
            int length = input.Length;
            int[] text = input.RawArray;

            // Every input character is written at most once, so reserve the room once and write straight into the array.
            output.EnsureAdditionalCapacity(length);
            int[] result = output.RawArray;
            int resultLength = output.Length;

            // Some texts like tags, English words and numbers need to be displayed in their original order.
            // Such a run is written to the output as it is found (i.e. reversed) starting at ltrStart,
            // and flipped back in place when the run ends. -1 means no run is open.
            int ltrStart = -1;

            int numberZero = preserveNumbers ? (int)EnglishNumbers.Zero
                : aramaicScript is AramaicScript.Persian ? (int)FarsiNumbers.Zero : (int)HinduNumbers.Zero;

            // When a '>' turns out not to close a valid tag, the backward search stopped at this index
            // (a '<' followed by a space, or -1 when there is no '<' at all). Any '>' above it would stop
            // at the same place, so it is known to be invalid without searching again.
            int failedTagSearchStop = int.MaxValue;

            for (int i = length - 1; i >= 0; i--)
            {
                bool isInMiddle = i > 0 && i < length - 1;
                bool isAtBeginning = i == 0;
                bool isAtEnd = i == length - 1;

                int characterAtThisIndex = text[i];
                int nextCharacter = isAtEnd ? 0 : text[i + 1];
                int previousCharacter = isAtBeginning ? 0 : text[i - 1];

                if (fixTextTags && characterAtThisIndex == '>' && i < failedTagSearchStop)
                {
                    // We need to check if it is actually the end of a tag.
                    int j = i - 1;
                    while (j >= 0 && text[j] != '<') j--;

                    // Tags do not start with space
                    if (j >= 0 && text[j + 1] != ' ')
                    {
                        if (ltrStart >= 0)
                        {
                            Reverse(result, ltrStart, resultLength - 1);
                            ltrStart = -1;
                        }

                        int tagLength = i - j + 1;
                        System.Array.Copy(text, j, result, resultLength, tagLength);
                        resultLength += tagLength;
                        i = j;
                        continue;
                    }

                    failedTagSearchStop = j;
                }

                if (IsPunctuationOrSymbol(characterAtThisIndex))
                {
                    // IsRTLCharacter returns false for null
                    bool isAfterRTLCharacter = Char32Utils.IsRTLCharacter(previousCharacter);
                    bool isBeforeRTLCharacter = Char32Utils.IsRTLCharacter(nextCharacter);

                    if ((isAfterRTLCharacter || isBeforeRTLCharacter) &&
                        TryGetMirroredCharacter(characterAtThisIndex, out int mirroredCharacter))
                    {
                        characterAtThisIndex = mirroredCharacter;
                    }

                    if (isInMiddle)
                    {
                        bool isBeforeWhiteSpace = Char32Utils.IsWhiteSpace(nextCharacter);
                        bool isAfterWhiteSpace = Char32Utils.IsWhiteSpace(previousCharacter);
                        bool isUnderline = characterAtThisIndex == '_';
                        bool isSpecialPunctuation = characterAtThisIndex == '.' ||
                                                    characterAtThisIndex == '،' ||
                                                    characterAtThisIndex == '؛';

                        if (isBeforeRTLCharacter && isAfterRTLCharacter ||
                            isAfterWhiteSpace && isSpecialPunctuation ||
                            isBeforeWhiteSpace && isAfterRTLCharacter ||
                            isBeforeRTLCharacter && isAfterWhiteSpace ||
                            (isBeforeRTLCharacter || isAfterRTLCharacter) && isUnderline)
                        {
                            if (ltrStart >= 0)
                            {
                                Reverse(result, ltrStart, resultLength - 1);
                                ltrStart = -1;
                            }

                            result[resultLength++] = characterAtThisIndex;
                        }
                        else
                        {
                            if (ltrStart < 0) ltrStart = resultLength;
                            result[resultLength++] = characterAtThisIndex;
                        }
                    }
                    else if (isAtEnd)
                    {
                        if (ltrStart < 0) ltrStart = resultLength;
                        result[resultLength++] = characterAtThisIndex;
                    }
                    else if (isAtBeginning)
                    {
                        if (ltrStart >= 0)
                        {
                            // This character goes out before the open run is flushed (the run is flushed after the loop),
                            // so flip the run and slide it one slot right to make room in front of it.
                            int runLength = resultLength - ltrStart;
                            Reverse(result, ltrStart, resultLength - 1);
                            System.Array.Copy(result, ltrStart, result, ltrStart + 1, runLength);
                            result[ltrStart] = characterAtThisIndex;
                            resultLength++;
                            ltrStart = -1;
                        }
                        else
                        {
                            result[resultLength++] = characterAtThisIndex;
                        }
                    }

                    continue;
                }

                // For cases where english words and farsi/arabic are mixed. This allows for using farsi/arabic, english and numbers in one sentence.
                // If the space is between numbers,symbols or English words, keep the order
                if (isInMiddle && characterAtThisIndex == ' ' &&
                    IsLtrNeighbour(nextCharacter, numberZero) &&
                    IsLtrNeighbour(previousCharacter, numberZero))
                {
                    if (ltrStart < 0) ltrStart = resultLength;
                    result[resultLength++] = characterAtThisIndex;
                    continue;
                }

                if (IsEnglishLetter(characterAtThisIndex) ||
                    IsNumber(characterAtThisIndex, numberZero) ||
                    // handle surrogates
                    (uint)(characterAtThisIndex - 0xD800) <= 0xDFFF - 0xD800)
                {
                    if (ltrStart < 0) ltrStart = resultLength;
                    result[resultLength++] = characterAtThisIndex;
                    continue;
                }

                if (ltrStart >= 0)
                {
                    Reverse(result, ltrStart, resultLength - 1);
                    ltrStart = -1;
                }

                if (characterAtThisIndex != 0xFFFF &&
                    characterAtThisIndex != (int)SpecialCharacters.ZeroWidthNoJoiner)
                {
                    result[resultLength++] = characterAtThisIndex;
                }
            }

            if (ltrStart >= 0)
                Reverse(result, ltrStart, resultLength - 1);

            output.SetLengthUnchecked(resultLength);
        }
    }
}
