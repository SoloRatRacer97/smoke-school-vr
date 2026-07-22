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

function requireText(filePath, patterns) {
  if (!existsSync(filePath)) throw new Error(`Missing ${filePath}`);
  const source = readFileSync(filePath, "utf8");
  for (const [pattern, description] of patterns) {
    if (!pattern.test(source)) throw new Error(`${filePath} is missing ${description}`);
  }
  return source;
}

requireText(templatePath, [
  [/id="auth-form"/, "the auth form"],
  [/fetch\(apiUrl/, "the dashboard API request"],
  [/CompleteApprovedLogin/, "the approved-profile Unity bridge"],
  [/passwordInput\.value = ""/, "password clearing"],
  [/response\.status === 429/, "rate-limit handling"],
]);
requireText(configPath, [[/api\/vr\/login/, "the VR login endpoint"]]);
requireText(dataInputPath, [
  [/public void CompleteApprovedLogin/, "the approved login entry point"],
  [/#if !UNITY_WEBGL \|\| UNITY_EDITOR/, "WebGL local-login suppression"],
]);
const scene = requireText(scenePath, [[/m_Name: LoginPanel/, "the LoginPanel GameObject"]]);
if (/m_TargetAssemblyTypeName: DataInput_Fields,[\s\S]{0,120}m_MethodName: OnGoButtonClicked/.test(scene)) {
  throw new Error("ChimneyScene still contains a WebGL-bypass login callback.");
}
requireText(builderPath, [
  [/ValidateAuthenticationTemplate/, "source auth validation"],
  [/ValidateBuiltAuthenticationGate/, "built auth validation"],
]);

const outputPath = process.argv[2] ? path.resolve(process.argv[2]) : null;
if (outputPath) {
  requireText(path.join(outputPath, "index.html"), [
    [/id="auth-form"/, "the built auth form"],
    [/CompleteApprovedLogin/, "the built Unity auth bridge"],
  ]);
  requireText(path.join(outputPath, "auth-config.js"), [[/api\/vr\/login/, "the built login endpoint"]]);
  requireText(path.join(outputPath, "_headers"), [[/Content-Encoding: br/, "Netlify Brotli headers"]]);
}

console.log(JSON.stringify({ ok: true, sourceVerified: true, outputVerified: Boolean(outputPath) }, null, 2));
