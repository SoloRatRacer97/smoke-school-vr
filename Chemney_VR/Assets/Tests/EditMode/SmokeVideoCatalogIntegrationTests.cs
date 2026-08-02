using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SmokeSchool.Tests
{
    public class SmokeVideoCatalogIntegrationTests
    {
        private const string CatalogPath = "Assets/Scripts/SmokeVideoURLData.asset";
        private const string ProductionTransform = "q_auto:best,f_mp4,vc_h264";
        private static readonly Regex FilenamePattern = new Regex(
            @"^(White|Black)(\d{2}|100)_V(\d+)-(\d{4})_[A-Za-z0-9]+$",
            RegexOptions.Compiled);

        private sealed class CatalogGroup
        {
            public int opacity;
            public string smokeType;
            public List<string> urls;
        }

        private List<CatalogGroup> groups;

        [SetUp]
        public void LoadCatalog()
        {
            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(CatalogPath);
            Assert.That(asset, Is.Not.Null, $"Production catalog is missing at {CatalogPath}");
            groups = ReadGroups(asset);
        }

        [Test]
        public void Catalog_HasAllOpacityAndSmokeTypeGroups()
        {
            int[] expectedOpacities = Enumerable.Range(0, 21).Select(index => index * 5).ToArray();
            Assert.That(groups.Select(group => group.opacity).Distinct().OrderBy(value => value), Is.EqualTo(expectedOpacities));

            foreach (int opacity in expectedOpacities)
            {
                string[] types = groups.Where(group => group.opacity == opacity)
                    .Select(group => group.smokeType)
                    .OrderBy(value => value)
                    .ToArray();
                Assert.That(types, Is.EqualTo(new[] { "Black", "White" }), $"Opacity {opacity}% must contain exactly White and Black groups.");
            }
        }

        [Test]
        public void WhiteCatalog_HasThirtyCorrectUniqueUrlsPerOpacity()
        {
            AssertCompleteCatalog("White");
        }

        [Test]
        public void BlackCatalog_HasThirtyUrlsPerOpacity()
        {
            List<string> failures = groups.Where(group => group.smokeType == "Black" && group.urls.Count != 30)
                .OrderBy(group => group.opacity)
                .Select(group => $"{group.opacity}%: {group.urls.Count}/30")
                .ToList();

            Assert.That(failures, Is.Empty,
                "Black catalog incomplete:\n" + string.Join("\n", failures) +
                $"\nTotal Black entries: {groups.Where(group => group.smokeType == "Black").Sum(group => group.urls.Count)}/630");
        }

        [Test]
        public void BlackCatalog_HasNoDuplicateUrls()
        {
            string[] duplicates = groups.Where(group => group.smokeType == "Black")
                .SelectMany(group => group.urls)
                .GroupBy(url => url, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => $"{group.Count()}x {group.Key}")
                .ToArray();

            Assert.That(duplicates, Is.Empty, "Duplicate Black URLs:\n" + string.Join("\n", duplicates));
        }

        [Test]
        public void BlackCatalog_UrlsMatchTheirAssignedColorOpacityAndSequence()
        {
            List<string> failures = ValidateIdentity(groups.Where(group => group.smokeType == "Black"), "Black", true);
            Assert.That(failures, Is.Empty, "Invalid Black mappings:\n" + string.Join("\n", failures));
        }

        [Test]
        public void Catalog_UrlsAreValidCloudinaryMp4Deliveries()
        {
            foreach (CatalogGroup group in groups)
            {
                foreach (string value in group.urls)
                {
                    Assert.That(Uri.TryCreate(value, UriKind.Absolute, out Uri uri), Is.True, $"Malformed URL in {group.smokeType} {group.opacity}%: {value}");
                    Assert.That(uri.Scheme, Is.EqualTo(Uri.UriSchemeHttps));
                    Assert.That(uri.Host, Is.EqualTo("res.cloudinary.com"));
                    Assert.That(uri.AbsolutePath, Does.StartWith("/dkzd0f0tu/video/upload/"));
                    Assert.That(uri.AbsolutePath, Does.EndWith(".mp4"));
                }
            }
        }

        [Test]
        public void CorrectedCatalog_DeliveriesUseProductionTransform()
        {
            string[] missingTransform = groups.SelectMany(group => group.urls.Select(url => new { group, url }))
                .Where(entry => !entry.url.Contains("/" + ProductionTransform + "/"))
                .Select(entry => $"{entry.group.smokeType} {entry.group.opacity}%: {entry.url}")
                .ToArray();

            Assert.That(missingTransform, Is.Empty,
                "Catalog URLs missing the production delivery transform:\n" + string.Join("\n", missingTransform.Take(20)) +
                (missingTransform.Length > 20 ? $"\n...and {missingTransform.Length - 20} more" : string.Empty));
        }

        private void AssertCompleteCatalog(string smokeType)
        {
            CatalogGroup[] typeGroups = groups.Where(group => group.smokeType == smokeType).OrderBy(group => group.opacity).ToArray();
            Assert.That(typeGroups, Has.Length.EqualTo(21));
            Assert.That(typeGroups.All(group => group.urls.Count == 30), Is.True, $"{smokeType} must have 30 URLs at every opacity.");
            Assert.That(typeGroups.SelectMany(group => group.urls).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(630));

            List<string> identityFailures = ValidateIdentity(typeGroups, smokeType, true);
            Assert.That(identityFailures, Is.Empty, $"Invalid {smokeType} mappings:\n" + string.Join("\n", identityFailures));
        }

        private static List<string> ValidateIdentity(IEnumerable<CatalogGroup> selectedGroups, string expectedType, bool requireCompleteSequence)
        {
            List<string> failures = new List<string>();
            foreach (CatalogGroup group in selectedGroups)
            {
                HashSet<int> sequences = new HashSet<int>();
                foreach (string url in group.urls)
                {
                    string filename = Path.GetFileNameWithoutExtension(new Uri(url).AbsolutePath);
                    Match match = FilenamePattern.Match(filename);
                    if (!match.Success)
                    {
                        failures.Add($"{expectedType} {group.opacity}% has malformed filename {filename}");
                        continue;
                    }

                    int filenameOpacity = int.Parse(match.Groups[2].Value);
                    int variant = int.Parse(match.Groups[3].Value);
                    int sequence = int.Parse(match.Groups[4].Value);
                    int expectedVariant = expectedType == "White" ? 1 : 2;
                    if (match.Groups[1].Value != expectedType || filenameOpacity != group.opacity)
                    {
                        failures.Add($"{expectedType} {group.opacity}% contains {filename}");
                    }
                    if (variant != expectedVariant)
                    {
                        failures.Add($"{expectedType} {group.opacity}% contains unexpected V{variant} source {filename}");
                    }
                    if (sequence < 1 || sequence > 30)
                    {
                        failures.Add($"{expectedType} {group.opacity}% contains out-of-range sequence {sequence:D4}");
                    }
                    sequences.Add(sequence);
                }

                if (requireCompleteSequence)
                {
                    int[] missing = Enumerable.Range(1, 30).Where(sequence => !sequences.Contains(sequence)).ToArray();
                    if (missing.Length > 0)
                    {
                        failures.Add($"{expectedType} {group.opacity}% is missing sequences {string.Join(", ", missing.Select(value => value.ToString("D4")))}");
                    }
                }
            }
            return failures;
        }

        private static List<CatalogGroup> ReadGroups(UnityEngine.Object asset)
        {
            SerializedProperty smokeVideos = new SerializedObject(asset).FindProperty("smokeVideos");
            Assert.That(smokeVideos, Is.Not.Null);
            List<CatalogGroup> result = new List<CatalogGroup>();
            for (int groupIndex = 0; groupIndex < smokeVideos.arraySize; groupIndex++)
            {
                SerializedProperty group = smokeVideos.GetArrayElementAtIndex(groupIndex);
                int opacity = group.FindPropertyRelative("percentage").intValue;
                SerializedProperty types = group.FindPropertyRelative("types");
                for (int typeIndex = 0; typeIndex < types.arraySize; typeIndex++)
                {
                    SerializedProperty type = types.GetArrayElementAtIndex(typeIndex);
                    SerializedProperty urls = type.FindPropertyRelative("videoURLs");
                    result.Add(new CatalogGroup
                    {
                        opacity = opacity,
                        smokeType = type.FindPropertyRelative("typeName").stringValue,
                        urls = Enumerable.Range(0, urls.arraySize)
                            .Select(index => urls.GetArrayElementAtIndex(index).stringValue.Trim())
                            .ToList()
                    });
                }
            }
            return result;
        }
    }
}
