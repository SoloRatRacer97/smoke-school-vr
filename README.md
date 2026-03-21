# Smoke School VR — EPA Method 9 Certification

Virtual reality smoke opacity observer certification tool built with Unity 6. Compliant with EPA Method 9 and ALT-152 requirements for VR-based certification programs.

## What It Does

Presents observers with 50 smoke plume videos (25 white, 25 black) at randomized opacities in 5% increments. Observers estimate opacity for each reading. Scoring follows EPA Method 9 Section 3.1:

- **Aggregate threshold:** ≤37 total deviations per color to pass
- **Individual fail gate:** Any single reading with >15% error = automatic fail
- **Audit trail:** All 50 readings logged with video identifiers, actual opacity, student answer, and deviation — sent via HTML email

## Tech Stack

- **Engine:** Unity 6 (URP) with Meta XR + WebXR support
- **Video Hosting:** Cloudinary (smoke plume clips)
- **Email:** Cloudflare Worker → SendGrid (`smokeschoolvr.piper-386.workers.dev`)
- **Hosting:** Netlify (WebGL build, manual deploy)

## Project Structure

```
Chemney_VR/
├── Assets/
│   ├── ManagerTesting.cs          # Main test controller (scoring, flow, slide tracking)
│   ├── Scripts/
│   │   ├── UnityWebRequest.cs     # Email sending + HTML template
│   │   ├── DataInput_Fields.cs    # Student identity management
│   │   └── SmokeVideoURLData.cs   # Video URL data structure
│   ├── Scenes/
│   │   └── ChimneyScene.unity     # Active scene
│   └── ...
├── WebGLBuild/
│   └── VR Web Build/              # Compiled WebGL output (deploy this folder)
└── ProjectSettings/
```

## Key Files

| File | Purpose |
|------|---------|
| `ManagerTesting.cs` | Test flow, scoring (white/black separate), 15% fail gate, slide data collection, question randomization |
| `UnityWebRequest.cs` | Email pipeline — builds HTML audit table with all 50 readings, sends to student + CC |
| `DataInput_Fields.cs` | Player email/name management, WebGL identity persistence across retries |
| `SmokeVideoURLData.cs` | ScriptableObject for smoke video URLs by color/opacity |

## EPA Method 9 Compliance

### Implemented ✅
- 25 white + 25 black smoke observations
- 5% opacity increments (0-100%)
- Randomized presentation order within each color
- Separate white/black scoring (≤37 deviations each)
- Individual 15% fail gate (deviation >3 = auto-fail)
- HTML email with full ALT-152 audit table (question #, color, video ID, actual %, student %, deviation)
- Deviations >3 highlighted in red
- Student name and email on certificate
- Email gated on pass only (no certificate sent on fail)
- Username/email persistence across retries

### Remaining
- Smokeschoolinc.com branding on in-app results panel (requires Unity Editor)
- Certifying official digital signature on certificate
- Test run numbering for retake tracking
- 6-month certification expiration display

## Deployment

### Current Setup (Netlify — Manual Deploy)
The WebGL build is deployed via Netlify Drop (drag-and-drop). To update:

1. Open project in Unity 6 Editor
2. **File → Build Settings → WebGL → Build**
3. Output goes to `Chemney_VR/WebGLBuild/VR Web Build/`
4. Deploy the `VR Web Build` folder to Netlify

### Connecting to GitHub (Recommended)
To enable auto-deploy on push:

1. In Netlify dashboard → **Site settings → Build & deploy → Link to Git**
2. Connect the `SoloRatRacer97/smoke-school-vr` repository
3. Set publish directory to `Chemney_VR/WebGLBuild/VR Web Build`
4. Note: This deploys the pre-built WebGL output. Unity builds still need to be done locally and committed.

## Email Configuration

| Setting | Dev | Production |
|---------|-----|------------|
| CC Email | `todd@cascadewebsolutions.co` | `piper@smokeschoolinc.com` |
| Student Email | `webgl@test.com` (WebGL default) | Student's real email |
| Sender | `info@piperhale.com` | `info@piperhale.com` |
| Worker | `smokeschoolvr.piper-386.workers.dev` | Same |

To swap between dev/prod, edit `ccEmail` in `UnityWebRequest.cs`.

## Development

### Prerequisites
- Unity 6 (6000.x) with WebGL Build Support module
- Meta XR SDK (for VR testing)

### Building
```bash
# Open in Unity Editor
# File → Build Settings → WebGL → Build
# Output: Chemney_VR/WebGLBuild/VR Web Build/
```

### Testing Email Pipeline
```bash
# Test directly against Cloudflare Worker (no Unity required)
curl -X POST https://smokeschoolvr.piper-386.workers.dev/ \
  -H "Content-Type: application/json" \
  -d '{"to":"your@email.com","subject":"Test","html":"<h1>Test</h1>"}'
```

## Recent Changes

- `469e2ae` — EPA 15% individual fail gate, Q#1 label fix, identity persistence
- `663c074` — Slide data tracking + HTML email with ALT-152 audit table
- `724fd8e` — Separate white/black scoring thresholds, dev email swap
- `77e028b` — Pass/fail threshold fix, email gating, student name in emails

## License

Proprietary — Smoke School Inc.
