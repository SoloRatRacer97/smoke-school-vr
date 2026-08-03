#!/usr/bin/env node
import assert from "node:assert/strict";
import { createHash } from "node:crypto";
import { existsSync, readFileSync } from "node:fs";
import path from "node:path";
import { brotliDecompressSync } from "node:zlib";

const root = process.cwd();
const project = path.join(root, "Chemney_VR");
const argumentsList = process.argv.slice(2);
const checkNetwork = argumentsList.includes("--network");
const sourceOnly = argumentsList.includes("--source-only");
const outputArgument = argumentsList.find((value) => !value.startsWith("--"));
const output = outputArgument
  ? path.resolve(outputArgument)
  : path.join(project, "VR Smoke School Stock WebXR");

const lockedSourceHashes = new Map([
  ["Assets/ManagerTesting.cs", "1ee0604302113f9cff4b057fa1ea1badd015c10f9ea22fc4a418df5f47f5f285"],
  ["Assets/Scenes/ChimneyScene.unity", "6877ff20e4d174a037f2907b09846c8fb82b7c110dfaa40fe0eb1a5c0e2d0283"],
  ["Assets/Scripts/DataInput_Fields.cs", "4f80ca0346316b38f5d3b14cbcea3ff6fd60d92ccfc60bf509c3430e854c6c1a"],
  ["Assets/Scripts/SmokeVideoDirectDisplay.cs", "dcff79f489dd5d9cbf0051dc1f92950700eff563ecfd75012189b1a13b0dc2a3"],
  ["Assets/Scripts/SmokeSchoolReturnHome.cs", "bb4e7bd268a5c961185ff9d87f38535725c4bf3fbffe0c28e9514438d89cb24a"],
  ["Assets/Scripts/SmokeVideoURLData.asset", "48b68c7a5835a2374ade71fce0b7d1f5036bef4ed33566a5cd27fe6c716aa8a3"],
  ["Assets/Scripts/SmokeTestManager.cs", "df0e1fc0b13d008292cd32e13d09503f5716639135c8673d262c72e1f4308b5a"],
  ["Assets/Scripts/PracticeTestManager.cs", "57a5d9ce5fe06e9782aa05766e2bb375de8d9db287671919605c294bfdeeb46e"],
  ["Assets/XR/Settings/WebXRSettings.asset", "a27c566910ea2e7284c37c69a694f6af99afcc10ab24d7935550ee49fe421ab0"],
  ["Assets/Video_player.renderTexture", "09c8fd50f76ee05c4d6aece5b2f1f3a0762d1d874f792e2dc3fdb2813dc8d248"],
  ["Assets/Video_playerBlackSmoke.renderTexture", "05faed65f15ef4a934f62f25a85c2b4f1d9b87e1d5d5a7ae8859165df74da7b2"],
]);

function read(relativePath) {
  const filePath = path.join(project, relativePath);
  assert.equal(existsSync(filePath), true, `missing Unity source: ${relativePath}`);
  return readFileSync(filePath);
}

function sha256(buffer) {
  return createHash("sha256").update(buffer).digest("hex");
}

for (const [relativePath, expectedHash] of lockedSourceHashes) {
  assert.equal(sha256(read(relativePath)), expectedHash, `${relativePath} drifted from the headset-verified clear-video baseline`);
}

const videoCatalog = read("Assets/Scripts/SmokeVideoURLData.asset").toString("utf8");
const videoUrls = [...videoCatalog.matchAll(/https:\/\/[^\s]+/g)].map((match) => match[0]);
assert.equal(videoUrls.length, 1260, "the Cloudinary smoke-video mapping count drifted");
for (const value of videoUrls) {
  const url = new URL(value);
  assert.equal(url.hostname, "res.cloudinary.com", `unexpected video host: ${url.hostname}`);
  assert.equal(url.pathname.startsWith("/dkzd0f0tu/video/upload/"), true, `unexpected Cloudinary account or path: ${url.pathname}`);
}

