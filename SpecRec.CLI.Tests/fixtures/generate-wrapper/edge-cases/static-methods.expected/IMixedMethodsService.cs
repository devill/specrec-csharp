using System;

namespace EdgeCases
{
    public interface IMixedMethodsService
    {
        bool IsInitialized { get; }

        void SetData(string data);
        string GetData();
        string ProcessData(string input);
        string ProcessWithFormatting(string input);
    }
}