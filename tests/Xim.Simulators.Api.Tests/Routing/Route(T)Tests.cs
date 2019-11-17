using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using Shouldly;

#pragma warning disable RCS1210 // Return Task.FromResult instead of returning null.

namespace Xim.Simulators.Api.Routing.Tests
{
    [TestFixture]
    public class Route_T_Tests
    {
        [Test]
        public void Constructor_Throws_WhenActionNull()
        {
            Action action = () => new Route<int>(null, (_, __) => null);
            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("action");
        }

        [Test]
        public void Constructor_Throws_WhenHandlerNull()
        {
            Action action = () => new Route<int>("GET /", null);
            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("handler");
        }

        [Test]
        public void Constructor_Throws_WhenNoTemplate()
        {
            ApiHandler<int> handler = (id, _) => Task.FromResult(ApiResponse.Ok(id));
            Action action = () => new Route<int>("POST /names/32", handler);
            action.ShouldThrow<ArgumentException>()
                .Message.ShouldBe(SR.Format(SR.ApiHandlerTemplateNoParameters));
        }

        [Test]
        public void Constructor_Throws_WhenTemplateDoesNotMatchTupleParameters()
        {
            ApiHandler<(int, bool)> handler = (value, _) => Task.FromResult(ApiResponse.Ok(value));
            Action action = () => new Route<(int, bool)>("POST /names/{id}", handler);
            action.ShouldThrow<ArgumentException>()
                .Message.ShouldBe(SR.Format(SR.ApiHanderTemplateInvalid, "POST /names/{id}", "(Int32,Boolean)"));
        }

        [Test]
        public void Constructor_Throws_WhenTemplateDoesNotMatchPrimitiveParameters()
        {
            ApiHandler<int> handler = (value, _) => Task.FromResult(ApiResponse.Ok(value));
            Action action = () => new Route<int>("POST /names/{id}/{age}", handler);
            action.ShouldThrow<ArgumentException>()
                .Message.ShouldBe(SR.Format(SR.ApiHanderTemplateInvalid, "POST /names/{id}/{age}", "Int32"));
        }

        [Test]
        public void Constructor_SetsCorrectAction()
        {
            ApiHandler<int> handler = (id, _) => Task.FromResult(ApiResponse.Ok(id));
            var route = new Route<int>("POST /names/{id}", handler);
            route.Action.ShouldBe("POST /names/{id}");
        }

        [Test]
        public void Match_ReturnsFull_WhenTemplateCanBeUsedForPrimitiveAndNoQueryUsed()
        {
            ApiHandler<int> handler = (id, _) => Task.FromResult(ApiResponse.Ok(id));
            var route = new Route<int>("GET /names/{id}", handler);
            route.Match("GET /names/32").ShouldBe(RouteOrder.Action);
        }

        [TestCase("POST /names/32")]
        [TestCase("GET /names/32/x")]
        public void Match_ReturnsNone_WhenMethodOrPathNotSame(string action)
        {
            ApiHandler<int> handler = (id, _) => Task.FromResult(ApiResponse.Ok(id));
            var route = new Route<int>("GET /names/{id}", handler);
            route.Match(action).ShouldBe(RouteOrder.None);
        }

        [TestCase("GET /names/254.8?a=b&filter=abc")]
        [TestCase("GET /names/33?filter=abc&a=b")]
        public void Match_ReturnsAction_WhenTemplateCanBeUsedForPrimitiveAndQueryTheSame(string action)
        {
            ApiHandler<double> handler = (age, _) => Task.FromResult(ApiResponse.Ok(age));
            var route = new Route<double>("GET /names/{age}?filter=abc&a=b", handler);
            route.Match(action).ShouldBe(RouteOrder.Action);
        }

        [Test]
        public void Match_ReturnsNoQuery_WhenTemplateCanBeUsedForPrimitiveAndOnlyPathMatches()
        {
            ApiHandler<int> handler = (id, _) => Task.FromResult(ApiResponse.Ok(id));
            var route = new Route<int>("GET /names/{id}", handler);
            route.Match("GET /names/32?full=false").ShouldBe(RouteOrder.ActionNoQuery);
            route.Match("GET /names/true?full=false").ShouldBe(RouteOrder.RouteNoQuery);
        }

        [Test]
        public void Match_ReturnsAction_WhenTemplateCanBeUsedForTupleAndNoQueryUsed()
        {
            ApiHandler<(int Id, bool IsLong)> handler = (value, _) => Task.FromResult(ApiResponse.Ok(value.IsLong));
            var route = new Route<(int, bool)>("GET /names/{id}/type/{long}", handler);
            route.Match("GET /names/777/type/true").ShouldBe(RouteOrder.Action);
            route.Match("GET /names/1/type/false").ShouldBe(RouteOrder.Action);
            route.Match("GET /names/1/type/burda").ShouldBe(RouteOrder.Route);
        }

