#!/usr/bin/env node
import assert from "node:assert/strict";
import { createRequire } from "node:module";
import { createServer } from "node:http";
import { existsSync, readFileSync } from "node:fs";
import path from "node:path";

const require = createRequire(import.meta.url);
const { chromium } = require("playwright");
const root = process.cwd();
const buildPath = path.resolve(process.argv[2] || "Chemney_VR/VR Smoke School Stock WebXR");
const screenshotPath = path.resolve(process.argv[3] || "webxr-authenticated-playwright.png");
const contentTypes = new Map([
  [".br", "application/octet-stream"],
  [".css", "text/css"],
  [".html", "text/html"],
  [".js", "application/javascript"],
  [".json", "application/json"],
  [".wasm", "application/wasm"],
]);

assert.equal(existsSync(path.join(buildPath, "index.html")), true, `missing WebXR build at ${buildPath}`);

const server = createServer((request, response) => {
  const requestPath = decodeURIComponent(new URL(request.url, "http://127.0.0.1").pathname);
  const relativePath = requestPath === "/" ? "index.html" : requestPath.slice(1);
  const filePath = path.resolve(buildPath, relativePath);
  if (!filePath.startsWith(buildPath + path.sep) || !existsSync(filePath)) {
    response.writeHead(404).end();
    return;
  }

  const headers = { "Content-Type": contentTypes.get(path.extname(filePath)) || "application/octet-stream" };
  if (filePath.endsWith(".wasm.br")) headers["Content-Type"] = "application/wasm";
  if (filePath.endsWith(".framework.js.br")) headers["Content-Type"] = "application/javascript";
  if (filePath.endsWith(".data.br")) headers["Content-Type"] = "application/octet-stream";
  if (filePath.endsWith(".br")) headers["Content-Encoding"] = "br";
  response.writeHead(200, headers).end(readFileSync(filePath));
});

await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
const address = server.address();
const origin = `http://127.0.0.1:${address.port}`;
const browser = await chromium.launch({ headless: true });
const page = await browser.newPage({ viewport: { width: 1440, height: 1000 }, deviceScaleFactor: 1 });
const checkTutorial = process.argv.includes("--tutorial");
let tutorialPlayback = null;
if (checkTutorial) {
  await page.addInitScript(() => {
    window.__tutorialVideos = [];
    const createElement = document.createElement;
    document.createElement = function (...args) {
      const element = createElement.apply(this, args);
      if (String(args[0]).toLowerCase() === "video") window.__tutorialVideos.push(element);
      return element;
    };
  });
}
const pageErrors = [];
const consoleErrors = [];
page.on("pageerror", (error) => pageErrors.push(error.message));
page.on("console", (message) => {
  if (message.type() === "error") consoleErrors.push(message.text());
});

