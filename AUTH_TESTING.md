# Smoke School VR Authentication Testing

## Components

- Dashboard API: `POST /api/vr/login`
- WebXR auth UI: `Chemney_VR/Assets/WebGLTemplates/WebXR2020/index.html`
- Endpoint configuration: `Chemney_VR/Assets/WebGLTemplates/WebXR2020/auth-config.js`
- Unity approval receiver: `LoginPanel.CompleteApprovedLogin`

Unity does not begin downloading until the dashboard approves the email and password. The password is cleared immediately after each request and is never sent to Unity or stored in `PlayerPrefs`.

## Configure A Dev Dashboard

The dashboard must be publicly reachable over HTTPS and its deployment environment must include the exact VR site origin in `SMOKESCHOOL_VR_ORIGINS`.

Set the default endpoint in `auth-config.js`, or override it without rebuilding:

```text
https://YOUR-VR-SITE.netlify.app/?authApi=https%3A%2F%2FYOUR-DASHBOARD%2Fapi%2Fvr%2Flogin
```

## Source Verification

```bash
node scripts/verify-auth-source.mjs
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

## End-To-End Cases

1. Correct email/password loads Unity and displays the canonical dashboard profile.
2. Wrong password leaves Unity unloaded and displays a generic error.
3. Expired access displays an expiration message.
4. Revoked/inactive access displays an inactive message.
5. The sixth failed attempt returns the dashboard throttle message and honors `Retry-After`.
6. Refresh requires authentication again; no password persists.
7. The Quest browser enters WebXR and clear-video playback remains unchanged.

The HTML gate is the dev MVP. Before a production security release, protect build/video delivery with a short-lived signed grant at the hosting edge.
