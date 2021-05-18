using System;
using System.Collections.Generic;

namespace Hosting.Extensions.Plugins
{
    /// <summary>
    /// Provides access to plugins.
    /// </summary>
    public interface IPlugins
    {
        /// <summary>
        /// Resolves all implementations of <typeparamref name="TPluginContract"/>
        /// within all the loaded plugins.
        /// </summary>
        /// <typeparam name="TPluginContract">The plugin contract type.</typeparam>
        /// <returns>A collection of <typeparamref name="TPluginContract"/> instances.</returns>
        IReadOnlyCollection<TPluginContract> GetImplementationsOf<TPluginContract>();

        /// <summary>
        /// Gets the <see cref="Type"/> of all implementations of
        /// <typeparamref name="TPluginContract"/> within all the loaded
        /// plugins.
        /// </summary>
        /// <typeparam name="TPluginContract">The plugin contract type.</typeparam>
        /// <returns>
        /// A collection of <see cref="Type"/>s implementing <typeparamref name="TPluginContract"/>.
        /// </returns>
        IReadOnlyCollection<Type> GetTypesImplementing<TPluginContract>();

        /// <summary>
        /// Resolves the specified implementation of <typeparamref name="TPluginContract"/>.
        /// </summary>
        /// <param name="typeAssemblyQualifiedName">
        /// The <see cref="Type.AssemblyQualifiedName"/> of the implementation to resolve.
        /// </param>
        /// <typeparam name="TPluginContract">The plugin contract type.</typeparam>
        /// <returns>An instance of <typeparamref name="TPluginContract"/>.</returns>
        TPluginContract GetImplementation<TPluginContract>(string typeAssemblyQualifiedName);
    }

    /// <summary>
    /// Provides access to implementations of the specified plugin contract.
    /// </summary>
    /// <typeparam name="TPluginContract">The plugin contract type.</typeparam>
    public interface IPlugins<TPluginContract>
    {
        /// <summary>
        /// Resolves all implementations of <typeparamref name="TPluginContract"/>
        /// within all the loaded plugins.
        /// </summary>
        IReadOnlyCollection<TPluginContract> Plugins { get; }

        /// <summary>
        /// Gets the <see cref="Type"/> of all implementations of
        /// <typeparamref name="TPluginContract"/> within all the loaded
        /// plugins.
        /// </summary>
        IReadOnlyCollection<Type> Types { get; }
    }
}