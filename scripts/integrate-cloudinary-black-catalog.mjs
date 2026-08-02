#!/usr/bin/env node
import assert from "node:assert/strict";
import { readFileSync, writeFileSync } from "node:fs";
import path from "node:path";

const CLOUD_NAME = "dkzd0f0tu";
const TRANSFORM = "q_auto:best,f_mp4,vc_h264";
const EXPECTED_MISSING = new Set(["45|28", "45|29", "45|30"]);
const PUBLIC_ID_PATTERN = /^Black(00|05|10|15|20|25|30|35|40|45|50|55|60|65|70|75|80|85|90|95|100)_V2-(\d{4})_([A-Za-z0-9]+)$/;
const root = process.cwd();
const inputArgument = process.argv.slice(2).find((value) => !value.startsWith("--"));
const allowIncomplete = process.argv.includes("--allow-incomplete");

assert.ok(inputArgument, "usage: node scripts/integrate-cloudinary-black-catalog.mjs <cloudinary-export.json> [--allow-incomplete]");

const inputPath = path.resolve(inputArgument);
const catalogPath = path.join(root, "Chemney_VR/Assets/Scripts/SmokeVideoURLData.asset");
const scenePath = path.join(root, "Chemney_VR/Assets/Scenes/ChimneyScene.unity");
const manifestPath = path.join(root, "scripts/black-smoke-cloudinary-manifest.json");
const input = JSON.parse(readFileSync(inputPath, "utf8"));

assert.equal(input.cloud_name, CLOUD_NAME, "unexpected Cloudinary cloud");
assert.ok(Array.isArray(input.resources), "export is missing resources");

const records = input.resources.map((resource) => {
  const match = resource.public_id?.match(PUBLIC_ID_PATTERN);
  assert.ok(match, `invalid Black V2 public ID: ${resource.public_id}`);

  const opacity = Number(match[1]);
  const sequence = Number(match[2]);
  assert.ok(sequence >= 1 && sequence <= 30, `out-of-range sequence: ${resource.public_id}`);
  assert.equal(resource.display_name, resource.public_id, `display name mismatch: ${resource.public_id}`);
  assert.equal(resource.asset_folder, `Black Smoke/Black ${String(opacity).padStart(2, "0")}`, `folder mismatch: ${resource.public_id}`);
  assert.equal(resource.resource_type, "video", `resource type mismatch: ${resource.public_id}`);
  assert.equal(resource.format, "mov", `source format mismatch: ${resource.public_id}`);
  assert.equal(resource.width, 4096, `source width mismatch: ${resource.public_id}`);
  assert.equal(resource.height, 2160, `source height mismatch: ${resource.public_id}`);
  assert.ok(Number.isInteger(resource.version) && resource.version > 0, `invalid version: ${resource.public_id}`);

  return {
    assetId: resource.asset_id,
    publicId: resource.public_id,
    opacity,
    sequence,
    version: resource.version,
    width: resource.width,
    height: resource.height,
    bytes: resource.bytes,
    duration: resource.duration,
    createdAt: resource.created_at,
    sourceUrl: resource.secure_url,
    deliveryUrl: `https://res.cloudinary.com/${CLOUD_NAME}/video/upload/${TRANSFORM}/v${resource.version}/${resource.public_id}.mp4`,
  };
}).sort((left, right) => left.opacity - right.opacity || left.sequence - right.sequence);

assert.equal(new Set(records.map((record) => record.assetId)).size, records.length, "duplicate Cloudinary asset IDs");
assert.equal(new Set(records.map((record) => record.publicId)).size, records.length, "duplicate Cloudinary public IDs");
assert.equal(new Set(records.map((record) => `${record.opacity}|${record.sequence}`)).size, records.length, "duplicate opacity/sequence slots");

const missing = [];
for (let opacity = 0; opacity <= 100; opacity += 5) {
  for (let sequence = 1; sequence <= 30; sequence += 1) {
    if (!records.some((record) => record.opacity === opacity && record.sequence === sequence)) {
      missing.push(`${opacity}|${sequence}`);
    }
  }
}

