using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Hosting.Extensions.Plugins.Internal.Extensions
{
    internal static class ServiceProviderExtensions
    {
        public static object[] ResolveParameters(
                this IServiceProvider serviceProvider,
                MethodInfo methodInfo) =>
            serviceProvider.ResolveParameters(methodInfo, Array.Empty<object>());

        public static object[] ResolveParameters(
            this IServiceProvider serviceProvider,
            MethodInfo methodInfo,
            IReadOnlyCollection<object> additionalResolvables)
        {
            return methodInfo
                .GetParameters()
                .Select(parameter =>
                {
                    var instance = serviceProvider.GetService(parameter.ParameterType) ??
                                   additionalResolvables.SingleOrDefault(
                                       resolvable =>
                                            resolvable
                                                .GetType()
                                                .IsAssignableTo(parameter.ParameterType));

                    if (instance == null)
                    {
                        throw new InvalidOperationException(
                            $"Failed to resolve parameter '{parameter.Name}' of " +
                            $"method {methodInfo.DeclaringType!.FullName}.{methodInfo.Name}");
                    }

                    return instance!;
                })
                .ToArray();
        }
    }
}