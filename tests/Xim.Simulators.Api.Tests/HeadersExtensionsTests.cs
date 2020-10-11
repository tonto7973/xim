using System;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.Api.Tests
{
    [TestFixture]
    public class HeadersExtensionsTests
    {
        [Test]
        public void AddLocation_Throws_WhenHeadersIsNull()
        {
            Action action = () => HeadersExtensions.AddLocation(null, new Uri("http://addr.io"));

            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("headers");
        }

        [Test]
        public void AddLocation_Throws_WhenLocationIsNull()
        {
            Action action = () => new Headers().AddLocation(null);

            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("location");
        }

        [Test]
        public void AddLocation_ReturnsTheSameHeadersInstance()
        {
            var headers = new Headers();

            Headers result = headers.AddLocation(new Uri("https://location.com"));

            result.ShouldBeSameAs(headers);
        }

        [Test]
        public void AddLocation_AddsValidLocationHeader()
        {
            var headers = new Headers();
            var uri = new Uri("https://location.com");

            headers.AddLocation(uri);

            headers["Location"].ShouldBe(uri.AbsoluteUri);
        }

        [Test]
        public void AddLocation_AddsValidRelativeLocationHeader()
        {
            var headers = new Headers();
            Uri uri = new Uri("https://location.com/somepath/local?q=32").MakeRelativeUri(new Uri("https://location.com/"));
            var relativeUri = uri.GetComponents(UriComponents.SerializationInfoString,
                                                UriFormat.UriEscaped);

            headers.AddLocation(uri);

            headers["Location"].ShouldBe(relativeUri);
        }

        [Test]
        public void AddWwwAuthenticate_Throws_WhenHeadersNull()
        {
            Action action = () => HeadersExtensions.AddWwwAuthenticate(null, "Bearer");

            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("headers");
        }

        [TestCase(null)]
        [TestCase("")]
        public void AddWwwAuthenticate_Throws_WhenChallengeIsEmpty(string challenge)
        {
            Action action = () => new Headers().AddWwwAuthenticate(challenge);

            ArgumentException exception = action.ShouldThrow<ArgumentException>();
            exception.ParamName.ShouldBe("challenge");
            exception.Message.ShouldStartWith(SR.Format(SR.ApiHeaderChallengeEmpty));
        }

        [TestCase("simple")]
        [TestCase("bearer")]
        public void AddWwwAuthenticate_AddsValidHeader(string challenge)
        {
            var headers = new Headers();

            headers.AddWwwAuthenticate(challenge);

            headers["WWW-Authenticate"].ShouldBe(challenge);
        }

        [Test]
        public void AddWwwAuthenticate_ReturnsTheSameHeadersInstance()
        {
            var headers = new Headers();

            Headers result = headers.AddWwwAuthenticate("a");

            result.ShouldBeSameAs(headers);
        }
    }
}
