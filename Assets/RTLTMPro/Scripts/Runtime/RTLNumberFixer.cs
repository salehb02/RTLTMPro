using System;

#if RTL_LOCALIZATION
using UnityEngine.Localization.Settings;
#endif

namespace RTLTMPro
{
    public static class RTLNumberFixer
    {
        private const int FarsiZeroOffset = (int)FarsiNumbers.Zero - '0';

        // Scratch buffer for building converted strings, so a conversion allocates only its result.
        [ThreadStatic] private static char[] charBuffer;

        public static string FixNumbers(string entry, bool farsi, bool forceFarsiNumbers)
        {
            return ShouldConvert(farsi, forceFarsiNumbers) ? ConvertEnglishNumbersExceptTags(entry) : entry;
        }

        /// <summary>
        ///     In-place version of <see cref="FixNumbers(string, bool, bool)"/>, for text that is still in a
        ///     <see cref="FastStringBuilder"/>. Gives the same result as converting the builder's string.
        /// </summary>
        public static void FixNumbersInPlace(FastStringBuilder entry, bool farsi, bool forceFarsiNumbers)
        {
            if (ShouldConvert(farsi, forceFarsiNumbers))
                ConvertEnglishNumbersExceptTags(entry);
        }

        /// <summary>
        ///     For text that is not treated as right-to-left, which skips shaping and <see cref="FixNumbers"/>:
        ///     converts English digits outside tags to Persian digits when <see cref="IsFarsiLocaleSelected"/>.
        /// </summary>
        public static string FixNumbersInLeftToRightText(string entry)
        {
            return string.IsNullOrEmpty(entry) || !IsFarsiLocaleSelected() ? entry : ConvertEnglishNumbersExceptTags(entry);
        }

        /// <summary>
        ///     True when Unity Localization is installed and its selected locale is Persian (the locale code contains "fa").
        /// </summary>
        public static bool IsFarsiLocaleSelected()
        {
#if RTL_LOCALIZATION
            var locale = LocalizationSettings.SelectedLocale;
            return locale != null && locale.Identifier.Code.IndexOf("fa", StringComparison.OrdinalIgnoreCase) >= 0;
#else
            return false;
#endif
        }

        private static bool ShouldConvert(bool farsi, bool forceFarsiNumbers)
        {
            bool isFa = farsi;

#if RTL_LOCALIZATION
            isFa = IsFarsiLocaleSelected();
#endif

            return isFa || forceFarsiNumbers;
        }

        private static char[] GetBuffer(int length)
        {
            char[] buffer = charBuffer;
            if (buffer == null || buffer.Length < length)
                charBuffer = buffer = new char[Math.Max(length, 256)];
            return buffer;
        }

        private static string ConvertEnglishNumbersExceptTags(string input)
        {
            char[] result = null;
            int tagEnd = -1;
            bool hasMoreClosingBrackets = true;

            for (int i = 0; i < input.Length; i++)
            {
                char character = input[i];
                if (i > tagEnd && character == '<')
                {
                    // Once there is no '>' left, every later search fails too.
                    tagEnd = hasMoreClosingBrackets ? input.IndexOf('>', i + 1) : -1;
                    hasMoreClosingBrackets = tagEnd >= 0;
                }

                if (i <= tagEnd || character < '0' || character > '9')
                {
                    if (result != null) result[i] = character;
                    continue;
                }

                if (result == null)
                {
                    result = GetBuffer(input.Length);
                    input.CopyTo(0, result, 0, i);
                }

                result[i] = (char)(character + FarsiZeroOffset);
            }

            return result == null ? input : new string(result, 0, input.Length);
        }

        private static void ConvertEnglishNumbersExceptTags(FastStringBuilder input)
        {
            int[] letters = input.RawArray;
            int length = input.Length;
            int tagEnd = -1;
            bool hasMoreClosingBrackets = true;

            for (int i = 0; i < length; i++)
            {
                int character = letters[i];
                if (i > tagEnd && character == '<')
                {
                    tagEnd = -1;
                    if (hasMoreClosingBrackets)
                    {
                        for (int j = i + 1; j < length; j++)
                        {
                            if (letters[j] == '>')
                            {
                                tagEnd = j;
                                break;
                            }
                        }
                    }

                    hasMoreClosingBrackets = tagEnd >= 0;
                }

                if (i > tagEnd && (uint)(character - '0') <= 9)
                    letters[i] = character + FarsiZeroOffset;
            }
        }

        public static string ConvertToPersianDigits(string value)
        {
            char[] result = null;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (character >= '0' && character <= '9')
                {
                    if (result == null)
                    {
                        result = GetBuffer(value.Length);
                        value.CopyTo(0, result, 0, i);
                    }
                    result[i] = (char)(character + FarsiZeroOffset);
                }
                else if (result != null)
                {
                    result[i] = character;
                }
            }

            return result == null ? value : new string(result, 0, value.Length);
        }

        public static string ConvertToEnglishDigits(string value)
        {
            char[] result = null;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (character >= (char)FarsiNumbers.Zero && character <= (char)FarsiNumbers.Nine)
                {
                    if (result == null)
                    {
                        result = GetBuffer(value.Length);
                        value.CopyTo(0, result, 0, i);
                    }
                    result[i] = (char)(character - FarsiZeroOffset);
                }
                else if (result != null)
                {
                    result[i] = character;
                }
            }

            return result == null ? value : new string(result, 0, value.Length);
        }

        public static void ConvertToEnglishDigits(FastStringBuilder value)
        {
            int[] letters = value.RawArray;
            int length = value.Length;
            for (int i = 0; i < length; i++)
            {
                int character = letters[i];
                if ((uint)(character - (int)FarsiNumbers.Zero) <= 9)
                    letters[i] = character - FarsiZeroOffset;
            }
        }
    }
}
