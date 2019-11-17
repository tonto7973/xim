using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Xim.Simulators.ServiceBus.Tests
{
    internal static class Construct
    {
        public static T Uninitialized<T>()
        {
            var type = typeof(T);
            return (T)FormatterServices.GetUninitializedObject(type);
        }

        public static T InitializePrivateProperty<T>(this T instance, string name)
        {
            var type = typeof(T);
            var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null)
                throw new NotSupportedException($"Property {type.Name}.{name} not available.");
            var setter = property.GetSetMethod() ?? property.GetSetMethod(true);
            if (setter == null)
                throw new NotSupportedException($"Property {type.Name}.{name} does not support setter.");
            var value = FormatterServices.GetUninitializedObject(property.PropertyType);
            setter.Invoke(instance, new object[] { value });
            return instance;
        }

        internal static T ForPrivate<T>(params object[] args)
        {
            if (args == null)
                args = Array.Empty<object>();

            var type = typeof(T);
            var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var constructor = Array.Find(constructors, c => IsMatch(c.GetParameters(), args));
            if (constructor == null)
                throw new NotSupportedException($"Constructor {type.Name}(...) not found.");
            return (T)constructor.Invoke(args);
        }

        private static bool IsMatch(ParameterInfo[] parameters, object[] args)
            => parameters.Length == args.Length
                && parameters
                    .Select((parameter, index) => parameter.ParameterType.IsInstanceOfType(args[index]))
                    .All(r => r);
    }
}
