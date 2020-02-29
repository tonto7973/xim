using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.Api.Tests
{
    [TestFixture]
    public class ApiResponseTests
    {
        [TestCase(200, "Okay", null, null)]
        [TestCase(409, null, "X-Operation: Revoke\r\nLocation: home", "{}")]
        public void Constructor_SetsRequiredProperties(int statusCode, string reasonPhrase, string httpHeaders, string data)
        {
            var headers = httpHeaders == null ? new Headers() : Headers.FromString(httpHeaders);
            var body = data == null ? null : Body.FromString(data);

            var apiResponse = new ApiResponse(statusCode, reasonPhrase, headers, body);

            apiResponse.ShouldSatisfyAllConditions(
                () => apiResponse.StatusCode.ShouldBe(statusCode),
                () => apiResponse.ReasonPhrase.ShouldBe(reasonPhrase),
                () => apiResponse.Headers.ShouldBeSameAs(headers),
                () => apiResponse.Body.ShouldBeSameAs(body)
            );
        }

        [Test]
        public void Constructor_CreatesHeaders_WhenHeadersArgumentIsNull()
        {
            var response = new ApiResponse(204, headers: null);

            response.Headers.ShouldNotBeNull();
        }

        [TestCase(201, "Made")]
        [TestCase(404, "Not There")]
        public void ToString_FormatsCodeAndPhrase(int code, string phrase)
        {
            var expectedResponse = $"HTTP {code} {phrase}";
            var response = new ApiResponse(code, reasonPhrase: phrase);

            response.ToString().ShouldBe(expectedResponse);
        }

        [TestCase(502)]
        [TestCase(400)]
        public void ToString_UsesDefaultPhrase_WhenPhraseIsNull(int code)
        {
            var expectedResponse = $"HTTP {code} {ReasonPhrases.GetReasonPhrase(code)}";
            var response = new ApiResponse(code, reasonPhrase: null);

            response.ToString().ShouldBe(expectedResponse);
        }

        [Test]
        public void Dispose_DisposesBody()
        {
            var body = Substitute.ForPartsOf<Body>(new object(), "any");
            var apiResponse = new ApiResponse(200, null, null, body);

            apiResponse.Dispose();

            body.Received(1).Dispose();
        }

        [Test]
        public void Dispose_DisposesBodyOnlyOnce_WhenCalledMultipleTimes()
        {
            var body = Substitute.ForPartsOf<Body>(new object(), "any");
            var apiResponse = new ApiResponse(302, body: body);

#pragma warning disable S3966 // Objects should not be disposed more than once - required for unit test
            apiResponse.Dispose();
            apiResponse.Dispose();
            apiResponse.Dispose();
            apiResponse.Dispose();
#pragma warning restore S3966 // Objects should not be disposed more than once - required for unit test

            body.Received(1).Dispose();
        }

        [TestCase(401)]
        [TestCase(100)]
        public async Task WriteAsync_SetsCorrectStatusCode(int statusCode)
        {
            var context = new DefaultHttpContext();
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var settings = new ApiSimulatorSettings(apiBuilder);
            var apiResponse = new ApiResponse(statusCode);

            await apiResponse.WriteAsync(context, settings);

            context.Response.StatusCode.ShouldBe(statusCode);
        }

        [TestCase("Forbidden")]
        [TestCase("Tokenizer")]
        [TestCase(null)]
        public async Task WriteAsync_SetsCorrectReasonPhrase(string reasonPhrase)
        {
            var context = new DefaultHttpContext();
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var settings = new ApiSimulatorSettings(apiBuilder);
            var apiResponse = new ApiResponse(403, reasonPhrase);

            await apiResponse.WriteAsync(context, settings);

            context.Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase.ShouldBe(reasonPhrase);
        }

        [Test]
        public async Task WriteAsync_SetsCorrectHeaders()
        {
            var context = new DefaultHttpContext();
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var settings = new ApiSimulatorSettings(apiBuilder);
            var apiResponse = new ApiResponse(200, headers: new Headers
            {
                ["X-Colour"] = "red,blue",
                ["X-Colour"] = "green",
                ["NameId"] = "123"
            });

            await apiResponse.WriteAsync(context, settings);
            var responseHeaders = context.Response.Headers;

            responseHeaders.ShouldSatisfyAllConditions(
                () => responseHeaders.Count.ShouldBe(2),
                () => ((string)responseHeaders["X-Colour"]).ShouldBe("red,blue,green"),
                () => ((string)responseHeaders["NameId"]).ShouldBe("123")
            );
        }

        [Test]
        public async Task WriteAsync_WritesBody()
        {
            var context = new DefaultHttpContext();
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var settings = new ApiSimulatorSettings(apiBuilder);
            var body = Substitute.ForPartsOf<TestBody>();
            var apiResponse = new ApiResponse(200, body: body);

            await apiResponse.WriteAsync(context, settings);

            body.Received(1).Written(context, settings);
        }

        [TestCase(null)]
        [TestCase("Text body")]
        public void Ok_SetsProperApiResponse_WhenBodyIs(string httpBody)
        {
            var body = httpBody == null ? null : Body.FromString(httpBody);

            var response = ApiResponse.Ok(body);

            response.ShouldSatisfyAllConditions(
                () => response.StatusCode.ShouldBe(200),
                () => response.ReasonPhrase.ShouldBeNull(),
                () => response.Headers.Count.ShouldBe(0),
                () => response.Body.ShouldBeSameAs(body)
            );
        }

        [TestCase("ETag: 1234", null)]
        [TestCase(null, "Text body")]
        public void OkWithHeaders_SetsProperApiResponse_WhenBodyIs(string httpHeaders, string httpBody)
        {
            var headers = httpHeaders == null ? new Headers() : Headers.FromString(httpHeaders);
            var body = httpBody == null ? null : Body.FromString(httpBody);

            var response = ApiResponse.Ok(headers, body);

            response.ShouldSatisfyAllConditions(
                () => response.StatusCode.ShouldBe(200),
                () => response.ReasonPhrase.ShouldBeNull(),
                () => response.Headers.ShouldBeSameAs(headers),
                () => response.Body.ShouldBeSameAs(body)
            );
        }

        [Test]
        public void OkTBody_SetsProperApiResponse()
        {
            var body = new { Id = 32 };

            var response = ApiResponse.Ok(body);

            response.ShouldSatisfyAllConditions(
                () => response.StatusCode.ShouldBe(200),
                () => response.ReasonPhrase.ShouldBeNull(),
                () => response.Headers.Count.ShouldBe(0),
                () => response.Body.Content.ShouldBeSameAs(body)
            );
        }

        [TestCase(null)]
        [TestCase("X-Data: tom")]
        public void OkTBody_SetsProperApiResponse_WhenHeaderIs(string httpHeaders)
        {
            var headers = httpHeaders == null ? new Headers() : Headers.FromString(httpHeaders);
            var body = new { Id = 32 };

            var response = ApiResponse.Ok(headers, body);

            response.ShouldSatisfyAllConditions(
                () => response.StatusCode.ShouldBe(200),
                () => response.ReasonPhrase.ShouldBeNull(),
                () => response.Headers.ShouldBeSameAs(headers),
                () => response.Body.Content.ShouldBeSameAs(body)
            );
        }

        [TestCase("17", null)]
        [TestCase(null, "some body")]
        public void Created_SetsProperApiResponse(string id, string httpBody)
        {
            var body = httpBody == null ? null : Body.FromString(httpBody);
            var location = new Uri($"https://rest.io/items/{id}");

            var response = ApiResponse.Created(location, body);

            response.ShouldSatisfyAllConditions(
                () => response.StatusCode.ShouldBe(201),
                () => response.ReasonPhrase.ShouldBeNull(),
                () => response.Headers["Location"].ShouldBe(location.AbsoluteUri),
                () => response.Headers.Count.ShouldBe(1),
                () => response.Body.ShouldBeSameAs(body)
            );
        }

        [TestCase("229a")]
        [TestCase(null)]
        public void Created_SetsProperApiResponse(string id)
        {
            var body = new { Name = id };
            var location = new Uri($"https://urls.co/forms/{id}");

            var response = ApiResponse.Created(location, body);

            response.ShouldSatisfyAllConditions(
                () => response.StatusCode.ShouldBe(201),
                () => response.ReasonPhrase.ShouldBeNull(),
                () => response.Headers["Location"].ShouldBe(location.AbsoluteUri),
                () => response.Headers.Count.ShouldBe(1),
                () => response.Body.Content.ShouldBeSameAs(body)
            );
        }

        [TestCase("a212", null)]
        [TestCase(null, "request accepted")]
        public void Accepted_SetsProperApiResponse(string id, string httpBody)
        {
            var body = httpBody == null ? null : Body.FromString(httpBody);
            var location = new Uri($"http://localhost:801/data/{id}");

            var response = ApiResponse.Accepted(location, body);

            response.ShouldSatisfyAllConditions(
                () => response.StatusCode.ShouldBe(202),
                () => response.ReasonPhrase.ShouldBeNull(),
                () => response.Headers["Location"].ShouldBe(location.AbsoluteUri),
                () => response.Headers.Count.ShouldBe(1),
                () => response.Body.ShouldBeSameAs(body)
            );
        }

        [TestCase("X534S")]
        [TestCase(null)]
        public void Accepted_SetsProperApiResponse(string id)
        {
            var body = new { Blob = id };
            var location = new Uri($"http://blobs.azure.com/blobs/{id}").MakeRelativeUri(new Uri($"http://blobs.azure.com/"));

            var expectedLocation = location.GetComponents(UriComponents.SerializationInfoString,
                                                          UriFormat.UriEscaped);

            var response = ApiResponse.Accepted(location, body);

            response.ShouldSatisfyAllConditions(
                () => response.StatusCode.ShouldBe(202),
                () => response.ReasonPhrase.ShouldBeNull(),
                () => response.Headers["Location"].ShouldBe(expectedLocation),
                () => response.Headers.Count.ShouldBe(1),
                () => response.Body.Content.ShouldBeSameAs(body)
            );
        }

        [TestCase("Invalid title")]
        [TestCase(null)]
        public void BadRequest_SetsProperApiResponse(string httpBody)
        {
            var body = httpBody == null ? null : Body.FromString(httpBody);

            var response = ApiResponse.BadRequest(body);

            response.ShouldSatisfyAllConditions(
                () => response.StatusCode.ShouldBe(400),
                () => response.ReasonPhrase.ShouldBeNull(),
                () => response.Headers.Count.ShouldBe(0),
                () => response.Body.ShouldBeSameAs(body)
            );
        }

        [Test]
        public void BadRequestTBody_SetsProperApiResponse()
        {
            var body = new { title = "abc" };

            var response = ApiResponse.BadRequest(body);

            response.ShouldSatisfyAllConditions(
                () => response.StatusCode.ShouldBe(400),
                () => response.ReasonPhrase.ShouldBeNull(),
                () => response.Headers.Count.ShouldBe(0),
                () => response.Body.Content.ShouldBeSameAs(body)
            );
        }

        [TestCase("some body")]
        [TestCase(null)]
        public void Unathorized_SetsProperApiResponse(string httpBody)
        {
            var body = httpBody == null ? null : Body.FromString(httpBody);

            var response = ApiResponse.Unauthorized(body);

            response.ShouldSatisfyAllConditions(
                () => response.StatusCode.ShouldBe(401),
                () => response.ReasonPhrase.ShouldBeNull(),
                () => response.Headers.Count.ShouldBe(0),
                () => response.Body.ShouldBeSameAs(body)
            );
        }

        [TestCase("digest")]
        [TestCase("basic")]
        [TestCase(null)]
        public void Unathorized_SetsProperApiResponse_WhenChallengeIs(string challenge)
        {
            var body = challenge == null ? null : Body.FromString(challenge);
            challenge = challenge ?? "bearer";

            var response = ApiResponse.Unauthorized(challenge, body);

            response.ShouldSatisfyAllConditions(
                () => response.StatusCode.ShouldBe(401),
                () => response.ReasonPhrase.ShouldBeNull(),
                () => response.Headers.Count.ShouldBe(1),
                () => response.Headers["WWW-Authenticate"].ShouldBe(challenge),
                () => response.Body.ShouldBeSameAs(body)
            );
        }

        [Test]
        public void UnathorizedTBody_SetsProperApiResponse()
        {
            var body = new { Name = "Lor" };

            var response = ApiResponse.Unauthorized(body);

            response.ShouldSatisfyAllConditions(
                () => response.StatusCode.ShouldBe(401),
                () => response.ReasonPhrase.ShouldBeNull(),
                () => response.Headers.Count.ShouldBe(0),
                () => response.Body.Content.ShouldBeSameAs(body)
            );
        }

        [TestCase("single")]
        [TestCase("Bearer error=\"not valid\"")]
        public void UnathorizedTBody_SetsProperApiResponse_WhenChallengeIs(string challenge)
        {
            var body = new { Tom = "mot" };

            var response = ApiResponse.Unauthorized(challenge, body);

            response.ShouldSatisfyAllConditions(
                () => response.StatusCode.ShouldBe(401),
                () => response.ReasonPhrase.ShouldBeNull(),
                () => response.Headers.Count.ShouldBe(1),
                () => response.Headers["WWW-Authenticate"].ShouldBe(challenge),
                () => response.Body.Content.ShouldBeSameAs(body)
            );
        }

        [TestCase("something")]
        [TestCase(null)]
        public void Forbidden_SetsProperApiResponse(string httpBody)
        {
            var body = httpBody == null ? null : Body.FromString(httpBody);

            var response = ApiResponse.Forbidden(body);

            response.ShouldSatisfyAllConditions(
                () => response.StatusCode.ShouldBe(403),
                () => response.ReasonPhrase.ShouldBeNull(),
                () => response.Headers.Count.ShouldBe(0),
                () => response.Body.ShouldBeSameAs(body)
            );
        }

        [Test]
        public void ForbiddenTBody_SetsProperApiResponse()
        {
            var body = new { age = 777 };

            var response = ApiResponse.Forbidden(body);

            response.ShouldSatisfyAllConditions(
                () => response.StatusCode.ShouldBe(403),
                () => response.ReasonPhrase.ShouldBeNull(),
                () => response.Headers.Count.ShouldBe(0),
                () => response.Body.Content.ShouldBeSameAs(body)
            );
        }

        [TestCase(null)]
        [TestCase("resource not found")]
        public void NotFound_SetsProperApiResponse(string httpBody)
        {
            var body = httpBody == null ? null : Body.FromString(httpBody);

            var response = ApiResponse.NotFound(body);

            response.ShouldSatisfyAllConditions(
                () => response.StatusCode.ShouldBe(404),
                () => response.ReasonPhrase.ShouldBeNull(),
                () => response.Headers.Count.ShouldBe(0),
                () => response.Body.ShouldBeSameAs(body)
            );
        }

        [Test]
        public void NotFoundTBody_SetsProperApiResponse()
        {
            var body = new { foo = true };

            var response = ApiResponse.NotFound(body);

            response.ShouldSatisfyAllConditions(
                () => response.StatusCode.ShouldBe(404),
                () => response.ReasonPhrase.ShouldBeNull(),
                () => response.Headers.Count.ShouldBe(0),
                () => response.Body.Content.ShouldBeSameAs(body)
            );
        }

        public abstract class TestBody : Body
        {
            protected TestBody() : base("22", "any")
            {
            }

            protected override Task WriteAsync(HttpContext context, ApiSimulatorSettings settings)
            {
                Written(context, settings);
                return Task.CompletedTask;
            }

            public abstract void Written(HttpContext context, ApiSimulatorSettings settings);
        }
    }
}
