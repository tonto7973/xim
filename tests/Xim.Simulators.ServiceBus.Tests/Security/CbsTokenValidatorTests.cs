using System;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.ServiceBus.Security.Tests
{
    [TestFixture]
    public class CbsTokenValidatorTests
    {
        [Test]
        public void Constructor_Throws_WhenSharedAccessKeyNameNull()
        {
            Action action = () => new CbsTokenValidator(null, "abcd");

            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("sharedAccessKeyName");
        }

        [Test]
        public void Constructor_Throws_WhenSharedAccessKeyNull()
        {
            Action action = () => new CbsTokenValidator("name", null);

            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("sharedAccessKey");
        }

        [TestCase("a634b76x")]
        [TestCase("dvero")]
        public void Constructor_SetsSharedAccessKey(string testKey)
        {
            var validator = new CbsTokenValidator("", testKey);

            validator.SharedAccessKey.ShouldBe(testKey);
        }

        [TestCase("all")]
        [TestCase("single")]
        [TestCase("double")]
        public void Constructor_SetsSharedAccessKeyName(string testName)
        {
            var validator = new CbsTokenValidator(testName, "");

            validator.SharedAccessKeyName.ShouldBe(testName);
        }

        [Test]
        public void Default_ReturnsTheSameInstance()
        {
            var default1 = CbsTokenValidator.Default;
            var default2 = CbsTokenValidator.Default;

            default1.ShouldBeSameAs(default2);
        }

        [Test]
        public void Default_ReturnsWellKnownKey()
            => CbsTokenValidator
                .Default
                .SharedAccessKey
                .ShouldBe("CLwo3FQ3S39Z4pFOQDefaiUd1dSsli4XOAj3Y9Uh1E=");

        [Test]
        public void Default_ReturnsWellKnownKeyName()
            => CbsTokenValidator
                .Default
                .SharedAccessKeyName
                .ShouldBe("all");

        [TestCase(null)]
        [TestCase("")]
        public void Validate_Throws_WhenTokenEmpty(string testToken)
        {
            var validator = new CbsTokenValidator("name", "key");

            Action action = () => validator.Validate(testToken);

            var ex = action.ShouldThrow<ArgumentException>();

            ex.ParamName.ShouldBe("token");
            ex.Message.ShouldStartWith(SR.Format(SR.SbCbsTokenEmpty));
        }

        [Test]
        public void Validate_Throws_WhenTokenDoesNotStartWithSharedAccessSignature()
        {
            const string testToken = "Bearer 1234";
            var validator = new CbsTokenValidator("name", "key");

            Action action = () => validator.Validate(testToken);

            var ex = action.ShouldThrow<ArgumentException>();

            ex.ParamName.ShouldBe("token");
            ex.Message.ShouldStartWith(SR.Format(SR.SbCbsTokenNoSas, "SharedAccessSignature"));
        }

        [Test]
        public void Validate_Throws_WhenTokenDoesNotContainSignedResource()
        {
            const string testToken = "SharedAccessSignature a=b&se=1&skn=2&sig=3";
            var validator = new CbsTokenValidator("name", "key");

            Action action = () => validator.Validate(testToken);

            var ex = action.ShouldThrow<ArgumentException>();

            ex.ParamName.ShouldBe("token");
            ex.Message.ShouldStartWith(SR.Format(SR.SbCbsTokenNoResource, "sr="));
        }

        [Test]
        public void Validate_Throws_WhenTokenDoesNotContainSignedExpiry()
        {
            const string testToken = "SharedAccessSignature sr=b&a=1&skn=2&sig=3";
            var validator = new CbsTokenValidator("name", "key");

            Action action = () => validator.Validate(testToken);

            var ex = action.ShouldThrow<ArgumentException>();

            ex.ParamName.ShouldBe("token");
            ex.Message.ShouldStartWith(SR.Format(SR.SbCbsTokenNoExpiry, "se="));
        }

        [Test]
        public void Validate_Throws_WhenTokenDoesNotContainSignedKeyName()
        {
            const string testToken = "SharedAccessSignature sr=b&se=1&a=2&sig=3";
            var validator = new CbsTokenValidator("name", "key");

            Action action = () => validator.Validate(testToken);

            var ex = action.ShouldThrow<ArgumentException>();

            ex.ParamName.ShouldBe("token");
            ex.Message.ShouldStartWith(SR.Format(SR.SbCbsTokenNoKeyName, "skn="));
        }

        [Test]
        public void Validate_Throws_WhenTokenDoesNotContainSignature()
        {
            const string testToken = "SharedAccessSignature sr=b&se=1&skn=2&c=3&d";
            var validator = new CbsTokenValidator("name", "key");

            Action action = () => validator.Validate(testToken);

            var ex = action.ShouldThrow<ArgumentException>();

            ex.ParamName.ShouldBe("token");
            ex.Message.ShouldStartWith(SR.Format(SR.SbCbsTokenNoSignature, "sig="));
        }

        [Test]
        public void Validate_Throws_WhenTokenDoesNotContainValidKeyName()
        {
            const string testToken = "SharedAccessSignature sr=localhost&se=1&skn=meme&sig=fcfcb";
            var validator = new CbsTokenValidator("mee", "1234");

            Action action = () => validator.Validate(testToken);

            var ex = action.ShouldThrow<ArgumentException>();

            ex.ParamName.ShouldBe("token");
            ex.Message.ShouldStartWith(SR.Format(SR.SbCbsTokenNameInvalid, "skn=meme"));
        }

        [Test]
        public void Validate_Throws_WhenTokenDoesNotContainValidSignature()
        {
            const string expiresOn = "1528479";
            const string key = "7ysh4jfk69gdi8rj";
            const string name = "007";
            var resource = HttpUtility.UrlEncode("http://path.io/resource/2?a=b&c=d");
            var testToken = $"SharedAccessSignature sr={resource}&se={expiresOn}&skn={name}&sig=fcfcb";
            var validator = new CbsTokenValidator(name, key);

            Action action = () => validator.Validate(testToken);

            var ex = action.ShouldThrow<ArgumentException>();

            ex.ParamName.ShouldBe("token");
            ex.Message.ShouldStartWith(SR.Format(SR.SbCbsTokenSignatureInvalid, "sig=fcfcb"));
        }

        [TestCase("one", "key007", 1550655844, "http://resource.on/path/subpath/q487")]
        [TestCase("BigAdminUser", "bcthe86kfgjHtdg4jfn", 1550248554, "my-test-resource 24")]
        public void Validate_DoesNotThrow_WhenTokenValid(string testName, string testKey, int testExpiresOn, string testResource)
        {
            var resource = HttpUtility.UrlEncode(testResource);
            var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(testKey));
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{resource}\n{testExpiresOn}")));
            var testToken = $"SharedAccessSignature sr={resource}&se={testExpiresOn}&skn={testName}&sig={HttpUtility.UrlEncode(signature)}";

            var validator = new CbsTokenValidator(testName, testKey);

            Action action = () => validator.Validate(testToken);

            action.ShouldNotThrow();
        }
    }
}
