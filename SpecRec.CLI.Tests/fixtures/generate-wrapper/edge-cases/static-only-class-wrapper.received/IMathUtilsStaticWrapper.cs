using System;

namespace FailureCases
{
    public interface IMathUtilsWrapperStaticWrapper
    {
        int Add(int a, int b);
        double Multiply(double a, double b);
        T Max<T>(T a, T b)
            where T : IComparable<T>;
        bool IsEven(int number);
    }
}