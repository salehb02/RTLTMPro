using NUnit.Framework;
using RTLTMPro;

namespace Tests
{
    public class RTLSupportTests
    {
        // Base Tests
        [TestCase("هَذَا النَّص العربي", "ﻲﺑﺮﻌﻟﺍ ﺺﱠﻨﻟﺍ ﺍَﺬَﻫ", AramaicScript.Arabic, true, true, TestName = "Arabic text is successfully converted")]
        [TestCase("متن فارسی", "ﯽﺳﺭﺎﻓ ﻦﺘﻣ", AramaicScript.Persian, true, true, TestName = "Farsi text is successfully converted")]
        [TestCase("صبا", "ﺎﺒﺻ", AramaicScript.Arabic, true, true, TestName = "Tashkeel is maintained in beginning of text")]
        [TestCase("مَرد", "ﺩﺮَﻣ", AramaicScript.Arabic, true, true, TestName = "Tashkeel is maintained in middle of text")]
        [TestCase("صبحِ", "ِﺢﺒﺻ", AramaicScript.Arabic, true, true, TestName = "Tashkeel is maintained at end of text")]
        [TestCase("العالم", "ﻢﻟﺎﻌﻟﺍ", AramaicScript.Arabic, true, true, TestName = "Arabic word conversion")]
        [TestCase("ﺍﻟﻌﺎﻟﻢ", "ﻢﻟﺎﻌﻟﺍ", AramaicScript.Arabic, true, true, TestName = "Arabic presentation form word should not convert")]
        [TestCase("اعلام لام", "ﻡﻻ ﻡﻼﻋﺍ", AramaicScript.Arabic, true, true, TestName = "Special lam handling base case")]
        [TestCase("اعلآم لآم", "ﻡﻵ ﻡﻶﻋﺍ", AramaicScript.Arabic, true, true, TestName = "Special lam handling AlefMaddaAbove case")]
        [TestCase("اعلأم لأم", "ﻡﻷ ﻡﻸﻋﺍ", AramaicScript.Arabic, true, true, TestName = "Special lam handling AlefHamzaAbove case")]
        [TestCase("اعلإم لإم", "ﻡﻹ ﻡﻺﻋﺍ", AramaicScript.Arabic, true, true, TestName = "Special lam handling AlefHamzaBelow case")]

        [TestCase("متن <color=yellow>زرد</color> ساده", "ﻩﺩﺎﺳ >roloc/<ﺩﺭﺯ>wolley=roloc< ﻦﺘﻣ", AramaicScript.Persian, true, true, TestName = "Rich text scene: yellow text")]
        [TestCase("کلمه‌ها ميتوانند در وسط جمله <size=100>بزرگ</size> باشند", "ﺪﻨﺷﺎﺑ >ezis/<ﮒﺭﺰﺑ>001=ezis< ﻪﻠﻤﺟ ﻂﺳﻭ ﺭﺩ ﺪﻨﻧﺍﻮﺘﯿﻣ ﺎﻫﻪﻤﻠﮐ", AramaicScript.Persian, true, true, TestName = "Rich text scene: large text")]
        [TestCase("کلمه‌ها حتي ميتوانند <voffset=1em>بالا</voffset> يا <voffset=-1em>پايين</voffset> باشند", "ﺪﻨﺷﺎﺑ >tesffov/<ﻦﯿﯾﺎﭘ>me1-=tesffov< ﺎﯾ >tesffov/<ﻻﺎﺑ>me1=tesffov< ﺪﻨﻧﺍﻮﺘﯿﻣ ﯽﺘﺣ ﺎﻫﻪﻤﻠﮐ", AramaicScript.Persian, true, true, TestName = "Rich text scene: sub and super text")]
        [TestCase("ﺗﺴﺖ <color=#FAF>ﺭﻧﮕﯽ<b><i>ﺑﺰﺭﮒ</b></color>ﮐﺞ</i>  <sprite index=1/>", ">/1=xedni etirps<  >i/<ﺞﮐ>roloc/<>b/<ﮒﺭﺰﺑ>i<>b<ﯽﮕﻧﺭ>FAF#=roloc< ﺖﺴﺗ", AramaicScript.Persian, true, true, TestName = "Rich text scene: color, emoji, bold and italic text")]

        // Demo Persian Scene
        [TestCase("متن ساده، ویرگول، دو نقطه: اعلام آزادی مردم", "ﻡﺩﺮﻣ ﯼﺩﺍﺯﺁ ﻡﻼﻋﺍ :ﻪﻄﻘﻧ ﻭﺩ ،ﻝﻮﮔﺮﯾﻭ ،ﻩﺩﺎﺳ ﻦﺘﻣ", AramaicScript.Persian, true, true, TestName = "Demo Persian scene: comma and colon")]
        [TestCase("متن فارسی and english در کنار هم", "ﻢﻫ ﺭﺎﻨﮐ ﺭﺩ and english ﯽﺳﺭﺎﻓ ﻦﺘﻣ", AramaicScript.Persian, true, true, TestName = "Demo Persian scene: mixed with english")]
        [TestCase("عدد انگلیسی 123 و 123.456 و 123,456 و (123) و (123.456) و (123,456) پایان.",
            ".ﻥﺎﯾﺎﭘ (123,456) ﻭ (123.456) ﻭ (123) ﻭ 123,456 ﻭ 123.456 ﻭ 123 ﯽﺴﯿﻠﮕﻧﺍ ﺩﺪﻋ", AramaicScript.Persian, true, true, TestName = "Demo Persian scene: english numbers")]

