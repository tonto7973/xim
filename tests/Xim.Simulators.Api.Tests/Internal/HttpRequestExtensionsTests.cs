using System.Text;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.Api.Internal
{
    [TestFixture]
    public class HttpRequestExtensionsTests
    {
        [Test]
        public void GetCharset_ReturnsNull_WhenRequestNull()
        {
            Encoding result = HttpRequestExtensions.GetCharset(null);

            result.ShouldBeNull();
        }

        [Test]
        public void GetCharset_ReturnsNull_WhenRequestContentTypeNull()
        {
            Encoding result = new DefaultHttpContext().Request.GetCharset();

            result.ShouldBeNull();
        }

        [Test]
        public void GetCharset_ReturnsNull_WhenRequestContentTypeEmpty()
        {
            var context = new DefaultHttpContext();
            context.Request.ContentType = "";

            Encoding result = context.Request.GetCharset();

            result.ShouldBeNull();
        }

        [Test]
        public void GetCharset_ReturnsNull_WhenRequestContentTypeInvalid()
        {
            var context = new DefaultHttpContext();
            context.Request.ContentType = "text; charzet-x";

            Encoding result = context.Request.GetCharset();

            result.ShouldBeNull();
        }

        [Test]
        public void GetCharset_ReturnsNull_WhenCharsetNotSpecified()
        {
            var context = new DefaultHttpContext();
            context.Request.ContentType = "text/html";

            Encoding result = context.Request.GetCharset();

            result.ShouldBeNull();
        }

        [Test]
        public void GetCharset_ReturnsEncoding_WhenRequestContentTypeSetWithJson()
        {
            var context = new DefaultHttpContext();
            context.Request.ContentType = "application/json; charset=" + Encoding.ASCII.HeaderName;

            Encoding result = context.Request.GetCharset();

            result.ShouldBe(Encoding.ASCII);
        }

        [Test]
        public void GetCharset_ReturnsEncoding_WhenRequestContentTypeSetWithHtml()
        {
            var context = new DefaultHttpContext();
            context.Request.ContentType = "text/html; charset=" + Encoding.UTF8.HeaderName;

            Encoding result = context.Request.GetCharset();

            result.ShouldBe(Encoding.UTF8);
        }

        [Test]
        public void GetMediaType_ReturnsNull_WhenRequestNull()
        {
            var result = HttpRequestExtensions.GetMediaType(null);

            result.ShouldBeNull();
        }

        [Test]
        public void GetMediaType_ReturnsNull_WhenRequestContentTypeNull()
        {
            var result = new DefaultHttpContext().Request.GetMediaType();

            result.ShouldBeNull();
        }

        [Test]
        public void GetMediaType_ReturnsNull_WhenRequestContentTypeEmpty()
        {
            var context = new DefaultHttpContext();
            context.Request.ContentType = "";

            var result = context.Request.GetMediaType();

            result.ShouldBeNull();
        }

        [Test]
        public void GetMediaType_ReturnsNull_WhenRequestContentTypeInvalid()
        {
            var context = new DefaultHttpContext();
            context.Request.ContentType = "test; foo-bar";

            var result = context.Request.GetMediaType();

            result.ShouldBeNull();
        }

        [TestCase("application/json")]
        [TestCase("text/html")]
        [TestCase("application/xml")]
        public void GetMediaType_ReturnsMediaType_WhenRequestContentTypeSetWithMediaType(string mediaType)
        {
            var context = new DefaultHttpContext();
            context.Request.ContentType = $"{mediaType}; charset={Encoding.ASCII.HeaderName}";

            var result = context.Request.GetMediaType();

            result.ShouldBe(mediaType);
        }
    }
}
