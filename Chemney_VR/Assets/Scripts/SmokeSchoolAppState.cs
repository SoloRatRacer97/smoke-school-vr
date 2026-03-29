using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class SmokeSchoolAppState
{
    public enum SmokeSection
    {
        White,
        Black
    }

    public struct QuestionResult
    {
        public int questionNumber;
        public SmokeSection smokeSection;
        public string videoFilename;
        public int actualOpacity;
        public int studentAnswer;
        public int deviation;
        public bool answered;

        public string SmokeColorLabel => smokeSection == SmokeSection.White ? "White" : "Black";
    }

    private static readonly Dictionary<SmokeSection, Dictionary<int, QuestionResult>> certificationResults =
        new Dictionary<SmokeSection, Dictionary<int, QuestionResult>>
        {
            { SmokeSection.White, new Dictionary<int, QuestionResult>() },
            { SmokeSection.Black, new Dictionary<int, QuestionResult>() }
        };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnDomainReload()
    {
        ResetCertificationState();
    }

    public static void ResetCertificationState()
    {
        certificationResults[SmokeSection.White].Clear();
        certificationResults[SmokeSection.Black].Clear();
    }

    public static void RecordCertificationAnswer(
        SmokeSection smokeSection,
        int questionIndex,
        int actualOpacity,
        int studentAnswer,
        string videoFilename)
    {
        certificationResults[smokeSection][questionIndex] = new QuestionResult
        {
            questionNumber = questionIndex + 1,
            smokeSection = smokeSection,
            videoFilename = videoFilename ?? string.Empty,
            actualOpacity = actualOpacity,
            studentAnswer = studentAnswer,
            deviation = Mathf.Abs(studentAnswer - actualOpacity) / 5,
            answered = true
        };
    }

    public static bool TryGetCertificationResult(SmokeSection smokeSection, int questionIndex, out QuestionResult result)
    {
        return certificationResults[smokeSection].TryGetValue(questionIndex, out result) && result.answered;
    }

    public static int GetCertificationTotalScore(SmokeSection smokeSection)
    {
        return certificationResults[smokeSection].Values.Where(result => result.answered).Sum(result => result.deviation);
    }

    public static bool HasAnyCertificationAnswer()
    {
        return certificationResults.Values.Any(sectionResults => sectionResults.Values.Any(result => result.answered));
    }

    public static List<QuestionResult> GetOrderedCertificationResults()
    {
        return certificationResults.Values
            .SelectMany(sectionResults => sectionResults.Values)
            .Where(result => result.answered)
            .OrderBy(result => result.smokeSection)
            .ThenBy(result => result.questionNumber)
            .ToList();
    }

    public static List<QuestionResult> GetCertificationFailures(int maxDeviationScore)
    {
        return GetOrderedCertificationResults()
            .Where(result => result.deviation > maxDeviationScore)
            .ToList();
    }
}
