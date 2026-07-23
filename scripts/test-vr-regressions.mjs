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
const outputArgument = argumentsList.find((value) => value !== "--network");
const output = outputArgument
  ? path.resolve(outputArgument)
  : path.join(project, "VR Smoke School Stock WebXR");

const lockedSourceHashes = new Map([
  ["Assets/ManagerTesting.cs", "83d4b582263aa8a3bb1db2a30df4950a586742b659c87b2632e6ea50cfaa934d"],
  ["Assets/Scripts/SmokeVideoDirectDisplay.cs", "543c44b967a491de1bc15620d145641fa88e07c0e7fdba88b14abd7066378039"],
  ["Assets/Scripts/SmokeVideoURLData.asset", "487c110665ef4e4e3c7a840df5c0d7e06b6f5c829d87f47976265a1f875d6161"],
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
assert.ok(videoUrls.length >= 100, "the Cloudinary smoke-video catalog is unexpectedly small");
for (const value of videoUrls) {
  const url = new URL(value);
  assert.equal(url.hostname, "res.cloudinary.com", `unexpected video host: ${url.hostname}`);
  assert.equal(url.pathname.startsWith("/dkzd0f0tu/video/upload/"), true, `unexpected Cloudinary account or path: ${url.pathname}`);
}

const expectedWhiteCounts = {
  0: 8, 5: 13, 10: 2, 15: 3, 20: 11, 25: 7, 30: 13,
  35: 12, 40: 2, 45: 17, 50: 12, 55: 10, 60: 10, 65: 11,
  70: 14, 75: 15, 80: 16, 85: 14, 90: 12, 95: 15, 100: 17,
};
let percentage = null;
let smokeType = null;
const whiteMappings = [];
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
  }
}
assert.deepEqual(whiteCounts, expectedWhiteCounts, "white-smoke URL counts drifted by opacity level");
assert.equal(whiteMappings.length, 234, "white-smoke mapping count drifted");
assert.equal(
  sha256(Buffer.from(whiteMappings.join("\n"))),
  "80c8ac96e99ae771cfb1aa6e1bb5d95e932315c85d146c18ec7061adc88923d1",
  "white-smoke URL order or opacity mapping drifted",
);

if (checkNetwork) {
  const uniqueUrls = [...new Set(videoUrls)];
  const failures = [];
  let nextIndex = 0;
  async function worker() {
    while (nextIndex < uniqueUrls.length) {
      const url = uniqueUrls[nextIndex++];
      try {
        const response = await fetch(url, { method: "HEAD", signal: AbortSignal.timeout(15_000) });
        if (!response.ok) failures.push(`${response.status} ${url}`);
      } catch (error) {
        failures.push(`${error.name} ${url}`);
      }
    }
  }
  await Promise.all(Array.from({ length: 8 }, worker));
  assert.deepEqual(failures, [], `Cloudinary video availability failures:\n${failures.join("\n")}`);
}

const scene = read("Assets/Scenes/ChimneyScene.unity").toString("utf8");
assert.match(scene, /m_Name: Emission Testing Text[\s\S]{0,1800}m_text: Emission Testing/);
assert.match(scene, /m_Name: Videos Tutorials Text[\s\S]{0,1800}m_text: Video Tutorials/);
assert.match(scene, /https:\/\/res\.cloudinary\.com\/dkzd0f0tu\/video\/upload\/v1774123829\/Smoke_School_Intro2_lzx9e4\.mov/);

const authSource = read("Assets/Scripts/DataInput_Fields.cs").toString("utf8");
assert.match(authSource, /UnityWebRequest/);
assert.match(authSource, /SetAuthenticationLoading\(true\)/);
assert.match(authSource, /inputStudentID\.text = string\.Empty/);
assert.doesNotMatch(authSource, /warningText\.text = "Checking access/);

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
  networkChecked: checkNetwork,
  authEndpoint: authUrl.origin + authUrl.pathname,
  decompressedSizes,
}, null, 2));
