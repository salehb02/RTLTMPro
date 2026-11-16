#if RTL_EMOJI
using Kyub.EmojiSearch.Utilities;
#endif

using TMPro;
using UnityEngine;

#if RTL_LOCALIZATION
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
#endif

namespace RTLTMPro
{
    [ExecuteInEditMode]
    public class RTLTextMeshPro : TextMeshProUGUI, ISerializationCallbackReceiver
    {
        // ReSharper disable once InconsistentNaming
#if TMP_VERSION_2_1_0_OR_NEWER || UNITY_6000_0_OR_NEWER
        public override string text
#else
        public new string text
#endif
        {
            get { return base.text; }
            set
            {
                if (originalText == value)
                    return;

                originalText = value;

                UpdateText();
            }
        }

        public string OriginalText
        {
            get { return originalText; }
        }

        public bool ForceFarsiNumbers
        {
            get { return forceFarsiNumbers; }
            set
            {
                if (forceFarsiNumbers == value)
                    return;

                forceFarsiNumbers = value;
                havePropertiesChanged = true;
            }
        }

        public AramaicScript AramaicScript
        {
            get { return aramaicScript; }
            set
            {
                if (aramaicScript == value)
                    return;

                aramaicScript = value;
                havePropertiesChanged = true;
            }
        }

        public bool FixTags
        {
            get { return fixTags; }
            set
            {
                if (fixTags == value)
                    return;

                fixTags = value;
                havePropertiesChanged = true;
            }
        }

        public bool ForceFix
        {
            get { return forceFix; }
            set
            {
                if (forceFix == value)
                    return;

                forceFix = value;
                havePropertiesChanged = true;
            }
        }

        [SerializeField] protected bool forceFarsiNumbers;

        [SerializeField, HideInInspector] private bool farsi = true;

        // A flag to ensure we only migrate the data once per component.
        [SerializeField, HideInInspector] private bool hasMigratedToEnum = false;
        [SerializeField] protected AramaicScript aramaicScript = AramaicScript.Persian;

        [SerializeField][TextArea(3, 10)] protected string originalText;

        [SerializeField] protected bool fixTags = true;

        [SerializeField] protected bool forceFix;

        [SerializeField] protected bool updateTextOnEnable;

        protected static readonly FastStringBuilder finalText = new FastStringBuilder(RTLSupport.DefaultBufferSize);

        // Result of the last fix, reused when Update() runs only because some unrelated property changed.
        private RTLFixedTextCache fixedTextCache;
        private System.Func<char, bool> checkSupportCharCallback;

        protected void Update()
        {
            if (havePropertiesChanged)
            {
                if (fixedTextCache.Matches(originalText, aramaicScript, fixTags, forceFix, forceFarsiNumbers, farsi, spriteAsset))
                    ApplyFixedText(fixedTextCache.Text, fixedTextCache.IsRightToLeft);
                else
                    UpdateText();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

#if RTL_LOCALIZATION
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
            // The selected locale may have changed while this text was disabled.
            havePropertiesChanged = true;
#endif

            if (updateTextOnEnable)
                UpdateText();
        }

#if RTL_LOCALIZATION
        protected override void OnDisable()
        {
            base.OnDisable();

            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        }

        // The digits follow the selected locale (see RTLNumberFixer), so the text is fixed again when it
        // changes. The fix cache keys on the locale, so Update() re-fixes instead of reusing the old text.
        private void OnSelectedLocaleChanged(Locale locale)
        {
            havePropertiesChanged = true;
        }
#endif

        public void UpdateText()
        {
            if (originalText == null)
                originalText = "";

            // Some runtimes allocate a copy in Replace() even when there is no tab to remove.
            string processed = originalText.IndexOf('\t') >= 0 ? originalText.Replace("\t", string.Empty) : originalText;

#if RTL_EMOJI
            if (MayContainEmojiSequence(processed))
                TMP_EmojiSearchEngine.ParseEmojiCharSequence(spriteAsset, ref processed);
#endif

            bool isRightToLeft = ForceFix || TextUtils.IsRTLInput(processed);
            // Text without right-to-left letters is not fixed, but a Persian locale still gets Persian digits.
            processed = isRightToLeft ? GetFixedText(processed) : RTLNumberFixer.FixNumbersInLeftToRightText(processed.Trim());

            fixedTextCache.Store(originalText, aramaicScript, fixTags, forceFix, forceFarsiNumbers, farsi, spriteAsset, processed, isRightToLeft);
            ApplyFixedText(processed, isRightToLeft);
        }

        private void ApplyFixedText(string fixedText, bool isRightToLeft)
        {
            isRightToLeftText = isRightToLeft;
            base.text = fixedText;
            // Do not clear havePropertiesChanged here: TMP only builds the mesh while it is set, and clears it
            // itself once the mesh is built. Clearing it left texts set while inactive (and 3D texts) without
            // a mesh, and TMP then threw in UpdateSDFScale when such a text was scaled.
        }

        private string GetFixedText(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            finalText.Clear();
            RTLSupport.FixRTL(input, finalText, aramaicScript, fixTags, true, checkSupportCharCallback ??= CheckSupportChar, true);
            finalText.Reverse();
            // Convert digits while the text is still in the builder, so only one string gets allocated.
            RTLNumberFixer.FixNumbersInPlace(finalText, farsi, forceFarsiNumbers);
            return finalText.ToString();
        }

        private bool CheckSupportChar(char character)
        {
            return m_fontAsset.HasCharacter(character);
        }

#if RTL_EMOJI
        /// <summary>
        /// Cheap pre-check for <see cref="TMP_EmojiSearchEngine.ParseEmojiCharSequence"/>, which allocates for every
        /// character and builds its whole lookup table (~100k allocations for a full emoji sheet) on first use.
        /// The engine only replaces multi-code-point sequences (sprites named like "1f468-200d-1f4bb", "2764-fe0f" or
        /// "0023-fe0f-20e3"), and each of those contains a surrogate, an emoji-range symbol, a variation selector or
        /// the keycap mark; it also expands "\u"/"\U" escapes. Text with none of these is returned unchanged by the
        /// engine, so it can skip it.
        /// </summary>
        private static bool MayContainEmojiSequence(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c < 0x00A9)
                {
                    if (c == '\\')
                        return true;
                    continue;
                }

                if (c == 0x00A9 || c == 0x00AE)
                    return true;
                if (c < 0x2000)
                    continue; // Latin, Arabic and Persian letters
                if (c <= 0x206F)
                {
                    // General punctuation (ZWNJ, RTL marks, dashes, quotes): only these two start emoji sequences.
                    if (c == 0x203C || c == 0x2049)
                        return true;
                    continue;
                }

                if (c <= 0x2BFF || char.IsSurrogate(c)) // symbols, arrows, dingbats, keycap mark; astral emoji
                    return true;
                if (c == 0x3030 || c == 0x303D || c == 0x3297 || c == 0x3299 || c == 0xFE0E || c == 0xFE0F)
                    return true;
            }

            return false;
        }
#endif

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (hasMigratedToEnum) return;
            aramaicScript = farsi ? AramaicScript.Persian : AramaicScript.Arabic;
            hasMigratedToEnum = true;
        }
    }
}
