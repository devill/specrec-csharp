using System;

namespace FailureCases
{
    public class MathUtilsStaticWrapper : IMathUtilsStaticWrapper
    {
        public int Add(int a, int b)
        {
            return MathUtils.Add(a, b);
        }

        public double Multiply(double a, double b)
        {
            return MathUtils.Multiply(a, b);
        }

        public T Max<T>(T a, T b)
            where T : IComparable<T>
        {
            return MathUtils.Max(a, b);
        }

        public bool IsEven(int number)
        {
            return MathUtils.IsEven(number);
        }
    }
}