try {
  await page.route("https://playwright.smokeschool.test/api/vr/login", async (route) => {
    if (route.request().method() === "OPTIONS") {
      await route.fulfill({
        status: 204,
        headers: {
          "access-control-allow-origin": origin,
          "access-control-allow-methods": "POST, OPTIONS",
          "access-control-allow-headers": "content-type",
        },
      });
      return;
    }
    assert.equal(route.request().method(), "POST");
    const credentials = route.request().postDataJSON();
    assert.deepEqual(credentials, { email: "dev@testing.com", password: "testing123" });
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      headers: { "access-control-allow-origin": origin },
      body: JSON.stringify({
        approved: true,
        sessionReference: "playwright-session",
        resultToken: "playwright-result-token",
        student: {
          certificationNumber: "SS-DEV-TEST-001",
          userId: "playwright-user",
          email: "dev@testing.com",
          displayName: "Playwright Student",
          company: "Smoke School",
          expiresAt: "2099-12-31T23:59:59Z",
        },
      }),
    });
  });

  const authApi = encodeURIComponent("https://playwright.smokeschool.test/api/vr/login");
  await page.goto(`${origin}/?authApi=${authApi}`, { waitUntil: "domcontentloaded", timeout: 120_000 });
  await page.locator("#unity-loading-bar").waitFor({ state: "hidden", timeout: 180_000 });
  await page.locator("#unity-login-overlay").waitFor({ state: "visible", timeout: 180_000 });
  assert.equal(await page.locator("#unity-coming-soon-banner").count(), 0);
  if (process.argv[4]) await page.screenshot({ path: path.resolve(process.argv[4]), fullPage: true });
  await page.getByLabel("User Email").fill("dev@testing.com");
  await page.getByLabel("Password").fill("testing123");
  await page.getByRole("button", { name: "Submit login" }).click();
  assert.equal(await page.getByLabel("Password").inputValue(), "");
  await page.locator("#unity-login-overlay").waitFor({ state: "hidden", timeout: 60_000 });
  await page.waitForTimeout(2_000);

  const canvas = page.locator("#unity-canvas");
  const bounds = await canvas.boundingBox();
  assert.ok(bounds && bounds.width >= 900 && bounds.height >= 550, `Unity canvas has unexpected dimensions: ${JSON.stringify(bounds)}`);
  await page.screenshot({ path: screenshotPath, fullPage: true });

  if (checkTutorial) {
    const expectedVideo = "https://res.cloudinary.com/dkzd0f0tu/video/upload/v1789398963/Smoke_School_Intro_copy_ndfg6o.mov";
    const tutorialScreenshot = screenshotPath.replace(/\.png$/, "-tutorial.png");
    // The tutorial button is rendered inside the Unity canvas.
    await page.mouse.click(bounds.x + bounds.width * (368 / 960), bounds.y + bounds.height * (181.5 / 600), { delay: 150 });
    try {
      await page.waitForFunction((expected) => window.__tutorialVideos.some((v) =>
        (v.currentSrc || v.src) === expected && v.readyState >= 2 && !v.paused && v.currentTime >= 1 && v.videoWidth > 0 && !v.error
      ), expectedVideo, { timeout: 45_000 });
      const firstTime = await page.evaluate((expected) => window.__tutorialVideos.find((v) => (v.currentSrc || v.src) === expected).currentTime, expectedVideo);
      await page.waitForFunction(({ expected, firstTime }) => window.__tutorialVideos.find((v) => (v.currentSrc || v.src) === expected).currentTime > firstTime + 1,
        { expected: expectedVideo, firstTime }, { timeout: 15_000 });
      tutorialPlayback = await page.evaluate((expected) => {
        const video = window.__tutorialVideos.find((v) => (v.currentSrc || v.src) === expected);
        return { url: video.currentSrc, currentTime: video.currentTime, width: video.videoWidth, height: video.videoHeight,
          duration: video.duration, paused: video.paused, muted: video.muted, volume: video.volume, error: video.error?.code || null };
      }, expectedVideo);
      assert.equal(tutorialPlayback.width, 1920);
      assert.equal(tutorialPlayback.height, 1080);
      assert.ok(tutorialPlayback.duration > 680 && tutorialPlayback.duration < 684);
    } finally {
      await page.screenshot({ path: tutorialScreenshot, fullPage: true });
      console.log(JSON.stringify({ tutorialPlayback, tutorialScreenshot, pageErrors, consoleErrors }, null, 2));
    }
  }

  assert.deepEqual(pageErrors, [], `browser page errors:\n${pageErrors.join("\n")}`);
  const unexpectedConsoleErrors = consoleErrors.filter((message) => !message.startsWith("DllNotFoundException: Unable to load DLL 'UnityOpenXR'"));
  assert.deepEqual(unexpectedConsoleErrors, [], `browser console errors:\n${unexpectedConsoleErrors.join("\n")}`);
  console.log(JSON.stringify({
    ok: true,
    buildPath: path.relative(root, buildPath),
    authenticated: true,
    tutorialPlayback,
    canvas: bounds,
    screenshotPath,
    expectedUnityOpenXrWarning: consoleErrors.length - unexpectedConsoleErrors.length,
  }, null, 2));
} finally {
  await browser.close();
  await new Promise((resolve) => server.close(resolve));
}
