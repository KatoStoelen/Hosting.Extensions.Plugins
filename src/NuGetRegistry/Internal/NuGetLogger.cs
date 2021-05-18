using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Hosting.Extensions.Plugins.NuGetRegistry.Internal
{
    internal class NuGetLogger : NuGet.Common.ILogger
    {
        private readonly ILogger<NuGetLogger> _logger;

        public NuGetLogger(ILogger<NuGetLogger> logger)
        {
            _logger = logger;
        }

        public void Log(NuGet.Common.LogLevel level, string data)
        {
            _logger.LogTrace("{Level}: {Data}", level, data);
        }

        public void Log(NuGet.Common.ILogMessage message)
        {
            Log(message.Level, $"({message.Code}) {message.Message}");
        }

        public Task LogAsync(NuGet.Common.LogLevel level, string data)
        {
            Log(level, data);

            return Task.CompletedTask;
        }

        public Task LogAsync(NuGet.Common.ILogMessage message)
        {
            Log(message);

            return Task.CompletedTask;
        }

        public void LogDebug(string data)
        {
            Log(NuGet.Common.LogLevel.Debug, data);
        }

        public void LogError(string data)
        {
            Log(NuGet.Common.LogLevel.Error, data);
        }

        public void LogInformation(string data)
        {
            Log(NuGet.Common.LogLevel.Information, data);
        }

        public void LogInformationSummary(string data)
        {
            Log(NuGet.Common.LogLevel.Information, data);
        }

        public void LogMinimal(string data)
        {
            Log(NuGet.Common.LogLevel.Minimal, data);
        }

        public void LogVerbose(string data)
        {
            Log(NuGet.Common.LogLevel.Verbose, data);
        }

        public void LogWarning(string data)
        {
            Log(NuGet.Common.LogLevel.Warning, data);
        }
    }
}