if (allowIncomplete) {
  assert.deepEqual(new Set(missing), EXPECTED_MISSING, "incomplete export does not match the known Black 45 gap");
} else {
  assert.deepEqual(missing, [], `Black catalog is incomplete: ${missing.join(", ")}`);
}

const byOpacity = new Map();
for (let opacity = 0; opacity <= 100; opacity += 5) {
  byOpacity.set(opacity, records.filter((record) => record.opacity === opacity));
}

function replaceCatalogBlackGroups(source) {
  const lines = source.split("\n");
  const output = [];
  const replaced = new Set();
  let opacity = null;

  for (let index = 0; index < lines.length; index += 1) {
    const percentageMatch = lines[index].match(/^  - percentage: (\d+)$/);
    if (percentageMatch) opacity = Number(percentageMatch[1]);

    if (lines[index] !== "    - typeName: Black") {
      output.push(lines[index]);
      continue;
    }

    assert.ok(byOpacity.has(opacity), `unexpected Black group at ${opacity}%`);
    assert.equal(lines[index + 1], "      videoURLs:", `malformed Black group at ${opacity}%`);
    output.push(lines[index], lines[index + 1]);
    for (const record of byOpacity.get(opacity)) output.push(`      - ${record.deliveryUrl}`);
    replaced.add(opacity);
    index += 1;
    while (index + 1 < lines.length && lines[index + 1].startsWith("      - ")) index += 1;
  }

  assert.equal(replaced.size, 21, "expected to replace 21 Black catalog groups");
  return output.join("\n");
}

function replaceTutorialUrls(source) {
  const tutorialOpacities = new Set([25, 50, 75, 100]);
  const replacementCounts = new Map([...tutorialOpacities].map((opacity) => [opacity, 0]));
  const firstByOpacity = new Map(
    [...tutorialOpacities].map((opacity) => [opacity, byOpacity.get(opacity).find((record) => record.sequence === 1).deliveryUrl]),
  );

  const result = source.replace(/^  - https:\/\/res\.cloudinary\.com\/dkzd0f0tu\/video\/upload\/(?:[^/\s]+\/)+Black(25|50|75|100)_V2-\d{4}_[A-Za-z0-9]+\.mp4$/gm, (line, opacityValue) => {
    const opacity = Number(opacityValue);
    replacementCounts.set(opacity, replacementCounts.get(opacity) + 1);
    return `  - ${firstByOpacity.get(opacity)}`;
  });

  for (const [opacity, count] of replacementCounts) {
    assert.equal(count, 2, `expected two Black ${opacity}% tutorial mappings, found ${count}`);
  }
  return result;
}

function readTypeUrls(source, expectedType) {
  const urls = [];
  let smokeType = null;
  for (const line of source.split("\n")) {
    const typeMatch = line.match(/^    - typeName: (White|Black)$/);
    if (typeMatch) {
      smokeType = typeMatch[1];
    } else if (smokeType === expectedType && line.startsWith("      - https://")) {
      urls.push(line);
    }
  }
  return urls;
}

const originalCatalog = readFileSync(catalogPath, "utf8");
const originalWhiteLines = readTypeUrls(originalCatalog, "White");
const updatedCatalog = replaceCatalogBlackGroups(originalCatalog);
assert.deepEqual(readTypeUrls(updatedCatalog, "White"), originalWhiteLines, "White mappings changed");

const originalScene = readFileSync(scenePath, "utf8");
const updatedScene = replaceTutorialUrls(originalScene);

const manifest = {
  generatedAt: input.generated_at,
  cloudName: CLOUD_NAME,
  transform: TRANSFORM,
  sourceExpression: input.expression,
  resourceCount: records.length,
  missing: missing.map((slot) => {
    const [opacity, sequence] = slot.split("|").map(Number);
    return { opacity, sequence };
  }),
  resources: records,
};

writeFileSync(catalogPath, updatedCatalog);
writeFileSync(scenePath, updatedScene);
writeFileSync(manifestPath, JSON.stringify(manifest, null, 2) + "\n");

console.log(JSON.stringify({
  ok: true,
  resourceCount: records.length,
  missing: manifest.missing,
  catalogPath: path.relative(root, catalogPath),
  scenePath: path.relative(root, scenePath),
  manifestPath: path.relative(root, manifestPath),
}, null, 2));
