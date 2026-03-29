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

### Build & Deploy Checklist

**Before building:**
1. Make sure ALL `.cs` files compile in Unity (check Console for red errors)
2. Key files that must stay in sync:
   - `Assets/ManagerTesting.cs` — main test controller
   - `Assets/Scripts/SmokeSchoolAppState.cs` — certification state (scores, results)
   - `Assets/Scripts/UnityWebRequest.cs` — email pipeline (reads from SmokeSchoolAppState)
3. If you see `SlideRecord` errors → `UnityWebRequest.cs` is outdated (SlideRecord was replaced by SmokeSchoolAppState)
4. If you see `SmokeSchoolAppState` not found → the file is missing from `Assets/Scripts/`

**Building WebGL:**
1. Open `Chemney_VR/` in Unity 6 Editor
2. **File → Build Settings → Platform: WebGL → Build**
3. Select output folder (usually `VR Web Build/` at project root or wherever you've been building)
4. Wait ~8 minutes for full build (incremental builds are faster)
5. Output: `index.html`, `Build/` folder with `.wasm`, `.framework.js`, `.loader.js`, `.data`

**Deploying to Netlify:**
1. Go to Netlify Drop (or your Netlify dashboard)
2. Drag the `VR Web Build/` folder (the one containing `index.html`)
3. Done — site updates in ~30 seconds

### Common Build Issues

| Error | Cause | Fix |
|-------|-------|-----|
| `SmokeSchoolAppState` not found | Missing script file | Pull from GitHub — `Assets/Scripts/SmokeSchoolAppState.cs` |
| `SlideRecord` does not exist | Old UnityWebRequest.cs | Pull latest `UnityWebRequest.cs` from GitHub |
| Build completes instantly (no output) | Compile errors blocking build | Check Unity Console (red errors), fix scripts first |
| Build takes 0 seconds | Unity cached build, nothing changed | Clean build: delete `Library/` folder, reopen project |

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

## Pushing to GitHub

⚠️ **Do NOT use `git push` directly** — the repo is ~1.5GB due to tracked Unity `Library/` files in history. Normal pushes will timeout with HTTP 408.

**Use the GitHub Contents API instead:**
```bash
# 1. Get your GitHub token from keychain
TOKEN=$(printf "protocol=https\nhost=github.com\n" | git credential-osxkeychain get 2>/dev/null | grep password | cut -d= -f2)

# 2. Get the current file SHA from GitHub
SHA=$(curl -s -H "Authorization: Bearer $TOKEN" \
  "https://api.github.com/repos/SoloRatRacer97/smoke-school-vr/contents/Chemney_VR/Assets/ManagerTesting.cs" \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['sha'])")

# 3. Push the file via API
curl -s -X PUT -H "Authorization: Bearer $TOKEN" \
  "https://api.github.com/repos/SoloRatRacer97/smoke-school-vr/contents/Chemney_VR/Assets/ManagerTesting.cs" \
  -d "$(python3 -c "
import json, base64
with open('Chemney_VR/Assets/ManagerTesting.cs', 'rb') as f:
    content = base64.b64encode(f.read()).decode()
print(json.dumps({
    'message': 'Your commit message here',
    'content': content,
    'sha': '$SHA',
    'branch': 'main'
}))
")"
```

**For multiple files**, repeat steps 2-3 for each file path.

**Long-term fix:** Remove `Library/` from git history with `git filter-repo` to shrink the repo, then normal pushes will work again.

## Recent Changes

- `02ca2e07` — Fix scratch/refresh state management and button layout
- `e3b864b5` — Re-enable scratch/refresh + fix slide counter on rereads
- `469e2ae` — EPA 15% individual fail gate, Q#1 label fix, identity persistence
- `663c074` — Slide data tracking + HTML email with ALT-152 audit table
- `724fd8e` — Separate white/black scoring thresholds, dev email swap
- `77e028b` — Pass/fail threshold fix, email gating, student name in emails

## License

Proprietary — Smoke School Inc.
