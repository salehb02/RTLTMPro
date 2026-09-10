using System.Collections.Generic;
// ReSharper disable IdentifierTypo

namespace RTLTMPro
{
    public static class TashkeelFixer
    {
        [System.ThreadStatic] private static List<TashkeelLocation> tashkeelLocations;
        private static List<TashkeelLocation> TashkeelLocations =>
            tashkeelLocations ?? (tashkeelLocations = new List<TashkeelLocation>(100));

        private static readonly HashSet<char> TashkeelCharactersSet = new HashSet<char>() {
            (char)TashkeelCharacters.Fathan,
            (char)TashkeelCharacters.Dammatan,
            (char)TashkeelCharacters.Kasratan,
            (char)TashkeelCharacters.Fatha,
            (char)TashkeelCharacters.Damma,
            (char)TashkeelCharacters.Kasra,
            (char)TashkeelCharacters.Shadda,
            (char)TashkeelCharacters.Sukun,
            (char)TashkeelCharacters.MaddahAbove,
            (char)TashkeelCharacters.SuperscriptAlef,
            (char)TashkeelCharacters.ShaddaWithDammatanIsolatedForm,
            (char)TashkeelCharacters.ShaddaWithKasratanIsolatedForm,
            (char)TashkeelCharacters.ShaddaWithFathaIsolatedForm,
            (char)TashkeelCharacters.ShaddaWithDammaIsolatedForm,
            (char)TashkeelCharacters.ShaddaWithKasraIsolatedForm,
            (char)TashkeelCharacters.ShaddaWithSuperscriptAlefIsolatedForm
        };

        private static readonly Dictionary<char, char> ShaddaCombinationMap = new Dictionary<char, char>()
        {
            [(char)TashkeelCharacters.Dammatan] = (char)TashkeelCharacters.ShaddaWithDammatanIsolatedForm,
            [(char)TashkeelCharacters.Kasratan] = (char)TashkeelCharacters.ShaddaWithKasratanIsolatedForm,
            [(char)TashkeelCharacters.Fatha] = (char)TashkeelCharacters.ShaddaWithFathaIsolatedForm,
            [(char)TashkeelCharacters.Damma] = (char)TashkeelCharacters.ShaddaWithDammaIsolatedForm,
            [(char)TashkeelCharacters.Kasra] = (char)TashkeelCharacters.ShaddaWithKasraIsolatedForm,
            [(char)TashkeelCharacters.SuperscriptAlef] = (char)TashkeelCharacters.ShaddaWithSuperscriptAlefIsolatedForm,
        };

        /// <summary>
        ///     Removes tashkeel from text.
        /// </summary>
        public static void RemoveTashkeel(FastStringBuilder input)
        {
            TashkeelLocations.Clear();
            int j = 0; // write index
            for (int i = 0; i < input.Length; i++)
            {
                int curChar = input.Get(i);
                if (Char32Utils.IsUnicode16Char(curChar) && TashkeelCharactersSet.Contains((char)curChar))
                {
                    TashkeelLocations.Add(new TashkeelLocation((TashkeelCharacters)curChar, i));
                }
                else
                {
                    input.Set(j, curChar);
                    j++;
                }
            }
            input.Length = j;
        }

        /// <summary>
        ///     Restores removed tashkeel.
        /// </summary>
        public static void RestoreTashkeel(FastStringBuilder letters)
        {
            int markCount = TashkeelLocations.Count;
            if (markCount == 0) return;

            int originalLength = letters.Length;
            letters.EnsureAdditionalCapacity(markCount);
            letters.SetLengthUnchecked(originalLength + markCount);

            int readIndex = originalLength - 1;
            int markIndex = markCount - 1;
            for (int writeIndex = originalLength + markCount - 1; writeIndex >= 0; writeIndex--)
            {
                if (markIndex >= 0 && TashkeelLocations[markIndex].Position == writeIndex)
                {
                    letters.Set(writeIndex, (int)TashkeelLocations[markIndex].Tashkeel);
                    markIndex--;
                }
                else
                {
                    letters.Set(writeIndex, letters.Get(readIndex));
                    readIndex--;
                }
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
            int j = 0; // write index
            int i = 0; // read index
            while (i < input.Length)
            {
                int curChar = input.Get(i);
                int nextChar = i < input.Length - 1 ? input.Get(i + 1) : (char)0;
                if ((TashkeelCharacters)curChar == TashkeelCharacters.Shadda
                    && ShaddaCombinationMap.TryGetValue((char)nextChar, out char combined))
                {
                    input.Set(j, combined);
                    j++;
                    i += 2;
                }
                else
                {
                    input.Set(j, curChar);
                    j++;
                    i++;
                }
            }
            input.Length = j;
        }
    }
}
