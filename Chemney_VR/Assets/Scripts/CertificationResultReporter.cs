using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class CertificationResultReporter
{
    private const string RulesVersion = "epa-method-9-v1";
    private const int ExpectedReadingCount = 50;
    private const int ExpectedSectionReadingCount = 25;
    private const int CertificationScoreThreshold = 37;
    private const int IndividualFailureThreshold = 3;

    [Serializable]
    private class CertificationAttemptRequest
    {
        public string resultToken;
        public string attemptId;
        public int runNumber;
        public string startedAt;
        public string completedAt;
        public string rulesVersion;
        public string clientVersion;
        public List<CertificationReading> readings;
    }

    [Serializable]
    private class CertificationReading
    {
        public string section;
        public int questionNumber;
        public string videoId;
        public int actualOpacity;
        public int studentOpacity;
    }

    [Serializable]
    private class CertificationAttemptResponse
    {
        public bool ok;
        public bool duplicate;
        public string attemptId;
        public CertificationAttemptResult result;
    }

    [Serializable]
    private class CertificationAttemptResult
    {
        public bool passed;
        public int whiteScore;
        public int blackScore;
        public int individualFailureCount;
        public int whiteReadingCount;
        public int blackReadingCount;
    }

    private static string attemptId;
    private static string startedAt;
    private static bool isSubmitting;
    private static bool hasSucceeded;

    public static bool IsSubmitting => isSubmitting;
    public static bool HasSucceeded => hasSucceeded;
    public static bool HasCompleteReadings => ValidateReadings(SmokeSchoolAppState.GetOrderedCertificationResults());

    public static void BeginNewRun()
    {
        attemptId = Guid.NewGuid().ToString();
        startedAt = DateTime.UtcNow.ToString("o");
        isSubmitting = false;
        hasSucceeded = false;
    }

    public static IEnumerator Submit(int runNumber)
    {
        if (hasSucceeded || isSubmitting)
        {
            yield break;
        }

        List<SmokeSchoolAppState.QuestionResult> results = SmokeSchoolAppState.GetOrderedCertificationResults();
        if (!ValidateReadings(results))
        {
            Debug.LogError("Certification result persistence skipped: expected questions 1-25 for both White and Black sections.");
            yield break;
        }

        string endpoint = DataInput_Fields.GetCertificationResultUrl();
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(DataInput_Fields.approvedResultToken))
        {
            Debug.LogError("Certification result persistence failed: approved result token or endpoint is unavailable.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(attemptId))
        {
            BeginNewRun();
        }

        isSubmitting = true;
        CertificationAttemptRequest payload = new CertificationAttemptRequest
        {
            resultToken = DataInput_Fields.approvedResultToken,
            attemptId = attemptId,
            runNumber = runNumber,
            startedAt = startedAt,
            completedAt = DateTime.UtcNow.ToString("o"),
            rulesVersion = RulesVersion,
            clientVersion = $"{Application.productName} {Application.version}",
            readings = BuildReadings(results)
        };

        string json = JsonUtility.ToJson(payload);
        using (UnityWebRequest request = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            CertificationAttemptResponse response = null;
            try
            {
                response = JsonUtility.FromJson<CertificationAttemptResponse>(request.downloadHandler.text);
            }
            catch (Exception)
            {
                // The response validation below reports malformed service responses.
            }

            string mismatch = null;
            bool responseMatches = false;
            if (response == null)
            {
                mismatch = "response JSON is malformed";
            }
            else if (!response.ok)
            {
                mismatch = "ok was false or missing";
            }
            else if (response.attemptId != attemptId)
            {
                mismatch = $"attempt ID was {response.attemptId ?? "missing"}; expected {attemptId}";
            }
            else
            {
                responseMatches = ValidateAuthoritativeResult(response.result, request.downloadHandler.text, results, out mismatch);
            }

            hasSucceeded = request.responseCode >= 200 && request.responseCode < 300 && responseMatches;
            if (hasSucceeded)
            {
                Debug.Log($"Certification result persisted for attempt {attemptId} (duplicate: {response.duplicate}).");
            }
            else
            {
                if (!string.IsNullOrEmpty(mismatch))
                {
                    Debug.LogError($"Certification result response mismatch for attempt {attemptId}: {mismatch}");
                }
                Debug.LogError($"Certification result persistence failed for attempt {attemptId} (HTTP {request.responseCode}). The same attempt ID will be reused on retry.");
            }
        }

        isSubmitting = false;
    }

    private static List<CertificationReading> BuildReadings(List<SmokeSchoolAppState.QuestionResult> results)
    {
        List<CertificationReading> readings = new List<CertificationReading>(ExpectedReadingCount);
        foreach (SmokeSchoolAppState.QuestionResult result in results)
        {
            readings.Add(new CertificationReading
            {
                section = result.SmokeColorLabel,
                questionNumber = result.questionNumber,
                videoId = result.videoFilename,
                actualOpacity = result.actualOpacity,
                studentOpacity = result.studentAnswer
            });
        }

        return readings;
    }

    private static bool ValidateAuthoritativeResult(
        CertificationAttemptResult authoritative,
        string responseJson,
        List<SmokeSchoolAppState.QuestionResult> results,
        out string mismatch)
    {
        mismatch = null;
        if (authoritative == null)
        {
            mismatch = "result is missing";
            return false;
        }

        string[] requiredFields =
        {
            "\"passed\"",
            "\"whiteScore\"",
            "\"blackScore\"",
            "\"individualFailureCount\"",
            "\"whiteReadingCount\"",
            "\"blackReadingCount\""
        };
        foreach (string field in requiredFields)
        {
            if (string.IsNullOrEmpty(responseJson) || !responseJson.Contains(field))
            {
                mismatch = $"required result field {field} is missing";
                return false;
            }
        }

        int localWhiteScore = 0;
        int localBlackScore = 0;
        int localIndividualFailureCount = 0;
        foreach (SmokeSchoolAppState.QuestionResult result in results)
        {
            if (result.smokeSection == SmokeSchoolAppState.SmokeSection.White)
            {
                localWhiteScore += result.deviation;
            }
            else
            {
                localBlackScore += result.deviation;
            }

            if (result.deviation > IndividualFailureThreshold)
            {
                localIndividualFailureCount++;
            }
        }

        bool localPassed = localWhiteScore <= CertificationScoreThreshold &&
            localBlackScore <= CertificationScoreThreshold && localIndividualFailureCount == 0;
        if (authoritative.whiteReadingCount != ExpectedSectionReadingCount ||
            authoritative.blackReadingCount != ExpectedSectionReadingCount)
        {
            mismatch = $"reading counts were White {authoritative.whiteReadingCount}, Black {authoritative.blackReadingCount}; expected 25 each";
            return false;
        }

        if (authoritative.whiteScore != localWhiteScore || authoritative.blackScore != localBlackScore)
        {
            mismatch = $"scores were White {authoritative.whiteScore}, Black {authoritative.blackScore}; expected White {localWhiteScore}, Black {localBlackScore}";
            return false;
        }

        if (authoritative.individualFailureCount != localIndividualFailureCount)
        {
            mismatch = $"individual failure count was {authoritative.individualFailureCount}; expected {localIndividualFailureCount}";
            return false;
        }

        if (authoritative.passed != localPassed)
        {
            mismatch = $"passed was {authoritative.passed}; expected {localPassed}";
            return false;
        }

        return true;
    }

    private static bool ValidateReadings(List<SmokeSchoolAppState.QuestionResult> results)
    {
        if (results.Count != ExpectedReadingCount)
        {
            return false;
        }

        for (int i = 0; i < 25; i++)
        {
            SmokeSchoolAppState.QuestionResult white = results[i];
            SmokeSchoolAppState.QuestionResult black = results[i + 25];
            if (white.smokeSection != SmokeSchoolAppState.SmokeSection.White || white.questionNumber != i + 1 ||
                black.smokeSection != SmokeSchoolAppState.SmokeSection.Black || black.questionNumber != i + 1)
            {
                return false;
            }
        }

        return true;
    }
}
