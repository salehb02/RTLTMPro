// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo

namespace RTLTMPro
{
    public static class RTLSupport
    {
        public const int DefaultBufferSize = 2048;

        [System.ThreadStatic] private static FastStringBuilder inputBuilder;
        [System.ThreadStatic] private static FastStringBuilder glyphFixerOutput;

        /// <summary>
        ///     Fixes the provided string
        /// </summary>
        /// <param name="input">Text to fix</param>
        /// <param name="output">Fixed text</param>
        ///         /// <param name="aramaicScript"></param>
        /// <param name="fixTextTags"></param>
        /// <param name="preserveNumbers"></param>
        /// <param name="checkSupportChar"></param>
        /// /// <returns>Fixed text</returns>
        public static void FixRTL(string input,
            FastStringBuilder output,
            AramaicScript aramaicScript = AramaicScript.Persian,
            bool fixTextTags = true,
            bool preserveNumbers = false,
           System.Func<char, bool> checkSupportChar = null,
           bool convertPersianNumbersToEnglish = false)
        {
            if (inputBuilder == null) inputBuilder = new FastStringBuilder(DefaultBufferSize);
            if (glyphFixerOutput == null) glyphFixerOutput = new FastStringBuilder(DefaultBufferSize);

            inputBuilder.SetValue(input);
            if (convertPersianNumbersToEnglish)
                RTLNumberFixer.ConvertToEnglishDigits(inputBuilder);
            TashkeelFixer.RemoveTashkeel(inputBuilder);
            // The shape of the letters in shapeFixedLetters is fixed according to their position in word. But the flow of the text is not fixed.
            GlyphFixer.Fix(inputBuilder, glyphFixerOutput, preserveNumbers, aramaicScript, fixTextTags, checkSupportChar);
            //Restore tashkeel to their places.
            TashkeelFixer.RestoreTashkeel(glyphFixerOutput);

            // Only restored marks can contain a Shadda, so there is nothing to combine without one.
            if (TashkeelFixer.RemovedShadda)
                TashkeelFixer.FixShaddaCombinations(glyphFixerOutput);
            // Fix flow of the text and put the result in FinalLetters field
            LigatureFixer.Fix(glyphFixerOutput, output, aramaicScript, fixTextTags, preserveNumbers);
            if (fixTextTags)
            {
                RichTextFixer.Fix(output);
            }
            inputBuilder.Clear();
        }

    }
}
