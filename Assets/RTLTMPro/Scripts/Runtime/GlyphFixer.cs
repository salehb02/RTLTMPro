using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RTLTMPro
{
    public static class GlyphFixer
    {
        // Kept for API compatibility. The hot path uses arithmetic conversion instead.
        public static Dictionary<char, char> EnglishToFarsiNumberMap = new Dictionary<char, char>()
        {
            [(char)EnglishNumbers.Zero] = (char)FarsiNumbers.Zero,
            [(char)EnglishNumbers.One] = (char)FarsiNumbers.One,
            [(char)EnglishNumbers.Two] = (char)FarsiNumbers.Two,
            [(char)EnglishNumbers.Three] = (char)FarsiNumbers.Three,
            [(char)EnglishNumbers.Four] = (char)FarsiNumbers.Four,
            [(char)EnglishNumbers.Five] = (char)FarsiNumbers.Five,
            [(char)EnglishNumbers.Six] = (char)FarsiNumbers.Six,
            [(char)EnglishNumbers.Seven] = (char)FarsiNumbers.Seven,
            [(char)EnglishNumbers.Eight] = (char)FarsiNumbers.Eight,
            [(char)EnglishNumbers.Nine] = (char)FarsiNumbers.Nine,
        };

        public static Dictionary<char, char> EnglishToHinduNumberMap = new Dictionary<char, char>()
        {
            [(char)EnglishNumbers.Zero] = (char)HinduNumbers.Zero,
            [(char)EnglishNumbers.One] = (char)HinduNumbers.One,
            [(char)EnglishNumbers.Two] = (char)HinduNumbers.Two,
            [(char)EnglishNumbers.Three] = (char)HinduNumbers.Three,
            [(char)EnglishNumbers.Four] = (char)HinduNumbers.Four,
            [(char)EnglishNumbers.Five] = (char)HinduNumbers.Five,
            [(char)EnglishNumbers.Six] = (char)HinduNumbers.Six,
            [(char)EnglishNumbers.Seven] = (char)HinduNumbers.Seven,
            [(char)EnglishNumbers.Eight] = (char)HinduNumbers.Eight,
            [(char)EnglishNumbers.Nine] = (char)HinduNumbers.Nine,
        };

        private const byte GlyphFixedFlag = 1;
        private const byte ConnectsToNextFlag = 2;
        private const byte ConnectsToPreviousFlag = 4;

        // Per-letter flags for U+0600..U+06FF, computed once from the original predicates at the bottom of this file.
        private static readonly byte[] LetterFlags = BuildLetterFlags();

        private static byte[] BuildLetterFlags()
        {
            var flags = new byte[GlyphTable.BlockSize];
            for (int i = 0; i < flags.Length; i++)
            {
                int character = GlyphTable.BlockStart + i;
                byte value = 0;
                if (TextUtils.IsGlyphFixedArabicCharacter((char)character)) value |= GlyphFixedFlag;
                if (CanCharacterBeConnectedToNext(character)) value |= ConnectsToNextFlag;
                if (CanCharacterBeConnectedToPrevious(character)) value |= ConnectsToPreviousFlag;
                flags[i] = value;
            }

            return flags;
        }

        /// <summary>
        ///     <see cref="ConnectsToNextFlag"/> / <see cref="ConnectsToPreviousFlag"/> for a code point.
        ///     Anything outside U+0600..U+06FF, including every code point above U+FFFF, never connects.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetConnectionFlags(int character)
        {
            uint index = (uint)(character - GlyphTable.BlockStart);
            return index < GlyphTable.BlockSize ? LetterFlags[index] : 0;
        }

        /// <summary>
        ///     Fixes the shape of letters based on their position.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="preserveNumbers"></param>
        /// <param name="aramaicScript"></param>
        /// <param name="fixTextTags"></param>
        /// <param name="checkSupportChar"></param>
        /// <returns></returns>
        public static void Fix(FastStringBuilder input, FastStringBuilder output, bool preserveNumbers, AramaicScript aramaicScript, bool fixTextTags, Func<char, bool> checkSupportChar = null)
        {
            FixYah(input, aramaicScript);

            output.SetValue(input);

            int[] letters = input.RawArray;
            int[] shapedLetters = output.RawArray;
            int length = input.Length;
            bool isKurdish = aramaicScript == AramaicScript.Kurdish;

            for (int i = 0; i < length; i++)
            {
                int iChar = letters[i];

                // For special Lam Letter connections.
                if (iChar == (int)ArabicGeneralLetters.Lam && i < length - 1 && HandleSpecialLam(input, output, i) ||
                    iChar == (int)ArabicGeneralLetters.KurdishLam && i < length - 1 && HandleSpecialKurdishLam(input, output, i))
                {
                    FixLamForm(input, output, ref i);   // For special Lam Letter connections.
                    continue;
                }

                // Index with the full code point: truncating to 16 bits first would shape e.g. U+20627 as Alef.
                uint letterIndex = (uint)(iChar - GlyphTable.BlockStart);
                if (letterIndex >= GlyphTable.BlockSize || (LetterFlags[letterIndex] & GlyphFixedFlag) == 0)
                    continue;

                char converted = (char)iChar;
                if (!isKurdish || converted != (char)ConstantChars.KurdishHa)
                    converted = GlyphTable.Convert(converted);

                // Neighbour connectivity, read once per letter.
                int currentFlags = GetConnectionFlags(iChar);
                bool previousConnects = i > 0 && (GetConnectionFlags(letters[i - 1]) & ConnectsToNextFlag) != 0;
                bool nextConnects = i < length - 1 && (GetConnectionFlags(letters[i + 1]) & ConnectsToPreviousFlag) != 0;
                bool connectsToPrevious = (currentFlags & ConnectsToPreviousFlag) != 0;
                bool connectsToNext = (currentFlags & ConnectsToNextFlag) != 0;

                bool isMiddle = previousConnects && connectsToPrevious && connectsToNext && nextConnects;
                bool isFinishing = previousConnects && connectsToPrevious && (!nextConnects || !connectsToNext);
                bool isLeading = (!previousConnects || !connectsToPrevious) && connectsToNext && nextConnects;
                char result;

                if (isKurdish && TryGetKurdishException(converted, isMiddle, isFinishing, isLeading, out char kurdishChar))
                {
                    result = kurdishChar;
                }
                else
                {
                    if (isMiddle) result = (char)(converted + 3);
                    else if (isFinishing) result = (char)(converted + 1);
                    else if (isLeading) result = (char)(converted + 2);
                    else result = converted;
                }

                bool isolated = !isMiddle && !isFinishing && !isLeading;
                if (isKurdish && isolated && iChar != converted)
                    CheckKurdishIsolateState(converted, iChar, checkSupportChar, out result);
                shapedLetters[i] = result;
            }

            if (!preserveNumbers)
            {
                if (fixTextTags) FixNumbersOutsideOfTags(output, aramaicScript);
                else FixNumbers(output, aramaicScript);
            }
        }

        /// <summary>
        /// Handles specific shaping exceptions for Kurdish characters that break the standard offset rule.
        /// </summary>
        private static bool TryGetKurdishException(char converted, bool isMiddle, bool isFinishing, bool isLeading, out char result)
        {
            bool isKurdishHa = converted is (char)0x0647 or (char)0x06BE;
            bool isKurdishAe = converted == (char)0x06D5;

            if (isKurdishHa)
            {
                if (isMiddle)
                {
                    result = (char)ConstantChars.KurdishHaMiddle;
                }
                else if (isFinishing)
                {
                    result = (char)ConstantChars.KurdishAeFinish;
                }
                else if (isLeading)
                {
                    result = (char)ConstantChars.KurdishHaLeading;
                }
                else
                {
                    result = converted;
                    return false;
                }

                return true;
            }

            if (isKurdishAe)
            {
                if (isMiddle || isFinishing)
                {
                    result = (char)ConstantChars.KurdishAeFinish;
                }
                else if (isLeading)
                {
                    result = (char)ConstantChars.KurdishAe;
                }
                else
                {
                    result = converted;
                    return false;
                }

                return true;
            }

            result = converted;
            return false;
        }

        private static void CheckKurdishIsolateState(char converted, int iChar, Func<char, bool> checkSupportChar, out char result)
        {
            var isSpecialKurdishLetter = TextUtils.IsSpecialKurdishLetter(converted) || checkSupportChar == null || !checkSupportChar(converted);
            result = isSpecialKurdishLetter ? (char)iChar : converted;
        }

        private static void FixLamForm(FastStringBuilder input, FastStringBuilder output, ref int i)
        {
            if (i > 0)
            {
                char converted = (char)output.Get(i);
                int previousIndexLetter = input.Get(i - 1);

                if ((GetConnectionFlags(previousIndexLetter) & ConnectsToNextFlag) != 0)
                {
                    var result = (char)(input.Get(i) == 0x644 ? converted + 1 : converted - 1);
                    output.Set(i, result);
                }
            }

            // Skip over the 0xFFFF character HandleSpecialLam injects.
            i++;
        }

        /// <summary>
        ///     Removes tashkeel. Converts general RTL letters to isolated form. Also fixes Farsi and Arabic ی letter.
        /// </summary>
        /// <param name="text">Input to prepare</param>
        /// <param name="aramaicScript"></param>
        /// /// <returns>Prepared input in char array</returns>
        public static void FixYah(FastStringBuilder text, AramaicScript aramaicScript)
        {
            int from, to;
            if (aramaicScript is AramaicScript.Kurdish or AramaicScript.Persian)
            {
                from = (int)ArabicGeneralLetters.Yeh;
                to = (int)ArabicGeneralLetters.FarsiYeh;
            }
            else if (aramaicScript is AramaicScript.Arabic)
            {
                from = (int)ArabicGeneralLetters.FarsiYeh;
                to = (int)ArabicGeneralLetters.Yeh;
            }
            else
            {
                return;
            }

            int[] letters = text.RawArray;
            int length = text.Length;
            for (int i = 0; i < length; i++)
            {
                if (letters[i] == from)
                    letters[i] = to;
            }
        }

        /// <summary>
        ///     Handles the special Lam-Alef connection in the text.
        ///     0x0625 0xFEF9
        ///     0x0627 0xFEFB
        ///     0x0623 0xFEF7
        ///     0x0622 0xFEF5
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="i">Index of Lam letter</param>
        /// <returns><see langword="true" /> if special connection has been made.</returns>
        private static bool HandleSpecialLam(FastStringBuilder input, FastStringBuilder output, int i)
        {
            bool isFixed;
            switch (input.Get(i + 1))
            {
                case (char)ArabicGeneralLetters.AlefHamzaBelow:
                    output.Set(i, (char)0xFEF9);
                    isFixed = true;
                    break;
                case (char)ArabicGeneralLetters.Alef:
                    output.Set(i, (char)0xFEFB);
                    isFixed = true;
                    break;
                case (char)ArabicGeneralLetters.AlefHamzaAbove:
                    output.Set(i, (char)0xFEF7);
                    isFixed = true;
                    break;
                case (char)ArabicGeneralLetters.AlefMaddaAbove:
                    output.Set(i, (char)0xFEF5);
                    isFixed = true;
                    break;
                default:
                    isFixed = false;
                    break;
            }

            if (isFixed)
            {
                output.Set(i + 1, (char)0xFFFF);
            }

            return isFixed;
        }

        private static bool HandleSpecialKurdishLam(FastStringBuilder input, FastStringBuilder output, int i)
        {
            bool isFixed;
            switch (input.Get(i + 1))
            {
                case (char)ArabicGeneralLetters.Alef:
                    output.Set(i, (char)0xE003);
                    isFixed = true;
                    break;
                default:
                    isFixed = false;
                    break;
            }

            if (isFixed)
            {
                output.Set(i + 1, (char)0xFFFF);
            }

            return isFixed;
        }

        /// <summary>
        ///     Converts English numbers to Persian or Arabic numbers.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="aramaicScript"></param>
        /// /// <returns>Converted number</returns>
        public static void FixNumbers(FastStringBuilder text, AramaicScript aramaicScript)
        {
            int offset = (aramaicScript is AramaicScript.Persian ? (int)FarsiNumbers.Zero : (int)HinduNumbers.Zero) - (int)EnglishNumbers.Zero;
            int[] letters = text.RawArray;
            int length = text.Length;
            for (int i = 0; i < length; i++)
            {
                int character = letters[i];
                if ((uint)(character - (int)EnglishNumbers.Zero) <= 9)
                    letters[i] = character + offset;
            }
        }

        /// <summary>
        ///     Converts English numbers that are outside tags to Persian or Arabic numbers.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="aramaicScript"></param>
        /// /// <returns>Text with converted numbers</returns>
        public static void FixNumbersOutsideOfTags(FastStringBuilder text, AramaicScript aramaicScript)
        {
            int offset = (aramaicScript is AramaicScript.Persian ? (int)FarsiNumbers.Zero : (int)HinduNumbers.Zero) - (int)EnglishNumbers.Zero;
            int[] letters = text.RawArray;
            int length = text.Length;
            for (int i = 0; i < length; i++)
            {
                var iChar = letters[i];
                // skip valid tags
                if (iChar == '<')
                {
                    bool sawValidTag = false;
                    for (int j = i + 1; j < length; j++)
                    {
                        int jChar = letters[j];
                        if ((j == i + 1 && jChar == ' ') || jChar == '<')
                        {
                            break;
                        }
                        else if (jChar == '>')
                        {
                            i = j;
                            sawValidTag = true;
                            break;
                        }
                    }

                    if (sawValidTag) continue;
                }

                if ((uint)(iChar - (int)EnglishNumbers.Zero) <= 9)
                {
                    letters[i] = iChar + offset;
                }
            }
        }

        // Original connection predicates. Only used to build LetterFlags; the hot path reads the table.
        // Tatweel (kashida) joins on both sides so the letters around it keep their connected forms, but it is
        // not glyph-fixed: it has no presentation forms and is left as it is.
        private static bool CanCharacterBeConnectedToNext(int character)
        {
            if (character == (int)ArabicGeneralLetters.Tatweel)
                return true;

            return
                character != (int)ArabicGeneralLetters.Hamza &&
                character != (int)ArabicGeneralLetters.AlefMaddaAbove &&
                character != (int)ArabicGeneralLetters.AlefHamzaAbove &&
                character != (int)ArabicGeneralLetters.AlefHamzaBelow &&
                character != (int)ArabicGeneralLetters.WawHamzaAbove &&
                character != (int)ArabicGeneralLetters.Alef &&
                character != (int)ArabicGeneralLetters.Dal &&
                character != (int)ArabicGeneralLetters.Thal &&
                character != (int)ArabicGeneralLetters.Reh &&
                character != (int)ArabicGeneralLetters.Zain &&
                character != (int)ArabicGeneralLetters.Jeh &&
                character != (int)ArabicGeneralLetters.Waw &&
                character != (int)ArabicGeneralLetters.KurdishReh &&
                character != (int)ArabicGeneralLetters.Oe &&
                character != (int)ArabicGeneralLetters.Ae &&
                TextUtils.IsGlyphFixedArabicCharacter((char)character);
        }

        private static bool CanCharacterBeConnectedToPrevious(int character)
        {
            if (character == (int)ArabicGeneralLetters.Tatweel)
                return true;

            return
                character != (int)ArabicGeneralLetters.Hamza &&
                TextUtils.IsGlyphFixedArabicCharacter((char)character);
        }
    }

}
