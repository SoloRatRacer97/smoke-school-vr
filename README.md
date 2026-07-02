# Smoke School VR

Unity WebGL/VR smoke opacity observer certification app for EPA Method 9 style training and testing.

The active production build is the Unity project in `Chemney_VR/` and the Netlify publish folder is `Chemney_VR/VR Smoke School/`.

## Current Production State

This repository currently contains the working Smoke School VR flow with the July 2026 White smoke video replacement and the final preloading/test-start fixes.

Current active runtime path:

| Item | Value |
| --- | --- |
| Unity version | `6000.0.62f1` |
| Active scene | `Chemney_VR/Assets/Scenes/ChimneyScene.unity` |
| Main controller | `Chemney_VR/Assets/ManagerTesting.cs` |
| Video URL data asset | `Chemney_VR/Assets/Scripts/SmokeVideoURLData.asset` |
| WebGL template | `Chemney_VR/Assets/WebGLTemplates/WebXR2020/index.html` |
| WebGL output folder | `Chemney_VR/VR Smoke School/` |
| Netlify publish directory | `Chemney_VR/VR Smoke School` |

## What The App Does

The app guides a student through White smoke practice, White smoke testing, Black smoke practice, and Black smoke testing. It presents smoke plume videos at randomized opacities in 5 percent increments and records the student's opacity estimates.

Certification behavior:

- 25 White smoke certification questions.
- 25 Black smoke certification questions.
- Actual opacities are selected from 0 percent through 100 percent in 5 percent increments.
- A single reading more than 15 percent away from actual opacity is an individual fail gate.
- White and Black certification scores are tracked separately.
- Certification results are sent through the existing Cloudflare Worker and SendGrid email pipeline.

## July 2026 Video Update

The White smoke videos were replaced with new Cloudinary uploads while keeping the current GitHub app/testing interface intact.

What changed:

- `SmokeVideoURLData.asset` now contains 630 transformed White smoke MP4 URLs.
- White smoke has 21 opacity groups: 0, 5, 10, ..., 100.
- Each White opacity group has 30 videos.
- URLs use Cloudinary transformed MP4 delivery:

```text
https://res.cloudinary.com/dkzd0f0tu/video/upload/q_auto:best,f_mp4,vc_h264/v.../WhiteXX_V1-....mp4
```

What did not change:

- Black smoke URLs were intentionally left unchanged.
- The production Unity scene and testing UI are still from the current GitHub app path.
- The experimental direct-quad/media-layer lab scenes are not part of the production runtime.

## Practice/Tutorial White Smoke URLs

The practice review/tutorial panels use separate hardcoded `SimpleVideoPlayer` URL arrays in `ChimneyScene.unity`. These are not driven by `SmokeVideoURLData.asset`.

The following White tutorial/practice opacities were updated to new Cloudinary transformed MP4 URLs:

- White 25 percent
- White 50 percent
- White 75 percent
- White 100 percent

Both duplicate scene arrays were updated. If these ever regress, search `ChimneyScene.unity` for old URL fragments such as `White25_1_taihxu`, `White50_3_eta62k`, `White75_V1-0030_dj7yx7`, or `White100_V1-0022_zquo4p`.

## Video Preloading And Transition Fixes

The app uses `ManagerTesting.cs` for certification/practice question videos and `SimpleVideoPlayer.cs` for the four-button tutorial/practice review panels.

### Main Question Video Preloading

`ManagerTesting.cs` has a tiered preload buffer enabled in the scene:

```text
enablePreloading: 1
preloadBufferSize: 3
```

The current implementation avoids the old pattern where a background player prepared a URL but the visible player still did a cold prepare on slide change.

Current behavior:

- Background preload `VideoPlayer`s prepare upcoming question videos.
- Each preload player renders to a hidden `RenderTexture` with the same resolution and aspect mode as the original visible player.
- When a prepared upcoming video is needed, the UI swaps to the preloaded render texture instead of re-preparing that URL on the visible player.
- Preloaded videos are briefly primed so the first frame reaches the hidden render texture, then paused until promoted.
- Practice/test answer transitions pause the active video rather than stopping it, which avoids a red or blank render-texture flash.
- Cold loads still hard reset the active player before `Prepare()`, which is required when entering a new phase.

Important implementation details:

