namespace SpecRec
{
    public static class SpecRecContext
    {
        private static readonly ThreadLocal<string?> _currentTestCase = new();

        public static string? CurrentTestCase
        {
            get => _currentTestCase.Value;
            internal set => _currentTestCase.Value = value;
        }

        public static void SetTestCase(string testCaseName)
        {
            _currentTestCase.Value = testCaseName;
        }

        public static void ClearTestCase()
        {
            _currentTestCase.Value = null;
        }
    }
}