const expectedWhiteCounts = Object.fromEntries(
  Array.from({ length: 21 }, (_, index) => [index * 5, 30]),
);
let percentage = null;
let smokeType = null;
const whiteMappings = [];
const blackMappings = [];
const whiteCounts = {};
for (const line of videoCatalog.split(/\r?\n/)) {
  const percentageMatch = line.match(/^  - percentage: (\d+)/);
  if (percentageMatch) {
    percentage = Number(percentageMatch[1]);
    smokeType = null;
    continue;
  }
  const typeMatch = line.match(/^    - typeName: (\w+)/);
  if (typeMatch) {
    smokeType = typeMatch[1];
    continue;
  }
  const urlMatch = line.match(/^      - (https:\/\/\S+)/);
  if (urlMatch && smokeType === "White") {
    whiteCounts[percentage] = (whiteCounts[percentage] || 0) + 1;
    whiteMappings.push(`${percentage}|${urlMatch[1]}`);
  } else if (urlMatch && smokeType === "Black") {
    blackMappings.push(`${percentage}|${urlMatch[1]}`);
  }
}
assert.deepEqual(whiteCounts, expectedWhiteCounts, "white-smoke URL counts drifted by opacity level");
assert.equal(whiteMappings.length, 630, "white-smoke mapping count drifted");
assert.equal(new Set(whiteMappings).size, 630, "corrected white-smoke mappings must remain unique");
for (const mapping of whiteMappings) {
  assert.match(mapping, /\|https:\/\/res\.cloudinary\.com\/dkzd0f0tu\/video\/upload\/q_auto:best,f_mp4,vc_h264\/v\d+\/White(?:00|05|10|15|20|25|30|35|40|45|50|55|60|65|70|75|80|85|90|95|100)_V1-\d{4}_[^/]+\.mp4$/);
}
assert.equal(
  sha256(Buffer.from(whiteMappings.join("\n"))),
  "eeda7d07e85a3154a7ec0dfae7c7815ac8ef0312c83ef99e15f59c69a530cd51",
  "white-smoke URL order or opacity mapping drifted",
);
assert.equal(blackMappings.length, 630, "black-smoke mapping count drifted");
assert.equal(new Set(blackMappings).size, 630, "integrated black-smoke mappings must remain unique");
for (const mapping of blackMappings) {
  assert.match(mapping, /\|https:\/\/res\.cloudinary\.com\/dkzd0f0tu\/video\/upload\/q_auto:best,f_mp4,vc_h264\/v\d+\/Black(?:00|05|10|15|20|25|30|35|40|45|50|55|60|65|70|75|80|85|90|95|100)_V2-\d{4}_[^/]+\.mp4$/);
}
assert.equal(
  sha256(Buffer.from(blackMappings.join("\n"))),
  "bd2faf2ca0bfff2d0a02509483c53293c606dc1e4f68c9022dfb0d80b1474a98",
  "black-smoke URL order or opacity mapping drifted",
);

if (checkNetwork) {
  const uniqueUrls = [...new Set(videoUrls)];
  const failures = [];
  let nextIndex = 0;
  async function checkUrl(url) {
    let failure = null;
    for (let attempt = 1; attempt <= 3; attempt += 1) {
      try {
        const response = await fetch(url, { method: "HEAD", signal: AbortSignal.timeout(30_000) });
        if (response.ok) return null;
        failure = `${response.status} ${url}`;
      } catch (error) {
        failure = `${error.name} ${url}`;
      }
      if (attempt < 3) await new Promise((resolve) => setTimeout(resolve, attempt * 500));
    }
    return failure;
  }
  async function worker() {
    while (nextIndex < uniqueUrls.length) {
      const url = uniqueUrls[nextIndex++];
      const failure = await checkUrl(url);
      if (failure) failures.push(failure);
    }
  }
  await Promise.all(Array.from({ length: 8 }, worker));
  assert.deepEqual(failures, [], `Cloudinary video availability failures:\n${failures.join("\n")}`);
}

