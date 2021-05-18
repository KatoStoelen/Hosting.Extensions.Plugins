using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Hosting.Extensions.Plugins.Internal
{
    internal class PluginHostEnvironment : IHostEnvironment
    {
        public PluginHostEnvironment(
            string environmentName,
            string applicationName,
            string contentRootPath,
            IFileProvider contentRootFileProvider)
        {
            EnvironmentName = environmentName;
            ApplicationName = applicationName;
            ContentRootPath = contentRootPath;
            ContentRootFileProvider = contentRootFileProvider;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; }
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
    }
}