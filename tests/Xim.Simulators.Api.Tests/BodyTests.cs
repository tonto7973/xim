using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NUnit.Framework;
using Shouldly;
using Xim.Simulators.Api.Routing;

namespace Xim.Simulators.Api.Tests
{
    [TestFixture]
    public class BodyTests
    {
        [Test]
        public void Dispose_DisposesStreamContent()
        {
            var value = Substitute.For<MemoryStream>();
            var body = Body.FromStream(value);

            body.Dispose();

            value.Received(1).Dispose();
        }

        [Test]
        public void Dispose_DoesNotDisposesStreamContent_WhenLeaveOpenIsTrue()
        {
            var value = Substitute.For<MemoryStream>();
            var body = Body.FromStream(value, leaveOpen: true);

            body.Dispose();

            value.DidNotReceive().Dispose();
        }

        [Test]
        public void Dispose_DisposesStreamContentOnlyOnce_WhenCalledMultipleTimes()
        {
            var value = Substitute.For<MemoryStream>();
            var body = Body.FromStream(value);

#pragma warning disable S3966 // Objects should not be disposed more than once - required for unit test
            body.Dispose();
            body.Dispose();
            body.Dispose();
#pragma warning restore S3966

            value.Received(1).Dispose();
        }

        [Test]
        public void FromString_Throws_WhenStringNull()
        {
            Action action = () => Body.FromString(null);

            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("httpBody");
        }

        [Test]
        public void FromString_SetsCorrectDefaultContentTypeAndCharset()
        {
            var body = Body.FromString("hello");

            body.ContentType.ShouldBe("text/plain; charset=utf-8");
        }

        [Test]
        public void FromString_SetsCorrectContentTypeUsingEncodingAndMediaType()
        {
            var body = Body.FromString("hello", Encoding.ASCII, "text/html");

            body.ContentType.ShouldBe("text/html; charset=" + Encoding.ASCII.HeaderName);
        }

        [TestCase("utf-8")]
        [TestCase("utf-16")]
        [TestCase("ascii")]
        public async Task FromString_WritesCorrectBody(string codepage)
        {
            const string text = "world 32 טרימ";
            var encoding = Encoding.GetEncoding(codepage);
            var memoryStream = new MemoryStream();
            var context = new DefaultHttpContext();
            context.Response.Body = memoryStream;
            var settings = new ApiSimulatorSettings(new ApiBuilder(Substitute.For<ISimulation>()));
            var body = Body.FromString(text, encoding);

            await body.InternalWriteAsync(context, settings);

            memoryStream.ToArray().ShouldBe(encoding.GetBytes(text));
        }

        [Test]
        public void FromStream_Throws_WhenStreamNull()
        {
            Action action = () => Body.FromStream(null);
            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("stream");
        }

        [Test]
        public void FromStream_SetsNullContentType_WhenContentTypeNull()
        {
            var body = Body.FromStream(new MemoryStream(new byte[] { 0 }));

            body.ContentType.ShouldBeNull();
        }

        [Test]
        public void FromStream_SetsCustomContentType_WhenContentTypeSet()
        {
            var body = Body.FromStream(new MemoryStream(new byte[] { 0 }), contentType: "app/kusto");

            body.ContentType.ShouldBe("app/kusto");
        }

        [Test]
        public async Task FromStream_WritesOctetStreamContentType_WhenContentTypeNull()
        {
            var context = new DefaultHttpContext();
            var settings = new ApiSimulatorSettings(new ApiBuilder(Substitute.For<ISimulation>()));
            var body = Body.FromStream(new MemoryStream(new byte[] { 0 }));

            await body.InternalWriteAsync(context, settings);

            context.Response.ContentType.ShouldBe("application/octet-stream");
        }

        [Test]
        public void FromStream_SetsValidContentType_WhenContentTypeSet()
        {
            const string contentType = "food/gnocci";
            var body = Body.FromStream(new MemoryStream(), contentType);

            body.ContentType.ShouldBe(contentType);
        }

