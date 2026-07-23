# Smoke School VR Authentication Testing

## Components

- Dashboard API: `POST /api/vr/login`
- Browser auth UI and Unity startup gate: `Chemney_VR/Assets/WebGLTemplates/WebXR2020/index.html`
- Endpoint configuration: `Chemney_VR/Assets/WebGLTemplates/WebXR2020/auth-config.js`
- Unity approval receiver: `LoginPanel.CompleteApprovedLogin`

The browser renders standard email/password inputs first, which keeps desktop and headset keyboards available. It posts to the configured dashboard endpoint and does not append the Unity loader or call `createUnityInstance` until the response is approved. The password is cleared after every request and never crosses `SendMessage`; only a sanitized approved payload containing `sessionReference` and canonical student fields enters Unity.

Authorization is not written to browser storage or `PlayerPrefs`. A scene reload in the same Unity instance can retain the approved identity and correlation fields in static memory, but a full browser reload always starts at the login form.

## Configure A Dev Dashboard

The dashboard must be publicly reachable over HTTPS and its deployment environment must include the exact VR site origin in `SMOKESCHOOL_VR_ORIGINS`.

Set the default endpoint in `auth-config.js`, or override it without rebuilding:

```text
https://YOUR-VR-SITE.netlify.app/?authApi=https%3A%2F%2FYOUR-DASHBOARD%2Fapi%2Fvr%2Flogin
```

Do not commit temporary tunnel URLs to `auth-config.js`; use the `authApi` query override for local or tunnel testing.

An approved response must have this shape:

```json
{
  "approved": true,
  "reason": null,
  "sessionReference": "opaque-correlation-reference",
  "student": {
    "userId": "user-id",
    "email": "student@example.com",
    "displayName": "Student Name",
    "company": "Company Name",
    "expiresAt": "2026-12-31T23:59:59.000Z"
  }
}
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

1. Before approval, DevTools shows no Unity loader, data, framework, or WASM request and the VR/AR controls are hidden.
2. Correct email/password starts Unity and displays the canonical dashboard profile.
3. Wrong credentials keep Unity unloaded and display the invalid-credentials message.
4. Revoked, inactive, and expired access each display their friendly denial message and keep Unity unloaded.
5. Rate limiting displays a retry message and keeps Unity unloaded.
6. Service or malformed-response failures display a temporary-service message and keep Unity unloaded.
7. The password input is empty after every request, including denied and failed requests.
8. Refresh requires browser authentication again; no authorization persists.
9. A successful `SendMessage` payload contains approved identity/correlation fields and no password.
10. The Quest browser enters WebXR after approval and clear-video playback remains unchanged.
