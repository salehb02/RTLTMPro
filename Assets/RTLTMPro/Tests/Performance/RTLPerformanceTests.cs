using NUnit.Framework;
using Unity.PerformanceTesting;

namespace RTLTMPro.Tests.Performance
{
    [Category("Performance")]
    public class RTLPerformanceTests
    {
        private const string Persian = "متن فارسی برای بررسی سرعت پردازش و نمایش صحیح کلمات ";
        private const string Mixed = "متن فارسی and English 123.456 در کنار هم (sample) ";
        private const string Rich = "متن <color=#FAF>رنگی <b>و ضخیم 123</b></color> ساده ";
        private const string Tashkeel = "تَشْكِيلُ النَّصِّ العَرَبِيِّ بِصُورَةٍ صَحِيحَةٍ ";
        private const string Unicode = "متن فارسی 😀 English 123 🚀 שלום العربية ";

        private FastStringBuilder output;

        [SetUp]
        public void SetUp() => output = new FastStringBuilder(4096);

        [TestCase(16), TestCase(256), TestCase(4096), Performance]
        public void FixRTL_PersianPlain(int length) => MeasurePipeline(BuildText(Persian, length), AramaicScript.Persian, false, true);

        [TestCase(256), TestCase(4096), Performance]
        public void FixRTL_MixedText(int length) => MeasurePipeline(BuildText(Mixed, length), AramaicScript.Persian, false, true);

        [TestCase(256), TestCase(4096), Performance]
        public void FixRTL_RichText(int length) => MeasurePipeline(BuildText(Rich, length), AramaicScript.Persian, true, true);

        [TestCase(256), TestCase(4096), Performance]
        public void FixRTL_Tashkeel(int length) => MeasurePipeline(BuildText(Tashkeel, length), AramaicScript.Arabic, false, true);

        [TestCase(256), TestCase(4096), Performance]
        public void FixRTL_UnicodeAndEmoji(int length) => MeasurePipeline(BuildText(Unicode, length), AramaicScript.Persian, false, true);

        [TestCase(AramaicScript.Persian), TestCase(AramaicScript.Arabic), TestCase(AramaicScript.Kurdish), Performance]
        public void FixRTL_ByScript(AramaicScript script) => MeasurePipeline(BuildText(Mixed, 1024), script, true, false);

        [Test, Performance]
        public void ConvertNumbers_OutsideTags()
        {
            string input = BuildText("123 <size=123>456</size> 789 ", 4096);
            Measure.Method(() => RTLNumberFixer.FixNumbers(input, true, true))
                .WarmupCount(10).MeasurementCount(50).IterationsPerMeasurement(10).GC().Run();
        }

        private void MeasurePipeline(string input, AramaicScript script, bool fixTags, bool preserveNumbers)
        {
            Measure.Method(() =>
                {
                    output.Clear();
                    RTLSupport.FixRTL(input, output, script, fixTags, preserveNumbers);
                })
                .WarmupCount(10).MeasurementCount(50)
                .IterationsPerMeasurement(input.Length <= 256 ? 100 : 10)
                .GC().Run();
        }

        private static string BuildText(string seed, int length)
        {
            var builder = new System.Text.StringBuilder(length + seed.Length);
            while (builder.Length < length) builder.Append(seed);
            if (builder.Length > length) builder.Length = length;
            return builder.ToString();
        }
    }
}