        [Test]
        public async Task FromStream_WritesValidContentType_WhenContentTypeSet()
        {
            const string contentType = "drink/cocacola";
            var context = new DefaultHttpContext();
            var settings = new ApiSimulatorSettings(new ApiBuilder(Substitute.For<ISimulation>()));
            var body = Body.FromStream(new MemoryStream(), contentType);

            await body.InternalWriteAsync(context, settings);

            context.Response.ContentType.ShouldBe(contentType);
        }

        [Test]
        public async Task FromStream_WritesValidContentLength_WhenContentLengthSet([Values(4, 12, 22)]long length)
        {
            var context = new DefaultHttpContext();
            var responseStream = new MemoryStream();
            var settings = new ApiSimulatorSettings(new ApiBuilder(Substitute.For<ISimulation>()));
            var bytes = Encoding.UTF8.GetBytes("Hello world!");
            var body = Body.FromStream(new MemoryStream(bytes), "test/abc", length);
            var expectedLength = Math.Min(length, bytes.Length);
            context.Response.Body = responseStream;

            await body.InternalWriteAsync(context, settings);

            context.Response.ContentType.ShouldBe("test/abc");
            context.Response.ContentLength.ShouldBe(length);
            responseStream.Length.ShouldBe(expectedLength);
        }

        [Test]
        public void FromObject_AcceptsNull()
        {
            Action action = () => Body.FromObject((string)null);

            action.ShouldNotThrow();
        }

        [Test]
        public void FromObject_SetsContentTypeToNull()
        {
            var body = Body.FromObject(new { Name = "au" });

            body.ContentType.ShouldBeNull();
        }

        [TestCase(null)]
        [TestCase("*/*")]
        [TestCase("application/json")]
        [TestCase("text/html, application/xhtml+xml, application/xml;q=0.9, */*;q=0.8")]
        [TestCase("text/html, application/xhtml+xml, application/xml;q=0.9, application/json")]
        public async Task FromObject_WriteSerializesToJson_WhenAcceptContainsJsonOrAnyWithBothXmlAndJsonSettingsSet(string testAcceptHeader)
        {
            var memoryStream = new MemoryStream();
            var context = new DefaultHttpContext();
            context.Request.Headers.Remove("Accept");
            if (testAcceptHeader != null)
            {
                context.Request.Headers["Accept"] = testAcceptHeader;
            }
            context.Response.Body = memoryStream;
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>())
                .SetJsonSettings(new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })
                .SetXmlSettings(new XmlWriterSettings());
            var settings = new ApiSimulatorSettings(apiBuilder);
            var body = Body.FromObject(new { Age = 32 });

            await body.InternalWriteAsync(context, settings);

            var bytes = memoryStream.ToArray();
            var content = Encoding.UTF8.GetString(bytes);