const scene = read("Assets/Scenes/ChimneyScene.unity").toString("utf8");
assert.match(scene, /m_GameObject: \{fileID: 55496161\}[\s\S]{0,180}m_LocalScale: \{x: 1, y: 1, z: 1\}/);
assert.match(scene, /m_Name: Emission Testing Text[\s\S]{0,1800}m_text: Video Tutorials/);
assert.match(scene, /m_Name: Videos Tutorials Text[\s\S]{0,1800}m_text: Emission Testing/);
assert.match(scene, /m_GameObject: \{fileID: 1737756091\}[\s\S]{0,700}m_text: Watch our training video/);
assert.match(scene, /m_GameObject: \{fileID: 1530105658\}[\s\S]{0,700}m_text: Complete the EPA Method 9 certification test/);
assert.match(scene, /m_text: Start Tutorial/);
assert.match(scene, /m_text: Begin Test/);
assert.match(scene, /m_text: Skip optional practice slides/);
assert.match(scene, /m_text: Video Tutorials/);
assert.match(scene, /m_text: Emission Testing/);
assert.match(scene, /m_text: Skip to White Smoke Test/);
assert.match(scene, /m_Name: Shared Return to Home Button/);
assert.match(scene, /m_text: Return to Home/);
assert.match(scene, /m_text: Open Results/);
assert.match(scene, /m_text: Continue to Signature/);
assert.match(scene, /m_Name: Testing Video Indicators Overlay[\s\S]{0,2500}m_OverrideSorting: 1[\s\S]{0,300}m_SortingOrder: 10/);
assert.match(scene, /--- !u!224 &835856472[\s\S]{0,700}m_AnchoredPosition: \{x: 0, y: -265\}/);
assert.match(scene, /--- !u!224 &1480371168[\s\S]{0,700}m_AnchoredPosition: \{x: 0, y: -265\}/);
assert.match(scene, /--- !u!224 &1958279382[\s\S]{0,700}m_AnchoredPosition: \{x: 0, y: -265\}/);
assert.doesNotMatch(scene, /\\x03/);
assert.doesNotMatch(scene, /m_MethodName:\s*\n/);
assert.doesNotMatch(scene, /m_Target: \{fileID: 0\}/);
assert.doesNotMatch(scene, /Open Result Pannel|Signature are required|Continue To|Smoke Testing|User ID|User Email/);
assert.match(scene, /https:\/\/res\.cloudinary\.com\/dkzd0f0tu\/video\/upload\/v1774123829\/Smoke_School_Intro2_lzx9e4\.mov/);

