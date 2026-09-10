using System.Text;

#if RTL_LOCALIZATION
using UnityEngine.Localization.Settings;
#endif

namespace RTLTMPro
{
    public static class RTLNumberFixer
    {
        public static string FixNumbers(string entry, bool farsi, bool forceFarsiNumbers)
        {
            bool isFa = farsi;

#if RTL_LOCALIZATION
            isFa = LocalizationSettings.SelectedLocale != null && LocalizationSettings.SelectedLocale.Identifier.Code.ToLower().Contains("fa");
#endif

            return isFa || forceFarsiNumbers ? ConvertEnglishNumbersExceptTags(entry) : entry;
        }

        private static string ConvertEnglishNumbersExceptTags(string input)
        {
            StringBuilder result = null;
            int tagEnd = -1;

            for (int i = 0; i < input.Length; i++)
            {
                char character = input[i];
                if (i > tagEnd && character == '<')
                    tagEnd = input.IndexOf('>', i + 1);

                if (i <= tagEnd || character < '0' || character > '9')
                {
                    if (result != null) result.Append(character);
                    continue;
                }

                if (result == null)
                {
                    result = new StringBuilder(input.Length);
                    result.Append(input, 0, i);
                }

                result.Append((char)((int)FarsiNumbers.Zero + character - '0'));
            }

            return result == null ? input : result.ToString();
        }

        public static string ConvertToPersianDigits(string value)
        {
            StringBuilder result = null;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (character >= '0' && character <= '9')
                {
                    if (result == null)
                    {
                        result = new StringBuilder(value.Length);
                        result.Append(value, 0, i);
                    }
                    result.Append((char)((int)FarsiNumbers.Zero + character - '0'));
                }
                else if (result != null)
                {
                    result.Append(character);
                }
            }

            return result == null ? value : result.ToString();
        }

        public static string ConvertToEnglishDigits(string value)
        {
            StringBuilder result = null;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (character >= (char)FarsiNumbers.Zero && character <= (char)FarsiNumbers.Nine)
                {
                    if (result == null)
                    {
                        result = new StringBuilder(value.Length);
                        result.Append(value, 0, i);
                    }
                    result.Append((char)('0' + character - (char)FarsiNumbers.Zero));
                }
                else if (result != null)
                {
                    result.Append(character);
                }
            }

            return result == null ? value : result.ToString();
        }

        public static void ConvertToEnglishDigits(FastStringBuilder value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                int character = value.Get(i);
                if (character >= (int)FarsiNumbers.Zero && character <= (int)FarsiNumbers.Nine)
                    value.Set(i, '0' + character - (int)FarsiNumbers.Zero);
            }
        }
    }
}
