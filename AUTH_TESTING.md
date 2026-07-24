# Smoke School VR Authentication Testing

## Components

- Dashboard API: `POST /api/vr/login`
- Certification result API: `POST /api/vr/certification-attempts`
- Unity auth UI: `LoginPanel` in `Chemney_VR/Assets/Scenes/ChimneyScene.unity`
- WebGL keyboard bridge: `Chemney_VR/Assets/WebGLTemplates/WebXR2020/index.html`
- Endpoint configuration: `Chemney_VR/Assets/WebGLTemplates/WebXR2020/auth-config.js`
- Unity authentication component: `DataInput_Fields`

Unity loads immediately and its C# `DataInput_Fields` component authenticates with the dashboard. The WebGL overlay only supplies browser keyboard input to the in-scene controls. Passwords are cleared after each request and are never stored in `PlayerPrefs`.

## Configure A Dev Dashboard

The dashboard must be publicly reachable over HTTPS and its deployment environment must include the exact VR site origin in `SMOKESCHOOL_VR_ORIGINS`.

Set the default endpoint in `auth-config.js`, or override it without rebuilding:

```text
https://YOUR-VR-SITE.netlify.app/?authApi=https%3A%2F%2FYOUR-DASHBOARD%2Fapi%2Fvr%2Flogin
```

## Source Verification

```bash
node scripts/verify-auth-source.mjs
node scripts/test-vr-regressions.mjs --source-only
```

## Build

```bash
"/Applications/Unity/Hub/Editor/6000.0.62f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -quit \
  -projectPath "$PWD/Chemney_VR" \
  -executeMethod CommandLineMainStockWebXRBuild.Build \
  -logFile "$PWD/Chemney_VR/main-auth-webxr-build.log"
```

Verify the generated output:

```bash
node scripts/verify-auth-source.mjs "Chemney_VR/VR Smoke School Stock WebXR"
```

Run the full drift regression, including clear-video source hashes, card labels,
authentication behavior, Cloudinary mappings, Brotli build integrity, and
Netlify headers:

```bash
node scripts/test-vr-regressions.mjs "Chemney_VR/VR Smoke School Stock WebXR"
```

Optionally verify every mapped Cloudinary URL over the network:

```bash
node scripts/test-vr-regressions.mjs "Chemney_VR/VR Smoke School Stock WebXR" --network
```

## End-To-End Cases

1. Correct email/password advances the Unity scene and displays the canonical dashboard profile.
2. Wrong password keeps the Unity login panel visible and displays a generic error.
3. Expired access displays an expiration message.
4. Revoked/inactive access displays an inactive message.
5. The sixth failed attempt returns the dashboard throttle message and honors `Retry-After`.
6. Refresh returns to the Unity login panel; no password persists.
7. The Quest browser enters WebXR and clear-video playback remains unchanged.
8. An approved login retains `certificationNumber`, `sessionReference`, and `resultToken` in memory; `userId` is used only when `certificationNumber` is absent.
9. Completing a certification submits one attempt containing the login `resultToken`, a UUID attempt ID, run/timestamps/version fields, `epa-method-9-v1`, and exactly 25 ordered White plus 25 ordered Black readings.
10. Each submitted reading contains its 1-based question number, assigned video filename, actual opacity, and selected opacity matching the final Unity result tables.
11. A pass and a threshold/individual-reading failure both persist the dashboard-calculated result without changing the Unity result shown to the student.
12. Reopening the final result or pressing End Test while a request is active does not create a simultaneous second request.
13. Force the first result request to fail, then restore the API and press End Test again; the retry uses the same `attemptId`, succeeds, and only then reloads the scene.
14. Return `{ok:true, duplicate:true}` for the retry and confirm Unity accepts it when the response `attemptId` matches.
15. Start a retake after a failed run and confirm the new run receives a new `attemptId` while the approved in-memory login remains available.
16. Confirm neither `resultToken` nor `sessionReference` appears in `PlayerPrefs`, and a full page refresh requires login again.

The Unity login is the development MVP. Before a production security release, protect build/video delivery with a short-lived signed grant at the hosting edge.