- `PromotePreloadedVideo()` performs the preloaded player/texture swap.
- `AssignDisplayedTexture()` updates all scene `RawImage`s that were showing the original main render texture.
- `PauseActiveVideo()` is used during normal slide transitions to avoid clearing the render texture.
- `PrepareAndPlayMainVideo()` is used for cold loads and phase starts.

### Deferred Begin Test Fix

The scene's post-practice `Begin Test` path can invoke `WhiteTestStart()` before the testing panel is active. This has caused recurring regressions where the test opens but question 1 is not already playing, or the app sits on an infinite spinner.

The current fix is in `ManagerTesting.cs`:

- `WhiteTestStart()`, `BlackPraticeStart()`, and `BlackTestStart()` call `StartOrDeferTest()`.
- If the manager GameObject is inactive when the button method is invoked, the requested phase is stored.
- `OnEnable()` resumes that deferred phase start.
- `RunDeferredTestStart()` waits one frame, then calls `SkipToTest()` so Unity's `VideoPlayer`, render textures, and UI objects are active before `Prepare()` runs.

Expected behavior after White practice:

1. Finish White practice.
2. Review the practice slides again.
3. Click `Begin Test`.
4. The testing environment opens on question 1.
5. Question 1 video is already preparing/playing automatically; the user should not need to manually click question 1.

## Key Files

| File | Purpose |
| --- | --- |
| `Chemney_VR/Assets/ManagerTesting.cs` | Main practice/test flow, scoring, preload buffer, deferred phase start, question video playback |
| `Chemney_VR/Assets/Scripts/SimpleVideoPlayer.cs` | Four-button tutorial/practice review video player and lightweight URL preloader |
| `Chemney_VR/Assets/Scripts/SmokeVideoURLData.asset` | Active White and Black Cloudinary URL data by opacity |
| `Chemney_VR/Assets/Scripts/SmokeVideoURLData.cs` | ScriptableObject schema for smoke video groups |
| `Chemney_VR/Assets/Scripts/SmokeSchoolAppState.cs` | Certification result state used by scoring and email output |
| `Chemney_VR/Assets/Scripts/UnityWebRequest.cs` | Email/certificate submission pipeline |
| `Chemney_VR/Assets/Scenes/ChimneyScene.unity` | Active production scene and UI wiring |
| `Chemney_VR/Assets/Editor/CommandLineNetlifyWebGLBuild.cs` | Batch-mode WebGL build helper for the normal Netlify output folder |
| `Chemney_VR/VR Smoke School/_headers` | Required Netlify Brotli headers for Unity WebGL `.br` files |

## Build Output Committed To This Repo

The repo deploys a pre-built Unity WebGL output from:

```text
Chemney_VR/VR Smoke School/
```

That folder must contain these items for Netlify or manual upload:

```text
index.html
_headers
Build/
TemplateData/
StreamingAssets/
```

The ZIP uploaded manually to Netlify must have `index.html` at the ZIP root, not nested under another folder.

## Building Locally

Preferred command-line build:

```bash
"/Applications/Unity/Hub/Editor/6000.0.62f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -quit \
  -projectPath "/path/to/smoke-school-vr/Chemney_VR" \
  -executeMethod CommandLineNetlifyWebGLBuild.Build \
  -logFile "/path/to/build.log"
```

The build script:

- Switches to WebGL.
- Builds `Assets/Scenes/ChimneyScene.unity`.
- Outputs to `Chemney_VR/VR Smoke School`.
- Rewrites `_headers` for Netlify Brotli delivery.

Manual Unity build:

1. Open `Chemney_VR/` in Unity `6000.0.62f1`.
2. Open `Assets/Scenes/ChimneyScene.unity`.
3. Select WebGL as the build target.
4. Build to `Chemney_VR/VR Smoke School/`.
5. Confirm `_headers` still exists after the build.

## Creating A Netlify ZIP

From the build output folder:

```bash
cd "Chemney_VR/VR Smoke School"
zip -r -9 "SmokeSchoolVR-Netlify.zip" . -x "*.DS_Store" -x "__MACOSX/*"
```

Verify it before upload:

```bash
unzip -t "SmokeSchoolVR-Netlify.zip"
zipinfo -1 "SmokeSchoolVR-Netlify.zip"
```

The top-level ZIP listing should start with files/folders like:

```text
index.html
TemplateData/
_headers
StreamingAssets/
Build/
```

## Netlify Headers

Unity WebGL emits Brotli-compressed `.br` files. Netlify must serve those files with both the correct MIME type and `Content-Encoding: br`.

