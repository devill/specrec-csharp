using System;

namespace EdgeCases
{
    public interface IMixedMethodsServiceWrapperStaticWrapper
    {
        string Version { get; }

        string FormatValue(string value);
        int Calculate(int a, int b);
        T CreateDefault<T>()
            where T : new();
    }
}