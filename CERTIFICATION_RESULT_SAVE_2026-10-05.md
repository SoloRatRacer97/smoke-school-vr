# Certification results save on signature submit — October 5, 2026

## Summary

Certification results now save to the dashboard the moment the student submits their signature, for both passed and failed runs. Before this change, no result had ever reached the dashboard database.

## Problem

The dashboard (`SoloRatRacer97/smokeschool-dashboard`) shows each user's Certification Status, attempt count and attempt history from Supabase. On October 5, 2026 the live database showed:

- VR logins were recorded normally (latest: October 4, 2026).
- `certification_attempts` had **0 rows**; every `access_records` row had `has_passed = false` and `certification_attempt_count = 0`.
- The live endpoint `POST https://smokeschool-dashboard.vercel.app/api/vr/certification-attempts` was reachable, and CORS allowed `https://heroic-malasada-c99a1b.netlify.app`.

The dashboard was displaying the database correctly. The VR app was not sending results.

## Root cause

When result persistence was added (`a1d37a58 feat(certification): persist VR test results`), `ManagerTesting.ShowingFinalResult()` started `CertificationResultReporter.Submit` as soon as the final result appeared, for passes and failures.

The completion-flow rework that followed (`07925393` through `df9a28c2`) removed that call. After it:

1. **Failed runs were never sent.** `StartEndTestFlow` routes a failure directly to `StartWhitePracticeRetake`, which reloads the scene without submitting.
2. **Passed runs were sent only when End Test was pressed** (`CompleteEndTest`). A student who removed the headset or closed the browser on the result screen left no record.
3. **Failures were silent.** A rejected submission only wrote to the developer log.

## The fix

### When results are sent

The signature panel's Submit button calls `ManagerTesting.ShowingFinalResult()`; this is a persistent call in `ChimneyScene.unity` alongside `EmailTrigger.OnSendEmail`. `ShowingFinalResult()` now calls:

```csharp
CertificationResultReporter.StartSubmission(testRunNumber);
```

The call runs after scoring, for both passed and failed results, whenever all 50 readings (25 White + 25 Black) are complete. Incomplete runs are not sent, as before; the dashboard API would reject them.

### End Test is now the retry

`CompleteEndTest` is unchanged. If the signature-time submission already succeeded, `Submit` returns immediately and the scene reloads. If a submission is still running, it waits for it. If the submission failed, End Test sends it again with the **same `attemptId`**, and the dashboard treats the repeat as a duplicate instead of a second attempt.

### Surviving a Retake

A failed student can press Retake immediately, which reloads the scene. Coroutines on `ManagerTesting` stop when the scene unloads, which would abort a request still in flight. `CertificationResultReporter` now runs submissions on a hidden `DontDestroyOnLoad` object (`CertificationResultSubmission`), so the request completes across the reload.

The retake's `ManagerTesting.Start()` calls `BeginNewRun()`, which issues a new `attemptId`. The in-flight submission keeps its own `runAttemptId` and only updates `hasSucceeded` / `isSubmitting` if it still belongs to the current run, so a late response cannot overwrite the new run's state. The readings are value-type structs copied into the payload before sending, so `ResetCertificationState()` cannot alter them.

### Unchanged

- Payload format, `epa-method-9-v1` rules version, and the 50-reading validation.
- Local verification of the dashboard's authoritative score (White/Black score, individual failure count, pass/fail).
- The result token from login (8-hour lifetime) and the endpoint derived from `auth-config.js`.
- The pass/fail routing, End Test / Retake buttons, email, signature panel and all 1,260 smoke-video mappings.

## Files changed

| File | Change |
|------|--------|
| `Chemney_VR/Assets/ManagerTesting.cs` | `ShowingFinalResult()` starts the submission after scoring |
| `Chemney_VR/Assets/Scripts/CertificationResultReporter.cs` | `StartSubmission()` with a persistent runner; per-run attempt ID guards |
| `scripts/test-vr-regressions.mjs` | Locked SHA-256 for `ManagerTesting.cs` updated to `317ee133…455517fb` |

## Data flow

```
Signature Submit button
  └─ ManagerTesting.ShowingFinalResult()
       └─ CertificationResultReporter.StartSubmission(run)
            └─ POST /api/vr/certification-attempts   (resultToken, attemptId, 50 readings)
                 └─ Supabase record_certification_attempt()
                      ├─ inserts certification_attempts row
                      └─ updates access_records: has_passed, passed_at,
                         certification_attempt_count, latest_certification_result
                           └─ Dashboard: Certification Status column + Attempt History
```

## Validation (October 5, 2026)

- Unity `6000.0.62f1` WebXR build: `Build Finished, Result: Success`, 0 compile errors.
- `node scripts/verify-auth-source.mjs "Chemney_VR/VR Smoke School Stock WebXR"`: source and output verified.
- `node scripts/test-vr-regressions.mjs "Chemney_VR/VR Smoke School Stock WebXR"`: passed (13 locked files, 1,260 Cloudinary mappings, build integrity).
- `scripts/test-webxr-playwright.mjs --tutorial`: login and tutorial playback passed.

**Not yet validated:** the browser test does not run a full 50-reading exam. Confirm on the Quest after deploying:

1. Log in, complete a full test, and submit the signature.
2. In the dashboard, the user's Certification Status changes from "Not Attempted" and the attempt appears in Attempt History.
3. Fail a run and press Retake immediately; that attempt still appears as "Not Passed".

## Deployment package

`~/Desktop/UPLOAD-THIS-Smoke-School-VR-Netlify-SIGNATURE-SAVE-20261005.zip` (17 files)

SHA-256: `4e237d216c65682010911776f88fe99be1a42951ef65b66f37c13c94b223c000`

Upload to the Netlify project `heroic-malasada-c99a1b` from its Deploys page. The package also contains the September 14 tutorial playback fix (`TUTORIAL_UPDATE_2026-09-14.md`). No dashboard change is required.