        // [TestCase("عدد فارسی 123 و 123.456 و 123,456 و (123) و (123.456) و (123,456) و [123] و [123.456] و [123,456] پایان.", ".ﻥﺎﯾﺎﭘ [۴۵۶,۱۲۳] ﻭ [۴۵۶.۱۲۳] ﻭ [۱۲۳] ﻭ (۴۵۶,۱۲۳) ﻭ (۴۵۶.۱۲۳) ﻭ (۱۲۳) ﻭ ۴۵۶,۱۲۳ ﻭ ۴۵۶.۱۲۳ ﻭ ۱۲۳ ﯽﺳﺭﺎﻓ ﺩﺪﻋ", true, true, false, TestName = "Demo Persian scene: farsi numbers")]
        [TestCase("متن با (پرانتز) {گیومه} و [براکت] و «کوت»", "«ﺕﻮﮐ» ﻭ [ﺖﮐﺍﺮﺑ] ﻭ {ﻪﻣﻮﯿﮔ} (ﺰﺘﻧﺍﺮﭘ) ﺎﺑ ﻦﺘﻣ", AramaicScript.Persian, true, true, TestName = "Demo Persian scene: parentheses, brackets")]
        [TestCase("نيم‌فاصله: مي‌خواهم - نيازمندي‌ها فاصله: مي خواهم - نيازمندي ها", "ﺎﻫ ﯼﺪﻨﻣﺯﺎﯿﻧ - ﻢﻫﺍﻮﺧ ﯽﻣ :ﻪﻠﺻﺎﻓ ﺎﻫﯼﺪﻨﻣﺯﺎﯿﻧ - ﻢﻫﺍﻮﺧﯽﻣ :ﻪﻠﺻﺎﻓﻢﯿﻧ", AramaicScript.Persian, true, true, TestName = "Demo Persian scene: zero width non joiner")]

        // Demo Arabic Scene
        [TestCase("تشکیل: ببَبِبُبًبٍبٌبْبّبَّبِّبُّبًّبٍّبٌّب، منظَّم، المؤثرات الصوتية", "ﺔﻴﺗﻮﺼﻟﺍ ﺕﺍﺮﺛﺆﻤﻟﺍ ،ﻢﱠﻈﻨﻣ ،ﺐﱞﺒﱟﺒًّﺒﱡﺒﱢﺒﱠﺒّﺒْﺒٌﺒٍﺒًﺒُﺒِﺒَﺒﺑ :ﻞﻴﮑﺸﺗ", AramaicScript.Arabic, true, true, TestName = "Demo Arabic scene: tashkeel")]
        [TestCase("الكلمات العربية and English جنباً إلى جنب", "ﺐﻨﺟ ﻰﻟﺇ ًﺎﺒﻨﺟ and English ﺔﻴﺑﺮﻌﻟﺍ ﺕﺎﻤﻠﻜﻟﺍ", AramaicScript.Arabic, true, true, TestName = "Demo Arabic scene: mixed with english")]

        // Demo Hebrew Scene
        [TestCase("חבילה זו תומכת כעת בשפה העברית!", "!תירבעה הפשב תעכ תכמות וז הליבח", AramaicScript.Arabic, true, true, TestName = "Demo Hebrew scene: base")]
        [TestCase("לפניכם טקסט מעורבב של English וטקסט עברי.", ".ירבע טסקטו English לש בברועמ טסקט םכינפל", AramaicScript.Arabic, true, true, TestName = "Demo Hebrew scene: mixed with english")]
        // Demo Kurdish Scene
        [TestCase("سڵاو و ڕێزی تایبەت بۆ هەموو هەڤاڵان و بەکارهێنەرانی خۆشەویست تکایە هەر باگ و کێشەیەکتان هەبوو پەیوەندیم پێوە بکەن", "نﮑﺑ ەﻮﭘ ﻢﯾﺪﻧەﻮﯾﭘ وﻮﺑھ نﺎﺘﮐﯾﺸﮐ و گﺎﺑ رھ ﯾﺎﮑﺗ ﺖﺴﯾوﺷﯚﺧ ﯽﻧارﻨھرﺎﮐﺑ و نﺎﭬھ وﻮﻣھ ﯚﺑ تﺒﯾﺎﺗ یﺰڕ و وﺳ", AramaicScript.Kurdish, true, true, TestName = "Kurdish simple text")]

        public void FixRTL(string input, string expected, AramaicScript aramaicScript, bool fixTags, bool preserveNumbers, System.Func<char, bool> checkSupportChar = null)
        {
            FastStringBuilder outut = new FastStringBuilder(RTLSupport.DefaultBufferSize);

            RTLSupport.FixRTL(input, outut, aramaicScript, fixTags, preserveNumbers, checkSupportChar);
            string result = outut.ToString();

            Assert.AreEqual(expected, result);
        }
    }
}