        [Test]
        public void Match_ReturnsFull_WhenTemplateWithDateUsed()
        {
            ApiHandler<(TestColor Color, DateTime Date, bool valid)> handler = (value, _) => Task.FromResult(ApiResponse.Ok(value.Date));
            var route = new Route<(TestColor, DateTime, bool)>("GET /names/{color}/type/{date}/{valid}", handler);
            route.Match("GET /names/blue/type/2012-01-01/True").ShouldBe(RouteOrder.Action);
            route.Match("GET /names/Green/type/2012-01-01T12:32:04/true").ShouldBe(RouteOrder.Action);
        }

        [Test]
        public void Match_DoesNotBreak_WhenRegexUsedInsideRoute()
        {
            ApiHandler<int> handler = (value, _) => Task.FromResult(ApiResponse.Ok(value));
            var route = new Route<int>("GET /{id}/([a-z]+)", handler);

            route.Match("GET /32/true").ShouldBe(RouteOrder.None);
        }

        [Test]
        public async Task Handler_ProvidesCorrectValuesForVariousTypes()
        {
            HttpContext context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Path = "/names/green/type/2012-01-01T12:32:04/true/-/15:32:08";
            ApiHandler<(TestColor Color, DateTime Date, bool Valid, TimeSpan Time)> handler = (value, _) => Task.FromResult(ApiResponse.Ok(value));
            var route = new Route<(TestColor, DateTime, bool, TimeSpan)>("GET /names/{color}/type/{date}/{valid}/-/{time}", handler);

            var response = await route.Handler(context);
            var body = response.Body as Body<(TestColor Color, DateTime Date, bool Valid, TimeSpan Time)>;

            body.Content.Time.ShouldBe(TimeSpan.Parse("15:32:08", CultureInfo.InvariantCulture));
            body.Content.Color.ShouldBe(TestColor.Green);
            body.Content.Date.ShouldBe(DateTime.Parse("2012-01-01T12:32:04", CultureInfo.InvariantCulture));
            body.Content.Valid.ShouldBe(true);
        }

        [Test]
        public async Task Handler_Returns404BadRequest_WhenCannotConvert()
        {
            HttpContext context = new DefaultHttpContext();
            context.Request.Method = "PUT";
            context.Request.Path = "/animals/SNAKE/color/Indigo/15.4";
            ApiHandler<(string Type, TestColor Color, DateTime Date)> handler = (value, _) => Task.FromResult(ApiResponse.Ok(value));
            var route = new Route<(string, TestColor, DateTime)>("PUT /animals/{type}/color/{color}/{date}", handler);

            var response = await route.Handler(context);

            response.StatusCode.ShouldBe(400);
            var error = ((Body<Error>)response.Body).Content;
            error.Title.ShouldBe("Failed to bind handler");
            error.Reasons.Count.ShouldBe(2);
            error.Reasons["{color}"].ShouldBe($"'Indigo' is not a valid value for {typeof(TestColor).Name}");
            error.Reasons["{date}"].ShouldBe($"'15.4' is not a valid value for {typeof(DateTime).Name}");
        }

        [Test]
        public async Task Handler_Returns404BadRequest_WhenIncorrectAmountOfGenericTypes()
        {
            HttpContext context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.Path = "/animals/32/fur";
            ApiHandler<(int Age, TestColor Color)> handler = (value, _) => Task.FromResult(ApiResponse.Ok(value));
            var route = new Route<(int, TestColor)>("POST /animals/{age}/fur/{fur}", handler);

            var response = await route.Handler(context);

            response.StatusCode.ShouldBe(400);
            var error = ((Body<Error>)response.Body).Content;
            error.Title.ShouldBe("Failed to bind handler");
            error.Reasons.Count.ShouldBe(0);
        }

        [Test]
        public async Task Handler_Returns404BadRequest_WhenCannotBind()
        {
            HttpContext context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Path = "/animalsies/528";
            ApiHandler<int> handler = (value, _) => Task.FromResult(ApiResponse.Ok(value));
            var route = new Route<int>("GET /animals/{age}", handler);

            var response = await route.Handler(context);

            response.StatusCode.ShouldBe(400);
            var error = ((Body<Error>)response.Body).Content;
            error.Title.ShouldBe("Failed to bind handler");
            error.Reasons.Count.ShouldBe(0);
        }

        public enum TestColor
        {
            Blue,
            Red,
            Green
        }
    }
}
