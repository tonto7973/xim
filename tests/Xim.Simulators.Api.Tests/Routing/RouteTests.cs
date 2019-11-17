using System;
using NUnit.Framework;
using Shouldly;

#pragma warning disable RCS1210 // Return Task.FromResult instead of returning null.

namespace Xim.Simulators.Api.Routing.Tests
{
    [TestFixture]
    public class RouteTests
    {
        [Test]
        public void Constructor_Throws_WhenActionNull()
        {
            Action action = () => new Route(null, _ => null);
            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("action");
        }

        [Test]
        public void Constructor_Throws_WhenHandlerNull()
        {
            Action action = () => new Route("GET /", null);
            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("handler");
        }

        [Test]
        public void Constructor_SetsActionAndHandler()
        {
            ApiHandler handler = _ => null;
            var route = new Route("PUT /names/32", handler);
            route.Action.ShouldBe("PUT /names/32");
            route.Handler.ShouldBeSameAs(handler);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("PIP")]
        [TestCase("POP ")]
        [TestCase("POST./3287")]
        public void Match_ReturnsNone_WhenActionInvalid(string action)
        {
            var route = new Route("PUT /names/32", _ => null);
            route.Match(action).ShouldBe(RouteOrder.None);
        }

        [TestCase("GET /index.htm")]
        [TestCase("POST /names")]
        [TestCase("PATCH api/v1//items/32")]
        public void Match_ReturnsFull_WhenActionExactlyTheSame(string action)
        {
            var route = new Route(action, _ => null);
            route.Match(action).ShouldBe(RouteOrder.Action);
        }

        [TestCase("DELETE /items/857", "?full=true")]
        [TestCase("OPTION /items/857", "?foo=bar&baz=")]
        public void Match_ReturnsFull_WhenActionAndQueryExactlyTheSame(string action, string query)
        {
            var route = new Route(action + query, _ => null);
            route.Match(action + query).ShouldBe(RouteOrder.Action);
        }

        [TestCase("?colour=red&zoo=full&animal=ant")]
        [TestCase("?animal=ant&zoo=full&colour=red")]
        [TestCase("?animal=ant&colour=red&zoo=full")]
        public void Match_ReturnsFull_WhenActionExactlyTheSameAndQuerySameButInDifferentOrder(string query)
        {
            var route = new Route("ANY /load?zoo=full&animal=ant&colour=red", _ => null);
            route.Match("ANY /load" + query).ShouldBe(RouteOrder.Action);
        }

        [TestCase("GET /data.html", "?age=32")]
        [TestCase("PUT /values/32", "?count=4&quality=5.86")]
        public void Match_ReturnsPartial_WhenOnlyPathTheSame(string action, string query)
        {
            var route = new Route(action, _ => null);
            route.Match(action + query).ShouldBe(RouteOrder.ActionNoQuery);
        }

        [Test]
        public void Match_ReturnsNone_WhenPathNotTheSame()
        {
            var route = new Route("GET /data", _ => null);
            route.Match("PUT /data").ShouldBe(RouteOrder.None);
            route.Match("GET /data/a").ShouldBe(RouteOrder.None);
        }

        [Test]
        public void Match_ReturnsNone_WhenQueryNotTheSame()
        {
            var route = new Route("GET /data?id=32", _ => null);
            route.Match("GET /data").ShouldBe(RouteOrder.None);
            route.Match("GET /data?id=33").ShouldBe(RouteOrder.None);
            route.Match("GET /data?id=32&spin=up").ShouldBe(RouteOrder.None);
        }
    }
}
