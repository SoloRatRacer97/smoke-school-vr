#!/usr/bin/env node
import { existsSync, readFileSync } from "node:fs";
import path from "node:path";

const root = process.cwd();
const project = path.join(root, "Chemney_VR");
const templatePath = path.join(project, "Assets/WebGLTemplates/WebXR2020/index.html");
const configPath = path.join(project, "Assets/WebGLTemplates/WebXR2020/auth-config.js");
const dataInputPath = path.join(project, "Assets/Scripts/DataInput_Fields.cs");
const scenePath = path.join(project, "Assets/Scenes/ChimneyScene.unity");
const builderPath = path.join(project, "Assets/Editor/CommandLineMainStockWebXRBuild.cs");
const netlifyPath = path.join(root, "netlify.toml");

function requireText(filePath, patterns) {
  if (!existsSync(filePath)) throw new Error(`Missing ${filePath}`);
  const source = readFileSync(filePath, "utf8");
  for (const [pattern, description] of patterns) {
    if (!pattern.test(source)) throw new Error(`${filePath} is missing ${description}`);
  }
  return source;
}

function rejectText(filePath, source, patterns) {
  for (const [pattern, description] of patterns) {
    if (pattern.test(source)) throw new Error(`${filePath} contains ${description}`);
  }
}

function verifyBrowserGate(filePath) {
  const template = requireText(filePath, [
    [/auth-config\.js/, "the auth configuration"],
    [/id="unity-login-overlay"/, "the browser login form"],
    [/id="unity-login-spinner"/, "the authentication spinner"],
    [/fetch\(apiUrl,\s*\{[\s\S]*?method: "POST"/, "the browser POST request"],
    [/cache: "no-store"/, "request cache prevention"],
    [/passwordInput\.value = ""/, "password clearing"],
    [/invalid_credentials/, "invalid-credentials denial mapping"],
    [/access_revoked/, "revoked-access denial mapping"],
    [/access_inactive/, "inactive-access denial mapping"],
    [/access_expired/, "expired-access denial mapping"],
    [/rate_limited/, "rate-limit denial mapping"],
    [/Authentication is temporarily unavailable/, "service-response failure message"],
    [/Could not reach the authentication service/, "service-network failure message"],
    [/sessionReference: result\.sessionReference/, "the approved session reference"],
    [/userId: result\.student\.userId/, "the approved user ID"],
    [/if \(approvedPayload\) \{[\s\S]{0,220}startUnity\(approvedPayload\)/, "approval-gated Unity startup"],
    [/SendMessage\("LoginPanel", "CompleteApprovedLogin", JSON\.stringify\(approvedPayload\)\)/, "the sanitized Unity approval bridge"],
  ]);
  rejectText(filePath, template, [
    [/localStorage|sessionStorage/, "persisted browser authorization"],
    [/ReceiveBrowserLogin/, "a raw credential Unity bridge"],
  ]);

  const startUnityIndex = template.indexOf("function startUnity(approvedPayload)");
  const startUnityEnd = template.indexOf('document.getElementById("unity-login-overlay").addEventListener', startUnityIndex);
  const createIndex = template.indexOf("createUnityInstance(");
  const appendIndexes = [...template.matchAll(/document\.body\.appendChild\(script\)/g)].map((match) => match.index);
  const sendCalls = template.match(/SendMessage\([^;\n]+/g) || [];
  if (startUnityIndex < 0 || startUnityEnd < 0 || createIndex < startUnityIndex || createIndex > startUnityEnd) {
    throw new Error(`${filePath} starts Unity outside the approved startup function`);
  }
  if (appendIndexes.length !== 1 || appendIndexes[0] < startUnityIndex || appendIndexes[0] > startUnityEnd) {
    throw new Error(`${filePath} appends the Unity loader outside the approved startup function`);
  }
  if (sendCalls.length !== 1 || /password/i.test(sendCalls[0])) {
    throw new Error(`${filePath} sends raw credentials across the Unity bridge`);
  }
  return template;
}

verifyBrowserGate(templatePath);
requireText(configPath, [[/api\/vr\/login/, "the VR login endpoint"]]);
const dataInput = requireText(dataInputPath, [
  [/class ApprovedPayload/, "the approved response model"],
  [/public bool approved/, "the approved flag"],
  [/public string sessionReference/, "the session reference schema"],
  [/public string userId/, "the user ID schema"],
  [/public static string approvedUserId/, "in-memory user ID correlation"],
  [/public static string approvedSessionReference/, "in-memory session correlation"],
  [/public void CompleteApprovedLogin/, "the approved login entry point"],
]);
rejectText(dataInputPath, dataInput, [
  [/UnityWebRequest|UnityEngine\.Networking/, "Unity network authentication"],
  [/PlayerPrefs/, "persisted Unity authorization"],
  [/password/i, "password handling"],
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
  [/ValidateBrowserFirstAuthentication/, "source auth validation"],
  [/ValidateBuiltBrowserFirstAuthentication/, "built auth validation"],
]);
requireText(netlifyPath, [[/publish = "Chemney_VR\/VR Smoke School Stock WebXR"/, "the stock WebXR publish directory"]]);

const outputPath = process.argv[2] ? path.resolve(process.argv[2]) : null;
if (outputPath) {
  verifyBrowserGate(path.join(outputPath, "index.html"));
  requireText(path.join(outputPath, "auth-config.js"), [[/api\/vr\/login/, "the built login endpoint"]]);
  requireText(path.join(outputPath, "_headers"), [[/Content-Encoding: br/, "Netlify Brotli headers"]]);
}

console.log(JSON.stringify({ ok: true, sourceVerified: true, outputVerified: Boolean(outputPath) }, null, 2));
