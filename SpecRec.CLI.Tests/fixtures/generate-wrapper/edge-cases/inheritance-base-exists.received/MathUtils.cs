using System;

namespace FailureCases
{
    public static class MathUtils
    {
        public static int Add(int a, int b)
        {
            return a + b;
        }

        public static double Multiply(double a, double b)
        {
            return a * b;
        }

        public static T Max<T>(T a, T b) where T : IComparable<T>
        {
            return a.CompareTo(b) >= 0 ? a : b;
        }

        public static bool IsEven(int number)
        {
            return number % 2 == 0;
        }
    }
}