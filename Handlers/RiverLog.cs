using Serilog.Core;
using Serilog.Events;
using System.Text;

namespace Mari_Module.Handlers
{
    public class RiverLog : ILogEventSink
    {
        private StringBuilder _stringBuilder = new StringBuilder();
        private TextBox? _textBox = null;

        public void Emit(LogEvent logEvent)
        {
            if (_stringBuilder != null)
            {
                _stringBuilder.AppendLine(logEvent.RenderMessage());

                Form1.TextWrite(logEvent.RenderMessage());
            }
        }

        public void setTextBox(TextBox textBox)
        {
            _textBox = textBox;
        }

        public string Export()
        {
            if (_stringBuilder != null)
            {
                return _stringBuilder.ToString();
            }

            return "";
        }
    }
}
