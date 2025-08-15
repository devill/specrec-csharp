using System;

namespace EdgeCases
{
    public interface IMixedMethodsServiceStaticWrapper
    {
        string Version { get; }

        string FormatValue(string value);
        int Calculate(int a, int b);
        T CreateDefault<T>()
            where T : new();
    }
}