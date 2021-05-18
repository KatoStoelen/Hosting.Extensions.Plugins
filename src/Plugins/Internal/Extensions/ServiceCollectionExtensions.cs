using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Hosting.Extensions.Plugins.Internal.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTypesFromAssembly(
            this IServiceCollection services, Assembly assembly)
        {
            var classes = assembly
                .GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract);

            foreach (var @class in classes)
            {
                _ = services
                    .AddTransient(@class)
                    .RegisterAsBaseTypes(@class)
                    .RegisterAsImplementedInterfaces(@class);
            }

            return services;
        }

        private static IServiceCollection RegisterAsBaseTypes(
            this IServiceCollection services, Type implementationType)
        {
            var baseType = implementationType.BaseType;

            while (baseType != null && baseType != typeof(object))
            {
                _ = services.AddTransient(baseType, implementationType);

                baseType = baseType.BaseType;
            }

            return services;
        }

        private static IServiceCollection RegisterAsImplementedInterfaces(
            this IServiceCollection services, Type implementationType)
        {
            foreach (var @interface in implementationType.GetInterfaces())
            {
                _ = services.AddTransient(@interface, implementationType);
            }

            return services;
        }
    }
}