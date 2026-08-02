using System;
using System.IO;
using NUnit.Framework;

namespace SmokeSchool.Tests
{
    public class WebGLAuthenticationIntegrationTests
    {
        private const string ProductionLoginUrl = "https://smokeschool-dashboard.vercel.app/api/vr/login";

        [Test]
        public void WebGLTemplateAndPluginUseTheUnityLoginContract()
        {
            string template = File.ReadAllText("Assets/WebGLTemplates/WebXR2020/index.html");
            string config = File.ReadAllText("Assets/WebGLTemplates/WebXR2020/auth-config.js");
            string plugin = File.ReadAllText("Assets/Plugins/WebGL/SmokeSchoolAuth.jslib");

            Assert.That(template, Does.Contain("<script src=\"auth-config.js\"></script>"));
            Assert.That(template, Does.Contain("ReceiveBrowserLogin"));
            Assert.That(template, Does.Contain("email: emailInput.value.trim()"));
            Assert.That(template, Does.Contain("password: passwordInput.value"));
            Assert.That(template, Does.Not.Contain("id=\"auth-form\""));
            Assert.That(config, Does.Contain(ProductionLoginUrl));
            Assert.That(plugin, Does.Contain("authApi"));
            Assert.That(plugin, Does.Contain("window.SMOKE_SCHOOL_AUTH"));
        }

        [TestCase("https://smokeschool-dashboard.vercel.app/api/vr/login", "https://smokeschool-dashboard.vercel.app/api/vr/certification-attempts")]
        [TestCase("https://dashboard.example.com:8443/api/vr/login/?environment=test#ignored", "https://dashboard.example.com:8443/api/vr/certification-attempts")]
        public void CertificationResultUrl_IsDerivedFromApprovedLoginOrigin(string loginUrl, string expected)
        {
            object previous = TestReflection.GetStaticField("DataInput_Fields", "approvedAuthenticationUrl");
            try
            {
                TestReflection.SetStaticField("DataInput_Fields", "approvedAuthenticationUrl", loginUrl);
                Assert.That(TestReflection.InvokeStatic("DataInput_Fields", "GetCertificationResultUrl"), Is.EqualTo(expected));
            }
            finally
            {
                TestReflection.SetStaticField("DataInput_Fields", "approvedAuthenticationUrl", previous);
            }
        }

        [TestCase("")]
        [TestCase("not-a-url")]
        [TestCase("ftp://dashboard.example.com/api/vr/login")]
        [TestCase("https://dashboard.example.com/api/login")]
        [TestCase("https://dashboard.example.com/api/vr/login/extra")]
        public void CertificationResultUrl_RejectsUnapprovedLocations(string loginUrl)
        {
            object previous = TestReflection.GetStaticField("DataInput_Fields", "approvedAuthenticationUrl");
            try
            {
                TestReflection.SetStaticField("DataInput_Fields", "approvedAuthenticationUrl", loginUrl);
                Assert.That(TestReflection.InvokeStatic("DataInput_Fields", "GetCertificationResultUrl"), Is.EqualTo(string.Empty));
            }
            finally
            {
                TestReflection.SetStaticField("DataInput_Fields", "approvedAuthenticationUrl", previous);
            }
        }
    }
}
