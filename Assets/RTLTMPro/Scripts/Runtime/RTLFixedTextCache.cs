#if RTL_LOCALIZATION
using UnityEngine.Localization.Settings;
#endif

namespace RTLTMPro
{
    /// <summary>
    ///     Remembers the inputs and the result of the last fix made by an RTL text component.
    ///     TMP raises <c>havePropertiesChanged</c> for any property change (color, material, ...), and the
    ///     components re-fix their text when it is raised. When none of the inputs of the fix changed, the
    ///     stored result is exactly what fixing again would produce, so it can be reused.
    /// </summary>
    internal struct RTLFixedTextCache
    {
        private bool hasValue;
        private string source;
        private AramaicScript aramaicScript;
        private bool fixTags;
        private bool forceFix;
        private bool forceFarsiNumbers;
        private bool farsi;
        private object spriteAsset;
        private object locale;

        public string Text { get; private set; }
        public bool IsRightToLeft { get; private set; }

        public bool Matches(string source, AramaicScript aramaicScript, bool fixTags, bool forceFix,
            bool forceFarsiNumbers, bool farsi, object spriteAsset)
        {
            return hasValue &&
                   ReferenceEquals(this.source, source) &&
                   this.aramaicScript == aramaicScript &&
                   this.fixTags == fixTags &&
                   this.forceFix == forceFix &&
                   this.forceFarsiNumbers == forceFarsiNumbers &&
                   this.farsi == farsi &&
                   ReferenceEquals(this.spriteAsset, spriteAsset) &&
                   ReferenceEquals(locale, GetCurrentLocale());
        }

        public void Store(string source, AramaicScript aramaicScript, bool fixTags, bool forceFix,
            bool forceFarsiNumbers, bool farsi, object spriteAsset, string text, bool isRightToLeft)
        {
            // Kurdish shaping asks the font whether it has a glyph, and dynamic font assets gain glyphs at
            // runtime, so the same inputs can give a different result later. Never reuse those.
            hasValue = aramaicScript != AramaicScript.Kurdish;
            this.source = source;
            this.aramaicScript = aramaicScript;
            this.fixTags = fixTags;
            this.forceFix = forceFix;
            this.forceFarsiNumbers = forceFarsiNumbers;
            this.farsi = farsi;
            this.spriteAsset = spriteAsset;
            locale = GetCurrentLocale();
            Text = text;
            IsRightToLeft = isRightToLeft;
        }

        private static object GetCurrentLocale()
        {
#if RTL_LOCALIZATION
            // RTLNumberFixer picks the digits from the selected locale.
            return LocalizationSettings.SelectedLocale;
#else
            return null;
#endif
        }
    }
}
