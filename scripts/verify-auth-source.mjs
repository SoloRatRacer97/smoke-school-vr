#!/usr/bin/env node
import { existsSync, readFileSync } from "node:fs";
import path from "node:path";

const root = process.cwd();
const project = path.join(root, "Chemney_VR");
const templatePath = path.join(project, "Assets/WebGLTemplates/WebXR2020/index.html");
const configPath = path.join(project, "Assets/WebGLTemplates/WebXR2020/auth-config.js");
const dataInputPath = path.join(project, "Assets/Scripts/DataInput_Fields.cs");
const resultReporterPath = path.join(project, "Assets/Scripts/CertificationResultReporter.cs");
const testManagerPath = path.join(project, "Assets/ManagerTesting.cs");
const authPluginPath = path.join(project, "Assets/Plugins/WebGL/SmokeSchoolAuth.jslib");
const scenePath = path.join(project, "Assets/Scenes/ChimneyScene.unity");
const builderPath = path.join(project, "Assets/Editor/CommandLineMainStockWebXRBuild.cs");

function requireText(filePath, patterns) {
  if (!existsSync(filePath)) throw new Error(`Missing ${filePath}`);
  const source = readFileSync(filePath, "utf8");
  for (const [pattern, description] of patterns) {
    if (!pattern.test(source)) throw new Error(`${filePath} is missing ${description}`);
  }
  return source;
}

