using System;
using System.Collections.Generic;

namespace RTLTMPro
{
    public static class GlyphFixer
    {
        public static Dictionary<char, char> EnglishToFarsiNumberMap = new Dictionary<char, char>()
        {
            [(char)EnglishNumbers.Zero] = (char)FarsiNumbers.Zero,
            [(char)EnglishNumbers.One] = (char)FarsiNumbers.One,
            [(char)EnglishNumbers.Two] = (char)FarsiNumbers.Two,
            [(char)EnglishNumbers.Three] = (char)FarsiNumbers.Three,
            [(char)EnglishNumbers.Four] = (char)FarsiNumbers.Four,
            [(char)EnglishNumbers.Five] = (char)FarsiNumbers.Five,
            [(char)EnglishNumbers.Six] = (char)FarsiNumbers.Six,
            [(char)EnglishNumbers.Seven] = (char)FarsiNumbers.Seven,
            [(char)EnglishNumbers.Eight] = (char)FarsiNumbers.Eight,
            [(char)EnglishNumbers.Nine] = (char)FarsiNumbers.Nine,
        };

        public static Dictionary<char, char> EnglishToHinduNumberMap = new Dictionary<char, char>()
        {
            [(char)EnglishNumbers.Zero] = (char)HinduNumbers.Zero,
            [(char)EnglishNumbers.One] = (char)HinduNumbers.One,
            [(char)EnglishNumbers.Two] = (char)HinduNumbers.Two,
            [(char)EnglishNumbers.Three] = (char)HinduNumbers.Three,
            [(char)EnglishNumbers.Four] = (char)HinduNumbers.Four,
            [(char)EnglishNumbers.Five] = (char)HinduNumbers.Five,
            [(char)EnglishNumbers.Six] = (char)HinduNumbers.Six,
            [(char)EnglishNumbers.Seven] = (char)HinduNumbers.Seven,
            [(char)EnglishNumbers.Eight] = (char)HinduNumbers.Eight,
            [(char)EnglishNumbers.Nine] = (char)HinduNumbers.Nine,
        };


        /// <summary>
        ///     Fixes the shape of letters based on their position.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="preserveNumbers"></param>
        /// <param name="aramaicScript"></param>
        /// <param name="fixTextTags"></param>
        /// <param name="checkSupportChar"></param>
        /// <returns></returns>
        public static void Fix(FastStringBuilder input, FastStringBuilder output, bool preserveNumbers, AramaicScript aramaicScript, bool fixTextTags, Func<char, bool> checkSupportChar = null)
        {
            FixYah(input, aramaicScript);

            output.SetValue(input);

            for (int i = 0; i < input.Length; i++)
            {
                int iChar = input.Get(i);

                // For special Lam Letter connections.
                if (iChar == (int)ArabicGeneralLetters.Lam && i < input.Length - 1 && HandleSpecialLam(input, output, i) ||
                                    iChar == (int)ArabicGeneralLetters.KurdishLam && i < input.Length - 1 && HandleSpecialKurdishLam(input, output, i))
                {
                    FixLamForm(input, output, ref i);   // For special Lam Letter connections.
                }
                else if (TextUtils.IsGlyphFixedArabicCharacter((char)iChar))
                {
                    char converted = (char)iChar;

                    if (aramaicScript != AramaicScript.Kurdish || converted != (char)ConstantChars.KurdishHa)
                        converted = GlyphTable.Convert(converted);

                    bool isMiddle = IsMiddleLetter(input, i);
                    bool isFinishing = IsFinishingLetter(input, i);
                    bool isLeading = IsLeadingLetter(input, i);
                    char result;

                    if (aramaicScript == AramaicScript.Kurdish && TryGetKurdishException(converted, isMiddle, isFinishing, isLeading, out char kurdishChar))
                    {
                        result = kurdishChar;
                    }
                    else
                    {
                        if (isMiddle) result = (char)(converted + 3);
                        else if (isFinishing) result = (char)(converted + 1);
                        else if (isLeading) result = (char)(converted + 2);
                        else result = converted;
                    }

                    bool isolated = !isMiddle && !isFinishing && !isLeading;
                    if (aramaicScript is AramaicScript.Kurdish && isolated && iChar != converted)
                        CheckKurdishIsolateState(converted, iChar, checkSupportChar, out result);
                    output.Set(i, result);
                }
            }

            if (!preserveNumbers)
            {
                if (fixTextTags) FixNumbersOutsideOfTags(output, aramaicScript);
                else FixNumbers(output, aramaicScript);
            }
        }

