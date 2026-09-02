# Smoke School VR Authentication Testing

## Components

- Dashboard API: `POST /api/vr/login`
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

The Unity login is the development MVP. Before a production security release, protect build/video delivery with a short-lived signed grant at the hosting edge.
