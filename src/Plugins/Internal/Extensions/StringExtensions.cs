using System;

namespace Hosting.Extensions.Plugins.Internal.Extensions
{
    internal static class StringExtensions
    {
        public static (string TypeName, string AssemblyName) SplitTypeAndAssemblyName(
            this string typeAssemblyQualifiedName)
        {
            var firstCommaIndex = typeAssemblyQualifiedName.IndexOf(',');

            if (firstCommaIndex == -1)
            {
                throw new ArgumentException(
                    $"'{typeAssemblyQualifiedName}' is not a assembly qualified type name");
            }

            return (
                typeAssemblyQualifiedName[..firstCommaIndex],
                typeAssemblyQualifiedName[(firstCommaIndex + 1)..].Trim()
            );
        }
    }
}