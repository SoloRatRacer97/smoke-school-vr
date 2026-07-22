# Smoke School VR Version Manifest

## Working Clear-Video Baseline

- Version flag: `WORKING_CLEAR_VIDEO`
- EPA status: Not the declined legacy build
- Headset verified: Yes
- Verified artifact: `VR Smoke School Stock WebXR`
- Artifact date: 2026-06-22
- Unity editor: `6000.0.62f1`
- Production scene: `Assets/Scenes/ChimneyScene.unity`
- WebGL template: `PROJECT:WebXR2020`
- Build method: `CommandLineMainStockWebXRBuild.Build`
- Video catalog: `Assets/Scripts/SmokeVideoURLData.asset`
- Video host: Cloudinary

### Clear-Video Source Path

- `Assets/ManagerTesting.cs`
- `Assets/Scripts/SmokeVideoDirectDisplay.cs`
- `Assets/XR/Settings/WebXRSettings.asset`
- `Assets/Video_player.renderTexture`
- `Assets/Video_playerBlackSmoke.renderTexture`
- `Assets/Scenes/ChimneyScene.unity`
- `Assets/Editor/CommandLineMainStockWebXRBuild.cs`

### Verified Artifact SHA-256

```text
cd963bef325f99727e01b6c07be8d27c481c7626c33b7be58221f7ce415f57fa  VR Smoke School Stock WebXR.data.br
7898b589b3507780b49cc209212966922d10c9d29e320ac085070b2679d052b0  VR Smoke School Stock WebXR.framework.js.br
0708ad8a2bbf21cfdd9b45ff1592b72716af97640e6488554a124d89a5328c26  VR Smoke School Stock WebXR.wasm.br
0a4507e84e5fb06cf8506197d9e7fee67f3c56ba770789b530049bd4f3860a29  VR Smoke School Stock WebXR.loader.js
```

## Historical Anchors

- `archive/epa-declined-candidate-2026-02-09`: Candidate legacy snapshot only; do not deploy without confirmation.
- `archive/local-complete-webgl-2026-06-19`: Complete pre-clear-video local WebGL snapshot.
- `archive/origin-incomplete-webgl-2026-06-19`: Incomplete remote deploy snapshot; archive only.

## Release Rules

- Do not rewrite or force-push tagged release history.
- Do not commit Unity `Library`, `Temp`, `Logs`, or `UserSettings` output.
- Keep diagnostic scenes and media-layer experiments out of production release commits.
- Store deployable WebGL ZIP files in GitHub Releases with checksums.
