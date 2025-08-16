using System;

namespace EdgeCases
{
    public interface IMixedMethodsServiceWrapper
    {
        bool IsInitialized { get; }

        void SetData(string data);
        string GetData();
        string ProcessData(string input);
        string ProcessWithFormatting(string input);
    }
}