using System;

namespace EdgeCases
{
    public interface IMathUtilsStaticWrapper
    {
        int Add(int a, int b);
        double Multiply(double a, double b);
        T Max<T>(T a, T b) where T : IComparable<T>;
        bool IsEven(int number);
    }
}