using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using CommunityToolkit.Maui.Core;

namespace GUI;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
            .UseMauiCommunityToolkitCore()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Sink(new LogObserver(), LogEventLevel.Verbose)
            .CreateLogger();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

    private class LogObserver : ILogEventSink
    {
        public void Emit (LogEvent e) => MainPage.LogMessage(e);
    }
}
