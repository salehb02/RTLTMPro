namespace RTLTMPro
{
    /// <summary>
    ///     Sets up and creates the conversion table
    /// </summary>
    public static class GlyphTable
    {
        // Every convertible letter lives in the Arabic block (U+0600..U+06FF), so a flat array indexed by
        // (letter - 0x0600) replaces the dictionary. A zero entry means "no conversion".
        internal const int BlockStart = 0x0600;
        internal const int BlockSize = 0x100;

        // Initialized from a field initializer (no static constructor) so the runtime can skip per-call init checks.
        private static readonly char[] IsolatedForms = BuildIsolatedForms();

        /// <summary>
        ///     Setting up the conversion table
        /// </summary>
        private static char[] BuildIsolatedForms()
        {
            var map = new char[BlockSize];
            // Each ArabicGeneralLetters member maps to the ArabicIsolatedLetters member with the same name.
            Add(map, ArabicGeneralLetters.Hamza, ArabicIsolatedLetters.Hamza);
            Add(map, ArabicGeneralLetters.AlefMaddaAbove, ArabicIsolatedLetters.AlefMaddaAbove);
            Add(map, ArabicGeneralLetters.AlefHamzaAbove, ArabicIsolatedLetters.AlefHamzaAbove);
            Add(map, ArabicGeneralLetters.WawHamzaAbove, ArabicIsolatedLetters.WawHamzaAbove);
            Add(map, ArabicGeneralLetters.AlefHamzaBelow, ArabicIsolatedLetters.AlefHamzaBelow);
            Add(map, ArabicGeneralLetters.YehHamzaAbove, ArabicIsolatedLetters.YehHamzaAbove);
            Add(map, ArabicGeneralLetters.Alef, ArabicIsolatedLetters.Alef);
            Add(map, ArabicGeneralLetters.Beh, ArabicIsolatedLetters.Beh);
            Add(map, ArabicGeneralLetters.TehMarbuta, ArabicIsolatedLetters.TehMarbuta);
            Add(map, ArabicGeneralLetters.Teh, ArabicIsolatedLetters.Teh);
            Add(map, ArabicGeneralLetters.Theh, ArabicIsolatedLetters.Theh);
            Add(map, ArabicGeneralLetters.Jeem, ArabicIsolatedLetters.Jeem);
            Add(map, ArabicGeneralLetters.Hah, ArabicIsolatedLetters.Hah);
            Add(map, ArabicGeneralLetters.Khah, ArabicIsolatedLetters.Khah);
            Add(map, ArabicGeneralLetters.Dal, ArabicIsolatedLetters.Dal);
            Add(map, ArabicGeneralLetters.Thal, ArabicIsolatedLetters.Thal);
            Add(map, ArabicGeneralLetters.Reh, ArabicIsolatedLetters.Reh);
            Add(map, ArabicGeneralLetters.Zain, ArabicIsolatedLetters.Zain);
            Add(map, ArabicGeneralLetters.Seen, ArabicIsolatedLetters.Seen);
            Add(map, ArabicGeneralLetters.Sheen, ArabicIsolatedLetters.Sheen);
            Add(map, ArabicGeneralLetters.Sad, ArabicIsolatedLetters.Sad);
            Add(map, ArabicGeneralLetters.Dad, ArabicIsolatedLetters.Dad);
            Add(map, ArabicGeneralLetters.Tah, ArabicIsolatedLetters.Tah);
            Add(map, ArabicGeneralLetters.Zah, ArabicIsolatedLetters.Zah);
            Add(map, ArabicGeneralLetters.Ain, ArabicIsolatedLetters.Ain);
            Add(map, ArabicGeneralLetters.Ghain, ArabicIsolatedLetters.Ghain);
            Add(map, ArabicGeneralLetters.Feh, ArabicIsolatedLetters.Feh);
            Add(map, ArabicGeneralLetters.Qaf, ArabicIsolatedLetters.Qaf);
            Add(map, ArabicGeneralLetters.Kaf, ArabicIsolatedLetters.Kaf);
            Add(map, ArabicGeneralLetters.Lam, ArabicIsolatedLetters.Lam);
            Add(map, ArabicGeneralLetters.Meem, ArabicIsolatedLetters.Meem);
            Add(map, ArabicGeneralLetters.Noon, ArabicIsolatedLetters.Noon);
            Add(map, ArabicGeneralLetters.Heh, ArabicIsolatedLetters.Heh);
            Add(map, ArabicGeneralLetters.Waw, ArabicIsolatedLetters.Waw);
            Add(map, ArabicGeneralLetters.AlefMaksura, ArabicIsolatedLetters.AlefMaksura);
            Add(map, ArabicGeneralLetters.Yeh, ArabicIsolatedLetters.Yeh);
            Add(map, ArabicGeneralLetters.FarsiYeh, ArabicIsolatedLetters.FarsiYeh);
            Add(map, ArabicGeneralLetters.Peh, ArabicIsolatedLetters.Peh);
            Add(map, ArabicGeneralLetters.TCheh, ArabicIsolatedLetters.TCheh);
            Add(map, ArabicGeneralLetters.Jeh, ArabicIsolatedLetters.Jeh);
            Add(map, ArabicGeneralLetters.Keheh, ArabicIsolatedLetters.Keheh);
            Add(map, ArabicGeneralLetters.Gaf, ArabicIsolatedLetters.Gaf);
            Add(map, ArabicGeneralLetters.Ve, ArabicIsolatedLetters.Ve);
            Add(map, ArabicGeneralLetters.Oe, ArabicIsolatedLetters.Oe);
            Add(map, ArabicGeneralLetters.KurdishYeh, ArabicIsolatedLetters.KurdishYeh);
            Add(map, ArabicGeneralLetters.KurdishReh, ArabicIsolatedLetters.KurdishReh);
            Add(map, ArabicGeneralLetters.KurdishLam, ArabicIsolatedLetters.KurdishLam);
            Add(map, ArabicGeneralLetters.Ae, ArabicIsolatedLetters.Ae);
            return map;
        }

        private static void Add(char[] map, ArabicGeneralLetters general, ArabicIsolatedLetters isolated)
        {
            map[(int)general - BlockStart] = (char)isolated;
        }

        public static char Convert(char toBeConverted)
        {
            uint index = (uint)(toBeConverted - BlockStart);
            if (index < BlockSize)
            {
                char converted = IsolatedForms[index];
                if (converted != 0) return converted;
            }

            return toBeConverted;
        }
    }
}
