# Smoke School Unity Tests

Run the Edit Mode suite with Unity 6000.0.62f1:

```bash
"/Applications/Unity/Hub/Editor/6000.0.62f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -nographics \
  -projectPath "/Users/toddanderson/projects/smoke-school-vr-certification/Chemney_VR" \
  -runTests \
  -testPlatform EditMode \
  -testFilter "SmokeSchool.Tests" \
  -testResults "/tmp/smoke-school-editmode.xml" \
  -logFile "/tmp/smoke-school-editmode.log"
```

Do not add `-quit`; Unity Test Framework exits after writing the results.

## Catalog Status

The White and Black Cloudinary catalogs each contain 630 unique, correctly classified, production-transformed URLs. All tests are expected to pass.
