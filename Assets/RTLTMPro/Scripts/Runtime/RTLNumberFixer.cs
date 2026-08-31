using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

#if RTL_LOCALIZATION
using UnityEngine.Localization.Settings;
#endif

namespace RTLTMPro
{
    public static class RTLNumberFixer
    {
        private static readonly Dictionary<char, char> ENGLISH_TO_PERSIAN_DIGITS = new Dictionary<char, char>()
        {
            {'0' ,'۰'},
            {'1','۱' },
            {'2','۲' },
            {'3','۳' },
            {'4','۴' },
            {'5','۵' },
            {'6','۶' },
            {'7','۷' },
            {'8','۸' },
            {'9','۹' },
        };

        private static readonly Dictionary<char, char> PERSIAN_TO_ENGLISH_DIGITS = new Dictionary<char, char>()
        {
            {'۰','0'},
            {'۱','1' },
            {'۲','2' },
            {'۳','3' },
            {'۴','4' },
            {'۵','5' },
            {'۶','6' },
            {'۷','7' },
            {'۸','8' },
            {'۹','9' },
        };

        public static string FixNumbers(string entry, bool farsi, bool forceFarsiNumbers)
        {
            bool isFa = farsi;

#if RTL_LOCALIZATION
            isFa = LocalizationSettings.SelectedLocale != null && LocalizationSettings.SelectedLocale.Identifier.Code.ToLower().Contains("fa");
#endif

            if (!isFa && !forceFarsiNumbers)
                return entry;

            string fixedStr = ConvertEnglishNumbersExceptTags(entry);
            return fixedStr;
        }

        private static string ConvertEnglishNumbersExceptTags(string input)
        {
            Regex regex = new Regex(@"(<[^>]*>)|([^<]+)");

            return regex.Replace(input, match =>
            {
                if (match.Value.StartsWith("<") && match.Value.EndsWith(">"))
                    return match.Value;

                return ConvertToPersianDigits(match.Value);
            });
        }

        public static string ConvertToPersianDigits(string value)
        {
            StringBuilder finalStr = new StringBuilder();

            for (int i = 0; i < value.Length; i++)
            {
                if (ENGLISH_TO_PERSIAN_DIGITS.ContainsKey(value[i]))
                {
                    finalStr.Append(ENGLISH_TO_PERSIAN_DIGITS[value[i]]);
                    continue;
                }

                finalStr.Append(value[i]);
            }

            return finalStr.ToString();
        }

        public static string ConvertToEnglishDigits(string value)
        {
            StringBuilder finalStr = new StringBuilder();

            for (int i = 0; i < value.Length; i++)
            {
                if (PERSIAN_TO_ENGLISH_DIGITS.ContainsKey(value[i]))
                {
                    finalStr.Append(PERSIAN_TO_ENGLISH_DIGITS[value[i]]);
                    continue;
                }

                finalStr.Append(value[i]);
            }

            return finalStr.ToString();
        }
    }
}