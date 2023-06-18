using NWLottoSystem.Library;
using Serilog;
using Serilog.Formatting.Json;

namespace NWLottoSystem.Utils
{
    public static class Factory
    {
        public static ILogger GetLogger()
        {
            LoggerConfiguration configuration = new LoggerConfiguration()
                .WriteTo.File(new JsonFormatter(renderMessage: true), "logs.json", fileSizeLimitBytes: 16777216, rollOnFileSizeLimit: true)
                .WriteTo.Console(theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
                .MinimumLevel.Is(Serilog.Events.LogEventLevel.Debug);
            return configuration.CreateLogger();
        }

        public static DatabaseContext GetDBContext(ILogger logger, string connectionString)
        {
            return new DatabaseContext(logger, connectionString);
        }
    }
}
