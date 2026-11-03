using TMPro;
using UnityEngine;

namespace RTLTMPro
{
    [ExecuteInEditMode]
    public class RTLTextMeshPro3D : TextMeshPro, ISerializationCallbackReceiver
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

        protected readonly FastStringBuilder finalText = new FastStringBuilder(RTLSupport.DefaultBufferSize);

        // Result of the last fix, reused when Update() runs only because some unrelated property changed.
        private RTLFixedTextCache fixedTextCache;
        private System.Func<char, bool> checkSupportCharCallback;

        protected void Update()
        {
            if (havePropertiesChanged)
            {
                if (fixedTextCache.Matches(originalText, aramaicScript, fixTags, forceFix, forceFarsiNumbers, farsi, null))
                    ApplyFixedText(fixedTextCache.Text, fixedTextCache.IsRightToLeft);
                else
                    UpdateText();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (updateTextOnEnable)
                UpdateText();
        }

        public void UpdateText()
        {
            if (originalText == null)
                originalText = "";

            bool isRightToLeft = ForceFix || TextUtils.IsRTLInput(originalText);
            // Text without right-to-left letters is not fixed, but a Persian locale still gets Persian digits.
            string fixedText = isRightToLeft ? GetFixedText(originalText) : RTLNumberFixer.FixNumbersInLeftToRightText(originalText);

            fixedTextCache.Store(originalText, aramaicScript, fixTags, forceFix, forceFarsiNumbers, farsi, null, fixedText, isRightToLeft);
            ApplyFixedText(fixedText, isRightToLeft);
        }

        private void ApplyFixedText(string fixedText, bool isRightToLeft)
        {
            isRightToLeftText = isRightToLeft;
            base.text = fixedText;

            havePropertiesChanged = false;
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