const authSource = read("Assets/Scripts/DataInput_Fields.cs").toString("utf8");
assert.match(authSource, /UnityWebRequest/);
assert.match(authSource, /SetAuthenticationLoading\(true\)/);
assert.match(authSource, /inputStudentID\.text = string\.Empty/);
assert.doesNotMatch(authSource, /warningText\.text = "Checking access/);
assert.match(authSource, /public static string approvedCertificationNumber/);
assert.match(authSource, /public static string approvedSessionReference/);
assert.match(authSource, /public static string approvedResultToken/);
assert.match(authSource, /student\.certificationNumber[\s\S]{0,120}student\.userId/);
assert.match(authSource, /Uri\.TryCreate\(approvedAuthenticationUrl, UriKind\.Absolute/);
assert.match(authSource, /Uri\.UriSchemeHttps[\s\S]{0,120}Uri\.UriSchemeHttp/);
assert.match(authSource, /AbsolutePath != "\/api\/vr\/login"[\s\S]{0,120}AbsolutePath != "\/api\/vr\/login\/"/);
assert.match(authSource, /new UriBuilder\(authenticationUri\)[\s\S]{0,180}Path = "\/api\/vr\/certification-attempts"[\s\S]{0,120}Query = string\.Empty[\s\S]{0,120}Fragment = string\.Empty/);
assert.doesNotMatch(authSource, /approvedAuthenticationUrl\.Replace/);
assert.doesNotMatch(authSource, /PlayerPrefs\.SetString\([^\n]*(resultToken|sessionReference)/);

const resultReporter = read("Assets/Scripts/CertificationResultReporter.cs").toString("utf8");
assert.match(resultReporter, /ExpectedReadingCount = 50/);
assert.match(resultReporter, /if \(hasSucceeded \|\| isSubmitting\)/);
assert.match(resultReporter, /Guid\.NewGuid\(\)/);
assert.match(resultReporter, /resultToken[\s\S]*attemptId[\s\S]*runNumber[\s\S]*startedAt[\s\S]*completedAt[\s\S]*rulesVersion[\s\S]*clientVersion[\s\S]*readings/);
assert.match(resultReporter, /section[\s\S]*questionNumber[\s\S]*videoId[\s\S]*actualOpacity[\s\S]*studentOpacity/);
assert.match(resultReporter, /epa-method-9-v1/);
assert.match(resultReporter, /white\.questionNumber != i \+ 1[\s\S]*black\.questionNumber != i \+ 1/);
assert.match(resultReporter, /authoritative\.whiteReadingCount != ExpectedSectionReadingCount[\s\S]{0,160}authoritative\.blackReadingCount != ExpectedSectionReadingCount/);
assert.match(resultReporter, /authoritative\.whiteScore != localWhiteScore[\s\S]{0,120}authoritative\.blackScore != localBlackScore/);
assert.match(resultReporter, /result\.deviation > IndividualFailureThreshold/);
assert.match(resultReporter, /authoritative\.individualFailureCount != localIndividualFailureCount/);
assert.match(resultReporter, /authoritative\.passed != localPassed/);
assert.match(resultReporter, /requiredFields[\s\S]{0,500}required result field/);
assert.match(resultReporter, /Certification result response mismatch/);

const managerSource = read("Assets/ManagerTesting.cs").toString("utf8");
assert.doesNotMatch(managerSource, /SmokeSchoolTestLayout\.Apply/);
assert.match(managerSource, /Initialize\(videoPlayer, videoPlayer\.transform as RectTransform\)/);
assert.match(managerSource, /CertificationResultReporter\.BeginNewRun\(\)/);
assert.match(managerSource, /ScreenshotSender\.didPass = didPass;[\s\S]{0,120}CertificationResultReporter\.Submit\(testRunNumber\)/);
assert.match(managerSource, /bool hasCompleteCertification = CertificationResultReporter\.HasCompleteReadings/);
assert.match(managerSource, /if \(!answeredAny \|\| !hasCompleteCertification\)/);
assert.match(managerSource, /ScreenshotSender\.didPass = hasCompleteCertification && !hasIndividualFail && whitePassed && blackPassed/);
assert.match(managerSource, /while \(CertificationResultReporter\.IsSubmitting\)/);
assert.match(managerSource, /private bool IsQuestionVideoCoveredByOverlay\(\)/);
assert.match(managerSource, /private void SetVideoIndicatorsVisible\(bool isVisible\)/);
assert.match(managerSource, /private bool TryUsePreparedVideo[\s\S]*SetActivePlaybackPlayer\(preparedPlayer\);[\s\S]*preparedPlayer\.Play\(\);/);
assert.match(managerSource, /private bool TryUsePreparedVideo[\s\S]*BeginVideoPlayback\(true\);[\s\S]*RequestSmokeVideoDirectDisplay\(\);/);
assert.match(managerSource, /private bool LoadQuestionVideo[\s\S]*BeginVideoPlayback\(false\);/);
assert.match(managerSource, /if \(suppressLoadingForPreparedVideo\)[\s\S]{0,120}loadingImage\.SetActive\(false\);/);
assert.doesNotMatch(managerSource, /waitingForVideoStart = true;/);
assert.doesNotMatch(managerSource, /private IEnumerator AutoAdvanceToNextQuestion\(\)[\s\S]{0,700}loadingImage\.SetActive\(true\)/);
assert.doesNotMatch(managerSource, /void OnNextButtonClicked\(\)[\s\S]{0,700}loadingImage\.SetActive\(true\)/);
assert.doesNotMatch(managerSource, /currentQuestionIndex = nextIndex;[\s\S]{0,220}loadingImage\.SetActive\(true\)/);
assert.match(managerSource, /private void SetActivePlaybackPlayer[\s\S]*smokeVideoDirectDisplay\.SetVideoPlayer\(activeVideoPlayer\);/);
assert.match(managerSource, /void StartPreloadSlot[\s\S]*slot\.player\.renderMode = VideoRenderMode\.APIOnly;[\s\S]*slot\.player\.Prepare\(\);/);
assert.match(managerSource, /private void StartCurrentPhaseAtFirstQuestion\(\)[\s\S]{0,700}SetVideoIndicatorsVisible\(true\)/);
assert.match(managerSource, /private void ShowTestCompletePanel\(\)[\s\S]{0,180}SetVideoIndicatorsVisible\(false\)/);
assert.match(managerSource, /if \(player != null\)[\s\S]{0,80}player\.Stop\(\);[\s\S]{0,80}waitingForVideoStart = false;/);
assert.match(managerSource, /private void ShowTestCompletePanel\(\)[\s\S]{0,120}StopActiveVideoPlayer\(\);[\s\S]{0,120}TestingCompletePannel\.SetActive\(true\)/);
assert.match(managerSource, /private void ShowRemarksForQuestion[\s\S]{0,800}StopActiveVideoPlayer\(\);[\s\S]{0,120}RemarksPannel\.SetActive\(true\)/);
assert.match(managerSource, /private void StartCurrentPhaseAtFirstQuestion\(\)[\s\S]{0,500}RemarksPannel\.SetActive\(false\)[\s\S]{0,200}TestingCompletePannel\.SetActive\(false\)[\s\S]{0,200}SignaturePannel\.SetActive\(false\)/);
assert.match(managerSource, /else if \(currenttype == TestType\.TestComplete\)[\s\S]{0,120}OpenSignaturePanel\(\);[\s\S]{0,40}return;/);
assert.match(managerSource, /else if \(currenttype == TestType\.blackTest\)[\s\S]{0,240}SubmissionButton\.SetActive\(true\)[\s\S]{0,180}openresultPannelButton\.gameObject\.SetActive\(false\)/);
assert.match(managerSource, /bool showSkip = isActive && \(currenttype == TestType\.whitePractice \|\| currenttype == TestType\.blackPractice\)/);
assert.match(managerSource, /if \(currenttype == TestType\.whitePractice\)[\s\S]{0,120}manageWhitePracticeTest\.GoToWhiteTutorial\(\)/);
assert.match(managerSource, /else if \(currenttype == TestType\.blackPractice\)[\s\S]{0,120}mangerBlackPractice\.GoToblackTutorial\(\)/);
assert.match(managerSource, /Skip to White Smoke Test/);
assert.match(managerSource, /Skip to Black Smoke Test/);
assert.doesNotMatch(managerSource, /Skip to Black Smoke Practice|Skip to Signature/);
assert.match(managerSource, /private void ShowTestCompletePanel\(\)[\s\S]{0,500}openresultPannelButton\.gameObject\.SetActive\(false\)/);
assert.doesNotMatch(managerSource, /else if \(currenttype == TestType\.whiteTest\)[\s\S]{0,400}openresultPannelButton\.gameObject\.SetActive\(true\)/);
assert.match(managerSource, /else if \(currenttype == TestType\.whiteTest\)[\s\S]{0,600}Feel free to review and change any answer before proceeding to Black Smoke Test\./);
assert.match(managerSource, /else if \(currenttype == TestType\.blackTest\)[\s\S]{0,500}Feel free to review and change any answer before continuing to the results page\./);
assert.match(managerSource, /private void StartWhiteTestRetake[\s\S]{0,220}testRunNumber\+\+;[\s\S]{0,180}restartAtWhiteTestIntro = true;[\s\S]{0,500}SceneManager\.LoadScene/);
assert.match(managerSource, /private IEnumerator CompleteEndTest[\s\S]{0,180}if \(!ScreenshotSender\.didPass\)[\s\S]{0,180}StartWhiteTestRetake\(completedRunNumber, true\);[\s\S]{0,80}yield break;/);
assert.ok(
  managerSource.indexOf("StartWhiteTestRetake(completedRunNumber, true);") <
    managerSource.indexOf("CertificationResultReporter.Submit(completedRunNumber)"),
  "Retake must route before result persistence can block it",
);

assert.match(authSource, /private bool ApplyPostReloadPanelRoute\(\)/);
assert.match(authSource, /ManagerTesting\.restartAtWhiteTestIntro = false;[\s\S]{0,260}whiteTestIntroPanel\.SetActive\(true\)/);

const returnHomeSource = read("Assets/Scripts/SmokeSchoolReturnHome.cs").toString("utf8");
assert.match(returnHomeSource, /SmokeSchoolAppState\.ResetCertificationState\(\)/);
assert.match(returnHomeSource, /DataInput_Fields\.checkSceneReload = 1/);
assert.match(returnHomeSource, /SceneManager\.LoadScene\(SceneManager\.GetActiveScene\(\)\.buildIndex\)/);

const directDisplaySource = read("Assets/Scripts/SmokeVideoDirectDisplay.cs").toString("utf8");
assert.match(directDisplaySource, /displayTarget\.GetWorldCorners\(corners\)/);
assert.match(directDisplaySource, /new Vector3\(availableWidth, availableHeight, 1f\)/);
assert.doesNotMatch(directDisplaySource, /GetVideoAspect\(\)/);
assert.doesNotMatch(directDisplaySource, /surfaceWidth = 4\.2f/);

const templateSource = read("Assets/WebGLTemplates/WebXR2020/index.html").toString("utf8");
assert.match(templateSource, /<button id="entervr" value="Enter VR" disabled>VR<\/button>/);
assert.match(templateSource, /ReceiveBrowserLogin/);
assert.match(templateSource, /#unity-login-overlay \{ display: none;/);

if (sourceOnly) {
  console.log(JSON.stringify({
    ok: true,
    sourceOnly: true,
    lockedSourceFiles: lockedSourceHashes.size,
    cloudinaryVideoUrls: videoUrls.length,
    whiteSmokeMappings: whiteMappings.length,
    whiteMappingDigest: sha256(Buffer.from(whiteMappings.join("\n"))),
    blackSmokeMappings: blackMappings.length,
    blackMappingDigest: sha256(Buffer.from(blackMappings.join("\n"))),
    networkChecked: checkNetwork,
  }, null, 2));
  process.exit(0);
}

const indexPath = path.join(output, "index.html");
const authConfigPath = path.join(output, "auth-config.js");
const headersPath = path.join(output, "_headers");
for (const filePath of [indexPath, authConfigPath, headersPath]) {
  assert.equal(existsSync(filePath), true, `missing deploy output: ${filePath}`);
}

const index = readFileSync(indexPath, "utf8");
assert.match(index, /id="unity-canvas"/);
assert.match(index, /id="unity-login-overlay"/);
assert.match(index, /id="unity-login-spinner"/);
assert.match(index, /createUnityInstance/);
assert.match(index, /ReceiveBrowserLogin/);
assert.match(index, /passwordInput\.value = ""/);
assert.doesNotMatch(index, /id="auth-form"/);
assert.doesNotMatch(index, />Checking access</);

const authConfig = readFileSync(authConfigPath, "utf8");
const apiMatch = authConfig.match(/apiUrl:\s*"([^"]+)"/);
assert.ok(apiMatch, "auth-config.js is missing apiUrl");
const authUrl = new URL(apiMatch[1]);
assert.equal(authUrl.protocol, "https:");
assert.equal(authUrl.pathname, "/api/vr/login");

const buildNames = {
  data: "VR Smoke School Stock WebXR.data.br",
  framework: "VR Smoke School Stock WebXR.framework.js.br",
  wasm: "VR Smoke School Stock WebXR.wasm.br",
  loader: "VR Smoke School Stock WebXR.loader.js",
};
for (const name of Object.values(buildNames)) {
  assert.equal(existsSync(path.join(output, "Build", name)), true, `missing Unity build file: ${name}`);
  assert.match(index, new RegExp(name.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")));
}

const minimumDecompressedSizes = { data: 20_000_000, framework: 100_000, wasm: 10_000_000 };
const decompressedSizes = {};
for (const key of ["data", "framework", "wasm"]) {
  const compressed = readFileSync(path.join(output, "Build", buildNames[key]));
  const decompressed = brotliDecompressSync(compressed);
  decompressedSizes[key] = decompressed.byteLength;
  assert.ok(decompressed.byteLength > minimumDecompressedSizes[key], `${key} build output is unexpectedly small`);
}

const headers = readFileSync(headersPath, "utf8");
assert.match(headers, /\.wasm\.br[\s\S]*Content-Type: application\/wasm[\s\S]*Content-Encoding: br/);
assert.match(headers, /\.framework\.js\.br[\s\S]*Content-Type: application\/javascript[\s\S]*Content-Encoding: br/);
assert.match(headers, /\.data\.br[\s\S]*Content-Type: application\/octet-stream[\s\S]*Content-Encoding: br/);

console.log(JSON.stringify({
  ok: true,
  lockedSourceFiles: lockedSourceHashes.size,
  cloudinaryVideoUrls: videoUrls.length,
  whiteSmokeMappings: whiteMappings.length,
  whiteMappingDigest: sha256(Buffer.from(whiteMappings.join("\n"))),
  blackSmokeMappings: blackMappings.length,
  blackMappingDigest: sha256(Buffer.from(blackMappings.join("\n"))),
  networkChecked: checkNetwork,
  authEndpoint: authUrl.origin + authUrl.pathname,
  decompressedSizes,
}, null, 2));
