using System;

namespace EdgeCases
{
    public interface IMixedMethodsServiceStaticWrapper
    {
        // Static methods wrapped as instance methods
        string FormatValue(string value);
        int Calculate(int a, int b);
        T CreateDefault<T>() where T : new();
        string Version { get; }
    }
}