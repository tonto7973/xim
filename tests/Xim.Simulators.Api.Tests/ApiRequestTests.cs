﻿using System.IO;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.Api.Tests
{
    [TestFixture]
    public class ApiRequestTests
    {
        [Test]
        public void Constructor_SetsProperties()
        {
            var testBody = new MemoryStream();
            HttpRequest fakeHttpRequest = Substitute.For<HttpRequest>();
            fakeHttpRequest.Method.Returns("SEND");
            fakeHttpRequest.Path.Returns(new PathString("/path/to/somewhere"));
            fakeHttpRequest.QueryString.Returns(new QueryString("?val=43&name=aaa"));
            fakeHttpRequest.Headers.Returns(new HeaderDictionary { ["x"] = "y" });
            fakeHttpRequest.ContentType.Returns("my/type");
            fakeHttpRequest.Body.Returns(testBody);

            var apiRequest = new ApiRequest(fakeHttpRequest);

            apiRequest.ShouldSatisfyAllConditions(
                () => apiRequest.Method.ShouldBe("SEND"),
                () => apiRequest.Path.ShouldBe("/path/to/somewhere"),
                () => apiRequest.Query.ShouldBe("?val=43&name=aaa"),
                () => apiRequest.Headers["x"].ShouldBe("y"),
                () => apiRequest.Body.ContentType.ShouldBe("my/type"),
                () => apiRequest.Body.Content.ShouldBeSameAs(testBody)
            );
        }

        [Test]
        public void Constructor_DoesNotSetHeaderAndBody_WhenHeaderAndBodyNull()
        {
            HttpRequest fakeHttpRequest = Substitute.For<HttpRequest>();
            fakeHttpRequest.Method.Returns("RECV");
            fakeHttpRequest.Path.Returns(new PathString("/a/bc"));
            fakeHttpRequest.Headers.Returns((IHeaderDictionary)null);
            fakeHttpRequest.ContentType.Returns("app/empty");
            fakeHttpRequest.Body.Returns((Stream)null);

            var apiRequest = new ApiRequest(fakeHttpRequest);

            apiRequest.ShouldSatisfyAllConditions(
                () => apiRequest.Method.ShouldBe("RECV"),
                () => apiRequest.Path.ShouldBe("/a/bc"),
                () => apiRequest.Query.ShouldBeNull(),
                () => apiRequest.Headers.ShouldBeNull(),
                () => apiRequest.Body.ShouldBeNull()
            );
        }

        [TestCase("GET", "/books/32", "?great=32")]
        [TestCase("TEG", "/", null)]
        public void ToString_FormatsMethodPathAndQuery(string method, string path, string query)
        {
            HttpRequest fakeHttpRequest = Substitute.For<HttpRequest>();
            fakeHttpRequest.Method.Returns(method);
            fakeHttpRequest.Path.Returns(new PathString(path));
            fakeHttpRequest.QueryString.Returns(new QueryString(query));

            var apiRequest = new ApiRequest(fakeHttpRequest);

            apiRequest.ToString().ShouldBe($"{method} {path}{query}");
        }
    }
}
