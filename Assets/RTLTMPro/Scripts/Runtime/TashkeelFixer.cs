using System;
// ReSharper disable IdentifierTypo

namespace RTLTMPro
{
    public static class TashkeelFixer
    {
        // Removed marks, kept as parallel arrays (position in the original text, mark character).
        [ThreadStatic] private static int[] markPositions;
        [ThreadStatic] private static int[] markCharacters;
        [ThreadStatic] private static int markCount;
        [ThreadStatic] private static bool removedShadda;

        /// <summary>
        ///     True when the last <see cref="RemoveTashkeel"/> call on this thread removed a Shadda,
        ///     i.e. when <see cref="FixShaddaCombinations"/> can have anything to combine.
        /// </summary>
        internal static bool RemovedShadda => removedShadda;

        private const int HarakatLow = (int)TashkeelCharacters.Fathan;          // 0x064B
        private const int HarakatHigh = (int)TashkeelCharacters.MaddahAbove;    // 0x0653
        private const int ShaddaFormsLow = (int)TashkeelCharacters.ShaddaWithDammatanIsolatedForm;        // 0xFC5E
        private const int ShaddaFormsHigh = (int)TashkeelCharacters.ShaddaWithSuperscriptAlefIsolatedForm; // 0xFC63

        /// <summary>
        ///     Same set as <see cref="TashkeelCharacters"/>, as range checks instead of a hash lookup.
        /// </summary>
        private static bool IsTashkeel(int ch)
        {
            return (uint)(ch - HarakatLow) <= HarakatHigh - HarakatLow
                   || ch == (int)TashkeelCharacters.SuperscriptAlef
                   || (uint)(ch - ShaddaFormsLow) <= ShaddaFormsHigh - ShaddaFormsLow;
        }

        /// <summary>
        ///     Removes tashkeel from text.
        /// </summary>
        public static void RemoveTashkeel(FastStringBuilder input)
        {
            int[] arr = input.RawArray;
            int len = input.Length;

            // Fast scan: most text has no tashkeel at all, and nothing before the first mark moves.
            int i = 0;
            while (i < len && !IsTashkeel(arr[i])) i++;

            if (i == len)
            {
                markCount = 0;
                removedShadda = false;
                return;
            }

            int[] positions = markPositions;
            int[] characters = markCharacters;
            if (positions == null)
            {
                markPositions = positions = new int[100];
                markCharacters = characters = new int[100];
            }

            int count = 0;
            bool shadda = false;
            int j = i; // write index
            for (; i < len; i++)
            {
                int curChar = arr[i];
                if (IsTashkeel(curChar))
                {
                    if (count == positions.Length)
                    {
                        Array.Resize(ref markPositions, count * 2);
                        Array.Resize(ref markCharacters, count * 2);
                        positions = markPositions;
                        characters = markCharacters;
                    }

                    positions[count] = i;
                    characters[count] = curChar;
                    count++;
                    shadda |= curChar == (int)TashkeelCharacters.Shadda;
                }
                else
                {
                    arr[j++] = curChar;
                }
            }

            markCount = count;
            removedShadda = shadda;
            input.Length = j;
        }

        /// <summary>
        ///     Restores removed tashkeel.
        /// </summary>
        public static void RestoreTashkeel(FastStringBuilder letters)
        {
            int count = markCount;
            if (count == 0) return;

            int[] positions = markPositions;
            int[] characters = markCharacters;

            int originalLength = letters.Length;
            letters.EnsureAdditionalCapacity(count);
            letters.SetLengthUnchecked(originalLength + count);

            int[] arr = letters.RawArray;
            int readIndex = originalLength - 1;
            int markIndex = count - 1;
            // Once the first mark is placed every remaining item is already in its final slot.
            for (int writeIndex = originalLength + count - 1; markIndex >= 0; writeIndex--)
            {
                if (positions[markIndex] == writeIndex)
                {
                    arr[writeIndex] = characters[markIndex];
                    markIndex--;
                }
                else
                {
                    arr[writeIndex] = arr[readIndex];
                    readIndex--;
                }
            }
        }

        private static bool TryGetShaddaCombination(char mark, out int combined)
        {
            switch (mark)
            {
                case (char)TashkeelCharacters.Dammatan:
                    combined = (int)TashkeelCharacters.ShaddaWithDammatanIsolatedForm;
                    return true;
                case (char)TashkeelCharacters.Kasratan:
                    combined = (int)TashkeelCharacters.ShaddaWithKasratanIsolatedForm;
                    return true;
                case (char)TashkeelCharacters.Fatha:
                    combined = (int)TashkeelCharacters.ShaddaWithFathaIsolatedForm;
                    return true;
                case (char)TashkeelCharacters.Damma:
                    combined = (int)TashkeelCharacters.ShaddaWithDammaIsolatedForm;
                    return true;
                case (char)TashkeelCharacters.Kasra:
                    combined = (int)TashkeelCharacters.ShaddaWithKasraIsolatedForm;
                    return true;
                case (char)TashkeelCharacters.SuperscriptAlef:
                    combined = (int)TashkeelCharacters.ShaddaWithSuperscriptAlefIsolatedForm;
                    return true;
                default:
                    combined = 0;
                    return false;
            }
        }

        /// <summary>
        /// Replace Shadda + Another Tashkeel with combined form
        /// </summary>
        public static void FixShaddaCombinations(FastStringBuilder input)
        {
            /*
             * Fix of https://github.com/mnarimani/RTLTMPro/issues/13
             */
            int[] arr = input.RawArray;
            int len = input.Length;

            // Nothing before the first Shadda changes.
            int i = 0; // read index
            while (i < len && arr[i] != (int)TashkeelCharacters.Shadda) i++;
            if (i == len) return;

            int j = i; // write index
            while (i < len)
            {
                int curChar = arr[i];
                if (curChar == (int)TashkeelCharacters.Shadda
                    && i < len - 1
                    && TryGetShaddaCombination((char)arr[i + 1], out int combined))
                {
                    arr[j++] = combined;
                    i += 2;
                }
                else
                {
                    arr[j++] = curChar;
                    i++;
                }
            }
            input.Length = j;
        }
    }
}