requireText(templatePath, [
  [/auth-config\.js/, "the auth configuration"],
  [/createUnityInstance/, "immediate Unity startup"],
  [/id="unity-login-overlay"/, "the Unity-aligned WebGL input bridge"],
  [/id="unity-login-spinner"/, "the password authentication spinner"],
  [/ReceiveBrowserLogin/, "the Unity browser-input callback"],
]);
const template = readFileSync(templatePath, "utf8");
if (/id="auth-form"/.test(template)) throw new Error("WebXR template still contains the HTML login gate.");
requireText(configPath, [[/api\/vr\/login/, "the VR login endpoint"]]);
requireText(dataInputPath, [
  [/UnityWebRequest/, "the Unity-native dashboard request"],
  [/SmokeSchoolGetAuthApi/, "the WebGL auth configuration bridge"],
  [/InputField\.ContentType\.Password/, "the password input mode"],
  [/inputStudentID\.text = string\.Empty/, "password clearing"],
  [/SetAuthenticationLoading\(true\)/, "authentication spinner activation"],
  [/public void CompleteApprovedLogin/, "the approved login entry point"],
  [/public void ReceiveBrowserLogin/, "the Unity browser-input entry point"],
  [/goButton\.onClick\.AddListener\(OnGoButtonClicked\)/, "the Unity login button callback"],
  [/public static string approvedCertificationNumber/, "approved certification retention"],
  [/public static string approvedSessionReference/, "approved session retention"],
  [/public static string approvedResultToken/, "approved result-token retention"],
  [/student\.certificationNumber[\s\S]{0,120}student\.userId/, "certification-number compatibility fallback"],
  [/Uri\.TryCreate\(approvedAuthenticationUrl, UriKind\.Absolute/, "absolute authentication URL parsing"],
  [/Uri\.UriSchemeHttps[\s\S]{0,120}Uri\.UriSchemeHttp/, "HTTP(S) endpoint restriction"],
  [/AbsolutePath != "\/api\/vr\/login"[\s\S]{0,120}AbsolutePath != "\/api\/vr\/login\/"/, "the exact login endpoint path"],
  [/new UriBuilder\(authenticationUri\)[\s\S]{0,180}Path = "\/api\/vr\/certification-attempts"[\s\S]{0,120}Query = string\.Empty[\s\S]{0,120}Fragment = string\.Empty/, "path-only certification endpoint derivation"],
]);
requireText(resultReporterPath, [
  [/ExpectedReadingCount = 50/, "the exact reading count"],
  [/if \(hasSucceeded \|\| isSubmitting\)/, "the exactly-once submission guard"],
  [/Guid\.NewGuid\(\)/, "the stable per-run attempt ID"],
  [/resultToken[\s\S]*attemptId[\s\S]*runNumber[\s\S]*startedAt[\s\S]*completedAt[\s\S]*rulesVersion[\s\S]*clientVersion[\s\S]*readings/, "all certification request fields"],
  [/section[\s\S]*questionNumber[\s\S]*videoId[\s\S]*actualOpacity[\s\S]*studentOpacity/, "all reading fields"],
  [/epa-method-9-v1/, "the EPA Method 9 rules version"],
  [/white\.questionNumber != i \+ 1[\s\S]*black\.questionNumber != i \+ 1/, "25 ordered readings per section"],
  [/authoritative\.whiteReadingCount != ExpectedSectionReadingCount[\s\S]{0,160}authoritative\.blackReadingCount != ExpectedSectionReadingCount/, "authoritative 25-reading section counts"],
  [/authoritative\.whiteScore != localWhiteScore[\s\S]{0,120}authoritative\.blackScore != localBlackScore/, "authoritative score validation"],
  [/result\.deviation > IndividualFailureThreshold/, "authoritative individual-failure calculation"],
  [/authoritative\.individualFailureCount != localIndividualFailureCount/, "authoritative individual-failure validation"],
  [/authoritative\.passed != localPassed/, "authoritative pass validation"],
  [/requiredFields[\s\S]{0,500}required result field/, "missing authoritative result-field rejection"],
]);
requireText(testManagerPath, [
  [/CertificationResultReporter\.BeginNewRun\(\)/, "new-run attempt reset"],
  [/ScreenshotSender\.didPass = didPass;[\s\S]{0,120}CertificationResultReporter\.Submit\(testRunNumber\)/, "result submission after final pass/fail calculation"],
  [/ScreenshotSender\.didPass = hasCompleteCertification && !hasIndividualFail && whitePassed && blackPassed/, "complete-reading end-test pass guard"],
  [/while \(CertificationResultReporter\.IsSubmitting\)/, "reload persistence wait"],
]);
requireText(authPluginPath, [
  [/SMOKE_SCHOOL_AUTH/, "the browser auth configuration bridge"],
  [/SmokeSchoolSetLoginOverlayVisible/, "the browser login visibility bridge"],
  [/SmokeSchoolSetAuthenticationLoading/, "the browser authentication spinner bridge"],
]);
const scene = requireText(scenePath, [[/m_Name: LoginPanel/, "the LoginPanel GameObject"]]);
if (!/value: Password/.test(scene)) throw new Error("ChimneyScene login field is not labeled Password.");
if (!/m_Name: Emission Testing Text[\s\S]{0,1800}m_text: Video Tutorials/.test(scene)) {
  throw new Error("Left card title is not Video Tutorials.");
}
if (!/m_Name: Videos Tutorials Text[\s\S]{0,1800}m_text: Emission Testing/.test(scene)) {
  throw new Error("Right card title is not Emission Testing.");
}
if (!/m_text: Start Tutorial/.test(scene) || !/m_text: Begin Test/.test(scene)) {
  throw new Error("Final card button mappings changed.");
}
requireText(builderPath, [
  [/ValidateUnityAuthentication/, "source auth validation"],
  [/ValidateBuiltUnityAuthentication/, "built auth validation"],
]);

const outputPath = process.argv[2] ? path.resolve(process.argv[2]) : null;
if (outputPath) {
  requireText(path.join(outputPath, "index.html"), [
    [/createUnityInstance/, "the built Unity application startup"],
    [/auth-config\.js/, "the built auth configuration"],
    [/id="unity-login-overlay"/, "the built Unity-aligned input bridge"],
    [/id="unity-login-spinner"/, "the built password spinner"],
  ]);
  const builtIndex = readFileSync(path.join(outputPath, "index.html"), "utf8");
  if (/id="auth-form"/.test(builtIndex)) throw new Error("Built output still contains the HTML login gate.");
  requireText(path.join(outputPath, "auth-config.js"), [[/api\/vr\/login/, "the built login endpoint"]]);
  requireText(path.join(outputPath, "_headers"), [[/Content-Encoding: br/, "Netlify Brotli headers"]]);
}

console.log(JSON.stringify({ ok: true, sourceVerified: true, outputVerified: Boolean(outputPath) }, null, 2));