Required `_headers` entries:

```text
/Build/VR%20Smoke%20School.wasm.br
  Content-Type: application/wasm
  Content-Encoding: br

/Build/VR%20Smoke%20School.framework.js.br
  Content-Type: application/javascript
  Content-Encoding: br

/Build/VR%20Smoke%20School.data.br
  Content-Type: application/octet-stream
  Content-Encoding: br

/Build/*.data.br
  Content-Encoding: br
  Content-Type: application/octet-stream
/Build/*.wasm.br
  Content-Encoding: br
  Content-Type: application/wasm
/Build/*.js.br
  Content-Encoding: br
  Content-Type: application/javascript
```

Symptom of missing headers:

- Unity loading bar appears but never completes.
- Browser may serve `.br` files as `application/x-brotli` without decoding.

Header check:

```bash
curl -I "https://YOUR-SITE.netlify.app/Build/VR%20Smoke%20School.wasm.br"
```

Expected response includes:

```text
Content-Type: application/wasm
Content-Encoding: br
```

## Smoke Test Checklist

After any code, scene, or URL change:

1. Open the app in browser or Quest browser.
2. Enter student name and email.
3. Start White practice.
4. Confirm question 1 video autoplays.
5. Answer several practice questions.
6. Confirm slide transitions do not flash red or blank.
7. Confirm preloaded transitions do not show a spinner unless a true cold load is happening.
8. Finish White practice.
9. Review the practice slides again.
10. Click `Begin Test`.
11. Confirm White testing opens on question 1 and question 1 video starts automatically.
12. Continue through several White test questions.
13. Continue into Black practice and Black test for a full regression pass.
14. Confirm final scoring and email behavior if validating certification output.

## Troubleshooting

| Symptom | Likely Cause | Fix |
| --- | --- | --- |
| Testing environment opens but question 1 does not autoplay | Phase start ran while test panel was inactive | Check `StartOrDeferTest()`, `OnEnable()`, and `RunDeferredTestStart()` in `ManagerTesting.cs` |
| Infinite spinner on first test question | Active `VideoPlayer` was not reset before a cold phase load | Check `PrepareAndPlayMainVideo()` hard reset before `Prepare()` |
| Spinner flashes between normal slides | Prepared preload was not used, or UI is showing spinner while paused | Check `TryUsePreparedVideo()`, `PromotePreloadedVideo()`, and `Update()` spinner conditions |
| Red flash between practice slides | Visible render texture was cleared during `Stop()` | Normal slide transitions should use `PauseActiveVideo()`, not `Stop()` |
| Video is slightly smaller than the white viewport | Preload player aspect mode does not match original scene player | Check `vp.aspectRatio = videoPlayer.aspectRatio` and recycled player aspect assignment |
| White practice/tutorial 25/50/75/100 use old videos | Those URLs live in scene `SimpleVideoPlayer` arrays, not the data asset | Search `ChimneyScene.unity` for old White URL fragments and replace both duplicate arrays |
| Unity WebGL hangs at loading bar | Missing Netlify Brotli headers | Verify `_headers` in `Chemney_VR/VR Smoke School/` |

## Git And Deploy Notes

Use a clean clone when possible. Unity can dirty tracked logs and generate a large `Library/` folder during builds. Do not commit generated `Library/`, local build logs, `.DS_Store`, or unrelated Unity log churn.

Files that are usually intended for a production update:

- Runtime code changed intentionally, such as `ManagerTesting.cs` or `SimpleVideoPlayer.cs`.
- Scene changes changed intentionally, such as updated practice URLs in `ChimneyScene.unity`.
- Data asset changes changed intentionally, such as `SmokeVideoURLData.asset`.
- WebGL output files under `Chemney_VR/VR Smoke School/` when deploying from GitHub/Netlify.
- Documentation such as this `README.md`.

Files that are usually not intended:

- `Chemney_VR/Library/`
- Unity `Logs/`
- Local `*.log` build outputs
- `.DS_Store`
- Local Netlify state

If normal `git push` times out because of repository size, use `gh` or the GitHub Contents API for the changed files, or push from a fresh clone with only intended files staged.

## Security Notes

Cloudinary API credentials must never be committed. If a Cloudinary secret is pasted into chat, terminals, or logs, rotate it after the work is complete.

## License

Proprietary. Smoke School Inc.