        /// <summary>
        /// Handles specific shaping exceptions for Kurdish characters that break the standard offset rule.
        /// </summary>
        private static bool TryGetKurdishException(char converted, bool isMiddle, bool isFinishing, bool isLeading, out char result)
        {
            bool isKurdishHa = converted is (char)0x0647 or (char)0x06BE;
            bool isKurdishAe = converted == (char)0x06D5;

            if (isKurdishHa)
            {
                if (isMiddle)
                {
                    result = (char)ConstantChars.KurdishHaMiddle;
                }
                else if (isFinishing)
                {
                    result = (char)ConstantChars.KurdishAeFinish;
                }
                else if (isLeading)
                {
                    result = (char)ConstantChars.KurdishHaLeading;
                }
                else
                {
                    result = converted;
                    return false;
                }

                return true;
            }

            if (isKurdishAe)
            {
                if (isMiddle || isFinishing)
                {
                    result = (char)ConstantChars.KurdishAeFinish;
                }
                else if (isLeading)
                {
                    result = (char)ConstantChars.KurdishAe;
                }
                else
                {
                    result = converted;
                    return false;
                }

                return true;
            }

            result = converted;
            return false;
        }

        private static void CheckKurdishIsolateState(char converted, int iChar, Func<char, bool> checkSupportChar, out char result)
        {
            var isSpecialKurdishLetter = TextUtils.IsSpecialKurdishLetter(converted) || checkSupportChar == null || !checkSupportChar(converted);
            result = isSpecialKurdishLetter ? (char)iChar : converted;
        }

        private static void FixLamForm(FastStringBuilder input, FastStringBuilder output, ref int i)
        {
            if (i > 0)
            {
                char converted = (char)output.Get(i);
                int previousIndexLetter = input.Get(i - 1);

                if (CanCharacterBeConnectedToNext(previousIndexLetter))
                {
                    var result = (char)(input.Get(i) == 0x644 ? converted + 1 : converted - 1);
                    output.Set(i, result);
                }
            }

            // Skip over the 0xFFFF character HandleSpecialLam injects. 
            i++;
        }

