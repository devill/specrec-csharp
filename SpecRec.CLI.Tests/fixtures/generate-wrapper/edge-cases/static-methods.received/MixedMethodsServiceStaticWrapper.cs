using System;

namespace EdgeCases
{
    public class MixedMethodsServiceStaticWrapper : IMixedMethodsServiceWrapperStaticWrapper
    {
        public string Version => MixedMethodsService.Version;

        public string FormatValue(string value)
        {
            return MixedMethodsService.FormatValue(value);
        }

        public int Calculate(int a, int b)
        {
            return MixedMethodsService.Calculate(a, b);
        }

        public T CreateDefault<T>()
            where T : new()
        {
            return MixedMethodsService.CreateDefault();
        }
    }
}