using System.Runtime.CompilerServices;

namespace SpecRec
{
    public class CallLogFormatter
    {
        private readonly CallLog _callLog;
        private readonly string? _forcedInterfaceName;

        public CallLogFormatter(CallLog callLog, string? forcedInterfaceName = null)
        {
            _callLog = callLog;
            _forcedInterfaceName = forcedInterfaceName;
        }

        public void LogConstructorCall(string emoji, List<(string name, object? value, string emoji)> parameters)
        {
            var interfaceName = _forcedInterfaceName ?? GetInterfaceName();
            _callLog.AppendLine($"{emoji} {interfaceName} constructor called with:");

            foreach (var (name, value, paramEmoji) in parameters)
            {
                _callLog.AppendLine($"  {paramEmoji} {name}: {value}");
            }

            _callLog.AppendLine();
        }

        public void LogMethodCall(string emoji, string methodName, List<(string name, object? value, string emoji)> parameters, object? returnValue, string? note)
        {
            _callLog.AppendLine($"{emoji} {methodName}:");

            foreach (var (name, value, paramEmoji) in parameters)
            {
                var formattedValue = _callLog.FormatValue(value);
                _callLog.AppendLine($"  {paramEmoji} {name}: {formattedValue}");
            }

            if (!string.IsNullOrEmpty(note))
            {
                _callLog.AppendLine($"  🗒️ {note}");
            }

            if (returnValue != null)
            {
                var formattedReturn = _callLog.FormatValue(returnValue);
                _callLog.AppendLine($"  🔹 Returns: {formattedReturn}");
            }

            _callLog.AppendLine();
        }

        private string GetInterfaceName()
        {
            var frame = new System.Diagnostics.StackFrame(3, false);
            var method = frame.GetMethod();
            var declaringType = method?.DeclaringType;

            if (declaringType != null)
            {
                var interfaces = declaringType.GetInterfaces();
                var mainInterface =
                    interfaces.FirstOrDefault(i => i.Name.StartsWith("I") && i != typeof(IConstructorCalledWith));
                return mainInterface?.Name ?? declaringType.Name;
            }

            return "Unknown";
        }
    }
}