            content.ShouldBe("{\"age\":32}");
            context.Response.ContentType.ShouldBe("application/json; charset=utf-8");
            context.Response.ContentLength.ShouldBe(bytes.Length);
        }

        [TestCase(null, "iso-8859-1")]
        [TestCase("application/xml", null)]
        [TestCase("text/html, application/xhtml+xml, application/xml;q=0.9", "ascii")]
        public async Task FromObject_WriteAlwaysSerializesToJson_WhenXmlSettingsNotSet(string testAcceptHeader, string codepage)
        {
            var encoding = codepage != null ? Encoding.GetEncoding(codepage) : null;
            var testObject = new { FirstName = "Gústo" };
            var memoryStream = new MemoryStream();
            var context = new DefaultHttpContext();
            context.Request.Headers.Remove("Accept");
            if (testAcceptHeader != null)
            {
                context.Request.Headers["Accept"] = testAcceptHeader;
            }
            context.Response.Body = memoryStream;
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>())
                .SetJsonSettings(new JsonSerializerOptions
                {
                    WriteIndented = false
                })
                .SetXmlSettings(null);
            var settings = new ApiSimulatorSettings(apiBuilder);
            var body = Body.FromObject(testObject, encoding);

            await body.InternalWriteAsync(context, settings);

            var bytes = memoryStream.ToArray();
            var expectedBytes = (encoding ?? Encoding.UTF8).GetBytes(JsonSerializer.Serialize(testObject, apiBuilder.JsonSettings));

            bytes.ShouldBe(expectedBytes);
            context.Response.ContentType.ShouldBe("application/json; charset=" + (encoding ?? Encoding.UTF8).HeaderName);
            context.Response.ContentLength.ShouldBe(bytes.Length);
        }

        [TestCase(null, "iso-8859-1")]
        [TestCase("*/*", "utf-32")]
        [TestCase("application/json", null)]
        [TestCase("text/html, application/xhtml+xml, application/xml;q=0.9, */*;q=0.8", "ascii")]
        [TestCase("text/html, application/xhtml+xml, application/xml;q=0.9, application/json", "utf-16")]
        public async Task FromObject_WriteAlwaysSerializesToXml_WhenJsonSettingsNotSet(string testAcceptHeader, string codepage)
        {
            var encoding = codepage != null ? Encoding.GetEncoding(codepage) : null;
            var testObject = new Bucket { Key = "5437áe" };
            var memoryStream = new MemoryStream();
            var context = new DefaultHttpContext();
            context.Request.Headers.Remove("Accept");
            if (testAcceptHeader != null)
            {
                context.Request.Headers["Accept"] = testAcceptHeader;
            }
            context.Response.Body = memoryStream;
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>())
                .SetJsonSettings(null)
                .SetXmlSettings(new XmlWriterSettings
                {
                    Encoding = encoding == null ? null : Encoding.UTF8,
                    OmitXmlDeclaration = true
                });
            var settings = new ApiSimulatorSettings(apiBuilder);
            var body = Body.FromObject(testObject, encoding);

            await body.InternalWriteAsync(context, settings);

            var bytes = memoryStream.ToArray();
            var expectedBytes = testObject.ToXml(apiBuilder.XmlSettings, encoding ?? Encoding.UTF8);

            bytes.ShouldBe(expectedBytes);
            context.Response.ContentType.ShouldBe("application/xml; charset=" + (encoding ?? Encoding.UTF8).HeaderName);
            context.Response.ContentLength.ShouldBe(bytes.Length);
        }

        [Test]
        public async Task WriteInternalAsync_SerializesErrorToXml()
        {
            var memoryStream = new MemoryStream();
            var context = new DefaultHttpContext();
            context.Response.Body = memoryStream;
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>())
                .SetJsonSettings(null)
                .SetXmlSettings(new XmlWriterSettings
                {
                    Encoding = Encoding.UTF8,
                    OmitXmlDeclaration = true
                });
            var settings = new ApiSimulatorSettings(apiBuilder);

            var body = Body.FromObject(new Error
            {
                Title = "Some error",
                ["key"] = "Key is not valid"
            });

            await body.InternalWriteAsync(context, settings);

            var bytes = memoryStream.ToArray();
            var content = Encoding.UTF8.GetString(bytes);

            content.ShouldBe("﻿<Error><Title>Some error</Title><Reason Name=\"key\">Key is not valid</Reason></Error>");
        }

        [Test]
        public async Task WriteInternalAsync_SerializesErrorToJson()
        {
            var memoryStream = new MemoryStream();
            var context = new DefaultHttpContext();
            context.Response.Body = memoryStream;
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>())
                .SetJsonSettings(new JsonSerializerOptions())
                .SetXmlSettings(null);
            var settings = new ApiSimulatorSettings(apiBuilder);
            var body = Body.FromObject(new Error
            {
                ["{id}"] = "Key is not valid",
                Title = "Some error message"
            });

            await body.InternalWriteAsync(context, settings);

            var bytes = memoryStream.ToArray();
            var content = Encoding.UTF8.GetString(bytes);

            content.ShouldBe(@"{""error"":""Some error message"",""reasons"":{""{id}"":""Key is not valid""}}");
        }

        [TestCase("application/xml")]
        [TestCase("text/html, application/xhtml+xml, application/xml;q=0.9")]
        public async Task FromObject_WriteSerializesToXml_WhenOnlyXmlHeaderPresent(string testAcceptHeader)
        {
            var memoryStream = new MemoryStream();
            var context = new DefaultHttpContext();
            context.Request.Headers.Remove("Accept");
            if (testAcceptHeader != null)
            {
                context.Request.Headers["Accept"] = testAcceptHeader;
            }
            context.Response.Body = memoryStream;
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>())
                .SetJsonSettings(new JsonSerializerOptions())
                .SetXmlSettings(new XmlWriterSettings
                {
                    Encoding = Encoding.UTF8,
                    OmitXmlDeclaration = true
                });
            var settings = new ApiSimulatorSettings(apiBuilder);
            var body = Body.FromObject(new Bucket { Key = "225" });

            await body.InternalWriteAsync(context, settings);

            var bytes = memoryStream.ToArray();
            var content = Encoding.UTF8.GetString(bytes);

            content.ShouldBe("﻿<Bucket><Key>225</Key></Bucket>");
            context.Response.ContentType.ShouldBe("application/xml; charset=utf-8");
            context.Response.ContentLength.ShouldBe(bytes.Length);
        }

        [Test]
        public void FromObject_WriteThrows_WhenNoFormatterSet()
        {
            var context = new DefaultHttpContext();
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>())
                .SetJsonSettings(null)
                .SetXmlSettings(null);
            var settings = new ApiSimulatorSettings(apiBuilder);
            var body = Body.FromObject(new { Alpha = true });

            Action action = () => body.InternalWriteAsync(context, settings);

            action.ShouldThrow<FormatException>()
                .Message.ShouldBe(SR.Format(SR.ApiResponseNotFormatted));
        }

        [Test]
        public void ToString_FormatsBody_WhenNoContentTypeAndLengthSet()
        {
            var content = new { Id = 1234 };
            var expectedResult = JsonSerializer.Serialize(content, content.GetType(), new JsonSerializerOptions { WriteIndented = true });
            var body = Body.FromObject(new { Id = 1234 });

            var result = body.ToString();

            result.ShouldBe(expectedResult);
        }

        [Test]
        public void ToString_FormatsBodyWithContentType_WhenContentTypeSet()
        {
            var body = Body.FromString("1234", Encoding.ASCII, "text/ble");

            var result = body.ToString();

            result.ShouldBe("Content-Type: text/ble; charset=us-ascii\nContent-Length: 4\n\n1234");
        }

        [Test]
        public void ReadAsStream_ReturnsStream_WhenBodyWrapsStream()
        {
            var stream = new MemoryStream();
            var body = Body.FromStream(stream);

            var result = body.ReadAsStream();

            result.ShouldBeSameAs(stream);
        }

        [Test]
        public void ReadAsStream_ReturnsMemoryStream_WhenBodyWrapsObject()
        {
            var obj = new { Id = 4 };
            var body = Body.FromObject(obj);

            var result = body.ReadAsStream();

            var ms = result.ShouldBeOfType<MemoryStream>();
            ms.Position.ShouldBe(0);
            Encoding.ASCII.GetString(ms.ToArray()).ShouldBe("{\r\n  \"Id\": 4\r\n}");
        }

        [Test]
        public void ReadAsStream_ReturnsInternalStringStream_WhenBodyWrapsString()
        {
            var body = Body.FromString("A body", Encoding.ASCII);

            var result = body.ReadAsStream();

            result.GetType().FullName.ShouldBe("Xim.Simulators.Api.Body+InternalStringStream");
        }

        public class Bucket
        {
            public string Key { get; set; }

            internal byte[] ToXml(XmlWriterSettings xmlSettings, Encoding encoding)
            {
                var xmlSerializer = new XmlSerializer(typeof(Bucket));
                using (var memoryStream = new MemoryStream())
                using (var streamWriter = new StreamWriter(memoryStream, encoding))
                using (var xmlWriter = XmlWriter.Create(streamWriter, xmlSettings))
                {
                    var namespaces = new XmlSerializerNamespaces();
                    namespaces.Add(string.Empty, string.Empty);
                    xmlSerializer.Serialize(xmlWriter, this, namespaces);
                    return memoryStream.ToArray();
                }
            }
        }
    }
}
