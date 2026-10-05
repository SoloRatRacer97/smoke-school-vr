# Tutorial release and playback fix — September 14, 2026

## Current package

`/Users/toddanderson/Desktop/UPLOAD-THIS-Smoke-School-VR-Netlify-PLAYBACK-FIX-20260914.zip`

SHA-256: `7f019027924ae15545104a314b1f7aa9e1348d49fbd375fb294f47a88473d294`

Upload to the existing Netlify project `heroic-malasada-c99a1b` using its Deploys page. The previous tutorial-only ZIP was deployed there but its Start Tutorial action opens a black video screen. This replacement has been built and tested locally; it has not been deployed yet.

## Source and fix

Unity project: `/Users/toddanderson/projects/smoke-school-vr-f8fe34b9-clean/Chemney_VR`; base commit `f8fe34b9`; Unity `6000.0.62f1`.

The scene tutorial URL is `https://res.cloudinary.com/dkzd0f0tu/video/upload/v1789398963/Smoke_School_Intro_copy_ndfg6o.mov`. Its opacity diagram correction was verified around 01:30 against the supplied Mega original.

The loader previously called Prepare during startup while its player was inactive. Start Tutorial activated the player but did not call Prepare again. The fix defers startup preparation while inactive and adds ReplayVideo immediately after the Start Tutorial button activates the player. The Coming Soon banner, email flow, authentication endpoint, certification logic, and 1,260 smoke-video mappings are unchanged.

## Validation

- Unity build, authentication source/output checks, and full source/build regression suite passed.
- Real login on the existing Netlify deployment passed with the user-provided test account.
- The rebuilt browser test uses the existing isolated authentication fixture, then loads the actual Cloudinary tutorial. Video frames advance; 1920×1080, duration 681.728 seconds, unpaused, unmuted, and no media error.
- The previous ZIP fails that same tutorial playback test.
- Rendered video screenshot: `/private/tmp/smoke-school-fixed-e2e-tutorial.png`.
- Headset playback/audio and a complete certification save still require validation.
- Archive integrity passed; its 17 files match the built output, and index.html, auth-config.js, and _headers match the previous ZIP exactly.

Run the browser check:

```sh
NODE_PATH=/Users/toddanderson/projects/smoke-school-dashboard-certification/node_modules node scripts/test-webxr-playwright.mjs "Chemney_VR/VR Smoke School Stock WebXR" /private/tmp/vr-auth.png /private/tmp/vr-login.png --tutorial
```

## Dashboard CORS

Dashboard repository `SoloRatRacer97/smokeschool-dashboard`, production commit `2b37158e631817bed2851d1e07d88a343248cf20`, allows the heroic Netlify origin alongside the existing environment origins. Login and certification preflights and browser responses were verified. Credential emails intentionally continue to link to the previous Netlify site per the user's instruction.
