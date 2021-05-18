using System;
using System.Linq;
using System.Reflection;

namespace Hosting.Extensions.Plugins.Internal.Extensions
{
    internal static class ServiceProviderExtensions
    {
        public static object[] ResolveParameters(
            this IServiceProvider serviceProvider,
            MethodInfo methodInfo)
        {
            return methodInfo
                .GetParameters()
                .Select(parameter =>
                {
                    var instance = serviceProvider.GetService(parameter.ParameterType);

                    if (instance == null)
                    {
                        throw new InvalidOperationException(
                            $"Failed to resolve parameter '{parameter.Name}' of " +
                            $"method {methodInfo.DeclaringType!.FullName}.{methodInfo.Name}");
                    }

                    return instance;
                })
                .ToArray();
        }
    }
}