        /// <summary>
        ///     Removes tashkeel. Converts general RTL letters to isolated form. Also fixes Farsi and Arabic ی letter.
        /// </summary>
        /// <param name="text">Input to prepare</param>
        /// <param name="aramaicScript"></param>
        /// /// <returns>Prepared input in char array</returns>
        public static void FixYah(FastStringBuilder text, AramaicScript aramaicScript)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (aramaicScript is AramaicScript.Kurdish or AramaicScript.Persian && text.Get(i) == (int)ArabicGeneralLetters.Yeh)
                {
                    text.Set(i, (char)ArabicGeneralLetters.FarsiYeh);
                }
                else if (aramaicScript is AramaicScript.Arabic && text.Get(i) == (int)ArabicGeneralLetters.FarsiYeh)
                {
                    text.Set(i, (char)ArabicGeneralLetters.Yeh);
                }
            }
        }

        /// <summary>
        ///     Handles the special Lam-Alef connection in the text.
        ///     0x0625 0xFEF9
        ///     0x0627 0xFEFB
        ///     0x0623 0xFEF7
        ///     0x0622 0xFEF5
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="i">Index of Lam letter</param>
        /// <returns><see langword="true" /> if special connection has been made.</returns>
        private static bool HandleSpecialLam(FastStringBuilder input, FastStringBuilder output, int i)
        {
            bool isFixed;
            switch (input.Get(i + 1))
            {
                case (char)ArabicGeneralLetters.AlefHamzaBelow:
                    output.Set(i, (char)0xFEF9);
                    isFixed = true;
                    break;
                case (char)ArabicGeneralLetters.Alef:
                    output.Set(i, (char)0xFEFB);
                    isFixed = true;
                    break;
                case (char)ArabicGeneralLetters.AlefHamzaAbove:
                    output.Set(i, (char)0xFEF7);
                    isFixed = true;
                    break;
                case (char)ArabicGeneralLetters.AlefMaddaAbove:
                    output.Set(i, (char)0xFEF5);
                    isFixed = true;
                    break;
                default:
                    isFixed = false;
                    break;
            }

            if (isFixed)
            {
                output.Set(i + 1, (char)0xFFFF);
            }

            return isFixed;
        }

        private static bool HandleSpecialKurdishLam(FastStringBuilder input, FastStringBuilder output, int i)
        {
            bool isFixed;
            switch (input.Get(i + 1))
            {
                case (char)ArabicGeneralLetters.Alef:
                    output.Set(i, (char)0xE003);
                    isFixed = true;
                    break;
                default:
                    isFixed = false;
                    break;
            }

            if (isFixed)
            {
                output.Set(i + 1, (char)0xFFFF);
            }

            return isFixed;
        }

        /// <summary>
        ///     Converts English numbers to Persian or Arabic numbers.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="aramaicScript"></param>
        /// /// <returns>Converted number</returns>
        public static void FixNumbers(FastStringBuilder text, AramaicScript aramaicScript)
        {
            var farsi = aramaicScript is AramaicScript.Persian;
            text.Replace((char)EnglishNumbers.Zero, farsi ? (char)FarsiNumbers.Zero : (char)HinduNumbers.Zero);
            text.Replace((char)EnglishNumbers.One, farsi ? (char)FarsiNumbers.One : (char)HinduNumbers.One);
            text.Replace((char)EnglishNumbers.Two, farsi ? (char)FarsiNumbers.Two : (char)HinduNumbers.Two);
            text.Replace((char)EnglishNumbers.Three, farsi ? (char)FarsiNumbers.Three : (char)HinduNumbers.Three);
            text.Replace((char)EnglishNumbers.Four, farsi ? (char)FarsiNumbers.Four : (char)HinduNumbers.Four);
            text.Replace((char)EnglishNumbers.Five, farsi ? (char)FarsiNumbers.Five : (char)HinduNumbers.Five);
            text.Replace((char)EnglishNumbers.Six, farsi ? (char)FarsiNumbers.Six : (char)HinduNumbers.Six);
            text.Replace((char)EnglishNumbers.Seven, farsi ? (char)FarsiNumbers.Seven : (char)HinduNumbers.Seven);
            text.Replace((char)EnglishNumbers.Eight, farsi ? (char)FarsiNumbers.Eight : (char)HinduNumbers.Eight);
            text.Replace((char)EnglishNumbers.Nine, farsi ? (char)FarsiNumbers.Nine : (char)HinduNumbers.Nine);
        }

        /// <summary>
        ///     Converts English numbers that are outside tags to Persian or Arabic numbers.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="aramaicScript"></param>
        /// /// <returns>Text with converted numbers</returns>
        public static void FixNumbersOutsideOfTags(FastStringBuilder text, AramaicScript aramaicScript)
        {
            var englishDigits = new HashSet<char>(EnglishToFarsiNumberMap.Keys);
            for (int i = 0; i < text.Length; i++)
            {
                var iChar = text.Get(i);
                // skip valid tags
                if (iChar == '<')
                {
                    bool sawValidTag = false;
                    for (int j = i + 1; j < text.Length; j++)
                    {
                        int jChar = text.Get(j);
                        if ((j == i + 1 && jChar == ' ') || jChar == '<')
                        {
                            break;
                        }
                        else if (jChar == '>')
                        {
                            i = j;
                            sawValidTag = true;
                            break;
                        }
                    }

                    if (sawValidTag) continue;
                }

                if (englishDigits.Contains((char)iChar))
                {
                    text.Set(i, aramaicScript is AramaicScript.Persian ? EnglishToFarsiNumberMap[(char)iChar] : EnglishToHinduNumberMap[(char)iChar]);
                }
            }
        }

        /// <summary>
        ///     Is the letter at provided index a leading letter?
        /// </summary>
        /// <returns><see langword="true" /> if the letter is a leading letter</returns>
        private static bool IsLeadingLetter(FastStringBuilder letters, int index)
        {
            if (index == letters.Length - 1)
                return false;

            int currentIndexLetter = letters.Get(index);
            int previousIndexLetter = index != 0 ? letters.Get(index - 1) : default;
            int nextIndexLetter = letters.Get(index + 1);

            return (index == 0 ||
                   !CanCharacterBeConnectedToNext(previousIndexLetter) ||
                   !CanCharacterBeConnectedToPrevious(currentIndexLetter)) &&
                   CanCharacterBeConnectedToNext(currentIndexLetter) &&
                   CanCharacterBeConnectedToPrevious(nextIndexLetter);
        }

        /// <summary>
        ///     Is the letter at provided index a finishing letter?
        /// </summary>
        /// <returns><see langword="true" /> if the letter is a finishing letter</returns>
        private static bool IsFinishingLetter(FastStringBuilder letters, int index)
        {
            if (index == 0)
                return false;

            int currentIndexLetter = letters.Get(index);
            int previousIndexLetter = letters.Get(index - 1);
            int nextIndexLetter = index < letters.Length - 1 ? letters.Get(index + 1) : default;

            return
                CanCharacterBeConnectedToNext(previousIndexLetter) &&
                CanCharacterBeConnectedToPrevious(currentIndexLetter) &&
                (index == letters.Length - 1 ||
                !CanCharacterBeConnectedToPrevious(nextIndexLetter) ||
                !CanCharacterBeConnectedToNext(currentIndexLetter)
                );
        }

        /// <summary>
        ///     Is the letter at provided index a middle letter?
        /// </summary>
        /// <returns><see langword="true" /> if the letter is a middle letter</returns>
        private static bool IsMiddleLetter(FastStringBuilder letters, int index)
        {
            if (index == 0 || index == letters.Length - 1)
                return false;

            int currentIndexLetter = letters.Get(index);
            int previousIndexLetter = letters.Get(index - 1);
            int nextIndexLetter = letters.Get(index + 1);

            return
                CanCharacterBeConnectedToNext(previousIndexLetter) &&
                CanCharacterBeConnectedToPrevious(currentIndexLetter) &&
                CanCharacterBeConnectedToNext(currentIndexLetter) &&
                CanCharacterBeConnectedToPrevious(nextIndexLetter);
        }

        private static bool CanCharacterBeConnectedToNext(int character)
        {
            return
                character != (int)ArabicGeneralLetters.Hamza &&
                character != (int)ArabicGeneralLetters.AlefMaddaAbove &&
                character != (int)ArabicGeneralLetters.AlefHamzaAbove &&
                character != (int)ArabicGeneralLetters.AlefHamzaBelow &&
                character != (int)ArabicGeneralLetters.WawHamzaAbove &&
                character != (int)ArabicGeneralLetters.Alef &&
                character != (int)ArabicGeneralLetters.Dal &&
                character != (int)ArabicGeneralLetters.Thal &&
                character != (int)ArabicGeneralLetters.Reh &&
                character != (int)ArabicGeneralLetters.Zain &&
                character != (int)ArabicGeneralLetters.Jeh &&
                character != (int)ArabicGeneralLetters.Waw &&
                character != (int)ArabicGeneralLetters.KurdishReh &&
                character != (int)ArabicGeneralLetters.Oe &&
                character != (int)ArabicGeneralLetters.Ae &&
                TextUtils.IsGlyphFixedArabicCharacter((char)character);
        }

        private static bool CanCharacterBeConnectedToPrevious(int character)
        {
            return
                character != (int)ArabicGeneralLetters.Hamza &&
                TextUtils.IsGlyphFixedArabicCharacter((char)character);
        }
    }

}