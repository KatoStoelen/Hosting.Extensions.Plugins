using System;
using Microsoft.Extensions.DependencyInjection;

namespace Hosting.Extensions.Plugins.Internal.Extensions
{
    internal static class ServiceDescriptorExtensions
    {
        public static Type? GetImplementationType(this ServiceDescriptor serviceDescriptor)
        {
            if (serviceDescriptor.ImplementationType != null)
            {
                return serviceDescriptor.ImplementationType;
            }

            if (serviceDescriptor.ImplementationInstance != null)
            {
                return serviceDescriptor.ImplementationInstance.GetType();
            }

            if (serviceDescriptor.ImplementationFactory != null)
            {
                var genericTypeArguments = serviceDescriptor.ImplementationFactory
                    .GetType()
                    .GenericTypeArguments;

                return genericTypeArguments[1];
            }

            return null;
        }
    }
}