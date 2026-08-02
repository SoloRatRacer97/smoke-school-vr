using System.Collections;
using System.Linq;
using NUnit.Framework;

namespace SmokeSchool.Tests
{
    public class SmokeSchoolAppStateTests
    {
        private object white;
        private object black;

        [SetUp]
        public void SetUp()
        {
            white = TestReflection.EnumValue("SmokeSchoolAppState+SmokeSection", "White");
            black = TestReflection.EnumValue("SmokeSchoolAppState+SmokeSection", "Black");
            Reset();
        }

        [TearDown]
        public void TearDown()
        {
            Reset();
        }

        [Test]
        public void RecordAnswer_ComputesDeviationAndOverwritesTheSameQuestion()
        {
            Record(white, 0, 35, 55, "White35_V1-0001.mp4");
            IList results = OrderedResults();
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(TestReflection.GetField(results[0], "questionNumber"), Is.EqualTo(1));
            Assert.That(TestReflection.GetField(results[0], "deviation"), Is.EqualTo(4));

            Record(white, 0, 35, 40, "White35_V1-0002.mp4");
            results = OrderedResults();
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(TestReflection.GetField(results[0], "deviation"), Is.EqualTo(1));
            Assert.That(TestReflection.GetField(results[0], "videoFilename"), Is.EqualTo("White35_V1-0002.mp4"));
        }

        [Test]
        public void WhiteAndBlackResultsRemainIndependentAndOrdered()
        {
            Record(black, 1, 55, 60, "Black55_V2-0002.mp4");
            Record(white, 1, 25, 20, "White25_V1-0002.mp4");
            Record(black, 0, 50, 50, "Black50_V2-0001.mp4");
            Record(white, 0, 20, 20, "White20_V1-0001.mp4");

            IList results = OrderedResults();
            Assert.That(results.Count, Is.EqualTo(4));
            Assert.That(TestReflection.GetField(results[0], "questionNumber"), Is.EqualTo(1));
            Assert.That(TestReflection.GetField(results[1], "questionNumber"), Is.EqualTo(2));
            Assert.That(TestReflection.GetField(results[2], "questionNumber"), Is.EqualTo(1));
            Assert.That(TestReflection.GetField(results[3], "questionNumber"), Is.EqualTo(2));
            Assert.That(TestReflection.GetField(results[0], "smokeSection").ToString(), Is.EqualTo("White"));
            Assert.That(TestReflection.GetField(results[2], "smokeSection").ToString(), Is.EqualTo("Black"));
        }

        [Test]
        public void FailureBoundary_IsStrictlyGreaterThanThreeDeviationPoints()
        {
            Record(white, 0, 50, 65, "White50_V1-0001.mp4");
            Record(white, 1, 50, 70, "White50_V1-0002.mp4");

            IList failures = (IList)TestReflection.InvokeStatic("SmokeSchoolAppState", "GetCertificationFailures", 3);
            Assert.That(failures.Count, Is.EqualTo(1));
            Assert.That(TestReflection.GetField(failures[0], "questionNumber"), Is.EqualTo(2));
            Assert.That(TestReflection.GetField(failures[0], "deviation"), Is.EqualTo(4));
        }

        [Test]
        public void SectionScoreBoundary_TracksThirtySevenAndThirtyEight()
        {
            for (int index = 0; index < 12; index++) Record(white, index, 50, 65, $"White50_V1-{index + 1:D4}.mp4");
            Record(white, 12, 50, 55, "White50_V1-0013.mp4");
            Assert.That(GetTotal(white), Is.EqualTo(37));

            Record(white, 13, 50, 55, "White50_V1-0014.mp4");
            Assert.That(GetTotal(white), Is.EqualTo(38));
        }

        [Test]
        public void CertificationCompleteness_RequiresTwentyFiveAnswersPerSection()
        {
            for (int index = 0; index < 25; index++)
            {
                Record(white, index, 50, 50, $"White50_V1-{index + 1:D4}.mp4");
                if (index < 24) Record(black, index, 50, 50, $"Black50_V2-{index + 1:D4}.mp4");
            }

            Assert.That((bool)TestReflection.GetStaticProperty("CertificationResultReporter", "HasCompleteReadings"), Is.False);
            Record(black, 24, 50, 50, "Black50_V2-0025.mp4");
            Assert.That((bool)TestReflection.GetStaticProperty("CertificationResultReporter", "HasCompleteReadings"), Is.True);
        }

        [Test]
        public void ResetClearsAllCertificationState()
        {
            Record(white, 0, 50, 50, "White50_V1-0001.mp4");
            Assert.That((bool)TestReflection.InvokeStatic("SmokeSchoolAppState", "HasAnyCertificationAnswer"), Is.True);
            Reset();
            Assert.That((bool)TestReflection.InvokeStatic("SmokeSchoolAppState", "HasAnyCertificationAnswer"), Is.False);
            Assert.That(OrderedResults().Count, Is.Zero);
        }

        private static void Reset()
        {
            TestReflection.InvokeStatic("SmokeSchoolAppState", "ResetCertificationState");
        }

        private static void Record(object section, int questionIndex, int actual, int answer, string filename)
        {
            TestReflection.InvokeStatic("SmokeSchoolAppState", "RecordCertificationAnswer",
                section, questionIndex, actual, answer, filename);
        }

        private static IList OrderedResults()
        {
            return (IList)TestReflection.InvokeStatic("SmokeSchoolAppState", "GetOrderedCertificationResults");
        }

        private static int GetTotal(object section)
        {
            return (int)TestReflection.InvokeStatic("SmokeSchoolAppState", "GetCertificationTotalScore", section);
        }
    }
}
