#!/usr/bin/env node
import { existsSync, readFileSync } from "node:fs";
import path from "node:path";

const root = process.cwd();
const project = path.join(root, "Chemney_VR");
const templatePath = path.join(project, "Assets/WebGLTemplates/WebXR2020/index.html");
const configPath = path.join(project, "Assets/WebGLTemplates/WebXR2020/auth-config.js");
const dataInputPath = path.join(project, "Assets/Scripts/DataInput_Fields.cs");
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
]);
requireText(authPluginPath, [
  [/SMOKE_SCHOOL_AUTH/, "the browser auth configuration bridge"],
  [/SmokeSchoolSetLoginOverlayVisible/, "the browser login visibility bridge"],
  [/SmokeSchoolSetAuthenticationLoading/, "the browser authentication spinner bridge"],
]);
const scene = requireText(scenePath, [[/m_Name: LoginPanel/, "the LoginPanel GameObject"]]);
if (!/value: Password/.test(scene)) throw new Error("ChimneyScene login field is not labeled Password.");
if (!/m_Name: Emission Testing Text[\s\S]{0,1800}m_text: Emission Testing/.test(scene)) {
  throw new Error("Emission Testing card title is incorrect.");
}
if (!/m_Name: Videos Tutorials Text[\s\S]{0,1800}m_text: Video Tutorials/.test(scene)) {
  throw new Error("Video Tutorials card title is incorrect.");
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
