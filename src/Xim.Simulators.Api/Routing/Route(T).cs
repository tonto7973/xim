using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Xim.Simulators.Api.Routing
{
    internal class Route<T> : Route
    {
        private readonly string[] _attrs;
        private readonly string _localPathPattern;

        public Route(string actionTemplate, ApiHandler<T> handler, Route previous)
            : base(actionTemplate, route => ((Route<T>)route).ToBaseHandler(handler), previous)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _attrs = IsAttributeRx
                .Matches(Uri.LocalPath)
                .OfType<Match>()
                .Select(m => m.Value)
                .ToArray();

            if (_attrs.Length == 0)
                throw new ArgumentException(SR.Format(SR.ApiHandlerTemplateNoParameters));

            if (!IsBindableType(out var typeName))
                throw new ArgumentException(SR.Format(SR.ApiHanderTemplateInvalid, actionTemplate, typeName));

            _localPathPattern = MakeLocalPathPattern(Uri.LocalPath);
        }

        private bool IsBindableType(out string typeName)
        {
            var type = typeof(T);
            int expectedAttrsLength;
            if (type.FullName.StartsWith("System.ValueTuple`", StringComparison.InvariantCulture))
            {
                var genericTypes = type.GetGenericArguments();
                expectedAttrsLength = genericTypes.Length;
                typeName = "(" + string.Join(",", genericTypes.Select(t => t.Name)) + ")";
            }
            else
            {
                expectedAttrsLength = 1;
                typeName = type.Name;
            }

            return _attrs.Length == expectedAttrsLength;
        }

        private ApiHandler ToBaseHandler(ApiHandler<T> handler)
            => context => Map(context, out var args, out var errors)
                            ? handler(args, context)
                            : BadRequestHandler(errors);

        private static Task<ApiResponse> BadRequestHandler(Dictionary<string, string> errors)
            => Task.FromResult(new ApiResponse(400, body: ErrorBody(errors)));

        private static Body ErrorBody(Dictionary<string, string> errors)
            => Body.FromObject(new Error(errors) { Title = "Failed to bind handler" });

        private bool Map(HttpContext context, out T value, out Dictionary<string, string> errors)
        {
            value = default;
            errors = new Dictionary<string, string>();

            var matcher = new Regex("^" + Regex.Escape(Method) + " " + _localPathPattern + "$");
            var match = matcher.Match($"{context.Request.Method} {context.Request.Path}");

            return match.Success && IsBindableMatch(match, errors, out value);
        }

        protected override RouteOrder OnMatch(string action, string method, Uri uri)
        {
            var exactMatcher = new Regex("^" + Regex.Escape(Method) + " " + _localPathPattern + Regex.Escape(Uri.Query) + "$");
            var exactMatch = exactMatcher.Match(action);
            if (exactMatch.Success && IsBindableMatch(exactMatch, null, out _))
                return RouteOrder.Action;
            else if (exactMatch.Success)
                return RouteOrder.Route;

            var partialMatcher = new Regex("^" + Regex.Escape(Method) + " " + _localPathPattern + "$");
            var partialMatch = partialMatcher.Match($"{method} {uri.LocalPath}");
            if (partialMatch.Success && IsBindableMatch(partialMatch, null, out _) && string.IsNullOrEmpty(Uri.Query))
                return RouteOrder.ActionNoQuery;
            else if (partialMatch.Success)
                return RouteOrder.RouteNoQuery;

            return RouteOrder.None;
        }

        private bool IsBindableMatch(Match match, IDictionary<string, string> errors, out T value)
        {
            if (match.Groups.Count - 1 != _attrs.Length)
            {
                value = default;
                return false;
            }
            return IsBindablePrimitive(match, errors, out value) || IsBindableTuple(match, errors, out value);
        }

        private bool IsBindablePrimitive(Match match, IDictionary<string, string> errors, out T value)
        {
            Fragment PrimitiveFragment()
                => new Fragment
                {
                    Name = _attrs[0],
                    Text = match.Groups[1].Value,
                    Type = typeof(T)
                };

            object arg = null;
            var isBindable = _attrs.Length == 1
                && IsBindableFragment(PrimitiveFragment(), errors, out arg);
            value = isBindable
                ? (T)arg
                : default;
            return isBindable;
        }

        private bool IsBindableTuple(Match match, IDictionary<string, string> errors, out T value)
        {
            var type = typeof(T);
            if (type.FullName.StartsWith("System.ValueTuple`", StringComparison.InvariantCulture))
            {
                var genericTypes = type.GetGenericArguments();
                var args = new object[genericTypes.Length];
                var areAllGenericTypesBindable = (genericTypes.Length == _attrs.Length)
                    && match.Groups
                        .OfType<Group>()
                        .Skip(1)
                        .Select((group, index) => new Fragment { Name = _attrs[index], Text = group.Value, Type = genericTypes[index] })
                        .Select((fragment, index) => IsBindableFragment(fragment, errors, out args[index]))
                        .Aggregate(true, (previous, current) => previous && current);
                if (areAllGenericTypesBindable)
                {
                    value = (T)Activator.CreateInstance(typeof(T), args);
                    return true;
                }
            }
            value = default;
            return false;
        }

        private static string MakeLocalPathPattern(string path)
            => string.Join("([^" + Regex.Escape("/?#") + "]+)", IsAttributeRx.Split(path).Select(Regex.Escape));

        private static bool IsBindableFragment(Fragment fragment, IDictionary<string, string> errors, out object value)
        {
            var isBindable = TryBind(fragment.Type, fragment.Text, out value);
            if (!isBindable && errors != null)
                errors[fragment.Name] = $"'{fragment.Text}' is not a valid value for {fragment.Type.Name}";
            return isBindable;
        }

        private static bool TryBind(Type type, string text, out object value)
        {
            try
            {
                value = Bind(type, text);
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }

        private static object Bind(Type type, string text)
            => TypeDescriptor
                .GetConverter(type)
                .ConvertFromInvariantString(text);

        private class Fragment
        {
            public Type Type;
            public string Name;
            public string Text;
        }
    }
}
