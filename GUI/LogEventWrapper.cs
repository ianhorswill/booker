using Serilog.Events;

namespace GUI
{
    public class LogEventWrapper
    {
        private readonly LogEvent _event;

        public LogEventWrapper(LogEvent @event) {
            _event = @event;
        }

        public string Message => _event.RenderMessage();

        public Color Color => _event.Level switch {
            LogEventLevel.Error => Colors.Orange,
            LogEventLevel.Fatal => Colors.Red,
            LogEventLevel.Warning => Colors.Yellow,
            _ => Colors.Gray
        };
    }
}
