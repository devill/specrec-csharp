namespace SpecRec
{
    public class CallLoggingContext
    {
        public readonly Dictionary<string, HashSet<int>> _ignoredArguments = new();
        public readonly HashSet<string> _ignoredCalls = new();
        public readonly HashSet<string> _ignoredAllArguments = new();
        public readonly HashSet<string> _ignoredReturnValues = new();

        public bool ShouldIgnoreCall(string methodName)
        {
            return _ignoredCalls.Contains(methodName);
        }

        public bool ShouldIgnoreArgument(string methodName, int argumentIndex)
        {
            return _ignoredArguments.ContainsKey(methodName) &&
                   _ignoredArguments[methodName].Contains(argumentIndex);
        }

        public bool ShouldIgnoreAllArguments(string methodName)
        {
            return _ignoredAllArguments.Contains(methodName);
        }

        public bool ShouldIgnoreReturnValue(string methodName)
        {
            return _ignoredReturnValues.Contains(methodName);
        }

        public void Clear()
        {
            _ignoredArguments.Clear();
            _ignoredCalls.Clear();
            _ignoredAllArguments.Clear();
            _ignoredReturnValues.Clear();
        }
    }
}