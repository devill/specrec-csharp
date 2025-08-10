using System;

namespace EdgeCases
{
    public interface IMixedMethodsService
    {
        // Only instance members should be in the interface - static members are excluded
        bool IsInitialized { get; }
        void SetData(string data);
        string GetData();
        string ProcessData(string input);
        string ProcessWithFormatting(string input);
    }
}