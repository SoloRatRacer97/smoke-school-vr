# Smoke School VR Active Runtime Path

This document identifies the currently active production path and cleanup boundaries for the Unity project at `Chemney_VR/`.

## Active Build Path

- Unity version: `6000.0.62f1`
- Enabled build scene: `Chemney_VR/Assets/Scenes/ChimneyScene.unity`
- WebGL template: `Chemney_VR/Assets/WebGLTemplates/WebXR2020/index.html`
- Manual WebGL output folder: `Chemney_VR/VR Smoke School/`
- Required Netlify header file: `Chemney_VR/VR Smoke School/_headers`

## Active Runtime Scripts

- `Chemney_VR/Assets/ManagerTesting.cs`: primary certification/practice flow controller.
- `Chemney_VR/Assets/Scripts/SmokeSchoolAppState.cs`: static certification results and audit state.
- `Chemney_VR/Assets/Scripts/SmokeVideoURLData.cs`: ScriptableObject schema for smoke video URL groups.
- `Chemney_VR/Assets/Scripts/SmokeVideoURLData.asset`: active Cloudinary URL data for certification videos.
- `Chemney_VR/Assets/Scripts/DataInput_Fields.cs`: login/name/email UI and persisted session identity.
- `Chemney_VR/Assets/Scripts/UnityWebRequest.cs`: `ScreenshotSender` email/certificate screenshot pipeline.
- `Chemney_VR/Assets/Scripts/SimpleVideoPlayer.cs`: tutorial video panel playback.
- `Chemney_VR/Assets/Scripts/ManageWhitePracticeTest.cs`: white tutorial/practice panel transition helper.
- `Chemney_VR/Assets/Scripts/MangerBlackPractice.cs`: black tutorial/practice panel transition helper.

## Legacy Or Quarantine Candidates

These appear to be older implementations or disabled-scene paths. Do not delete without checking scene references in Unity first.

- `Chemney_VR/Assets/NewPracticeManager.cs`
- `Chemney_VR/Assets/Scripts/SmokeTestManager.cs`
- `Chemney_VR/Assets/Scripts/PracticeTestManager.cs`
- `Chemney_VR/Assets/Scripts/WhiteSmokeTestManager.cs`
- `Chemney_VR/Assets/ScoreManager.cs`
- `Chemney_VR/Assets/VideoLoader.cs`
- `Chemney_VR/Assets/Scenes/OnlyUiSmokeSchool.unity`
- `Chemney_VR/Assets/Scenes/NEWAPPROACH.unity`
- `Chemney_VR/Assets/Scenes/SampleScene.unity`

## Cloudinary Video Quality Notes

Certification videos are not pulled from an API at runtime. They are stored as direct Cloudinary URLs in `SmokeVideoURLData.asset`, selected by `ManagerTesting.cs`, and assigned directly to `VideoPlayer.url`.

The current URL set does not request Cloudinary transformations such as `q_auto`, `f_auto`, `w_`, or `h_`. Sampled certification clips are already served as 4K H.264 MP4s, typically around 20-31 Mbps. If the videos look pixelated, the likely first bottlenecks are Unity/WebGL display resolution and video panel sizing, not the Cloudinary source URLs.

Relevant current display constraints:

- Main white/black smoke video render textures are `2560x1440` with no depth/stencil buffer and generated mips disabled.
- Scene `VideoPlayer` components are URL-driven; `Play On Awake` is disabled to avoid empty-source playback during startup.
- WebGL default canvas size is `960x600`.
- In browser mode the visible video panel is only part of that canvas.

Recommended visual-quality investigation:

1. Test the current build in browser and VR mode separately.
2. Verify the optimized `2560x1440` render texture test in headset before changing Cloudinary URLs.
3. Only consider Cloudinary transformations after confirming the display/canvas path is not the bottleneck.

## Unity Smoke Test Checklist

Use this after code or scene changes:

1. Open Unity Hub.
2. Open `/Users/toddanderson/projects/smoke-school-vr/Chemney_VR` with Unity `6000.0.62f1`.
3. Open `Assets/Scenes/ChimneyScene.unity`.
4. Wait for Unity import/compile to finish.
5. Confirm the Console has no red compile errors.
6. Press Play.
7. Enter a student name and email.
8. Start white practice and answer several questions.
9. Confirm skip and refresh buttons stay hidden.
10. Complete question 25 in practice to verify the end-of-practice flow.
11. Continue into white test and confirm video playback/answers work.
12. Continue into black practice and black test when doing a full certification smoke test.
13. Build WebGL only after Play Mode behaves correctly.

## WebGL Build Checklist

1. Open Build Profiles or Build Settings.
2. Select WebGL.
3. Build to `Chemney_VR/VR Smoke School/`.
4. Confirm `Chemney_VR/VR Smoke School/_headers` still exists.
5. Deploy the folder containing `index.html`, `Build/`, and `_headers` to Netlify.
6. Verify Netlify serves `.br` build assets with `Content-Encoding: br`, not only `application/x-brotli`.
