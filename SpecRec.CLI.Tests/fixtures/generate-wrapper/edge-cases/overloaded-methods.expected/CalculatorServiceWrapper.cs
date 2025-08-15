using System;

namespace EdgeCases
{
    public class CalculatorServiceWrapper : ICalculatorService
    {
        private readonly CalculatorService _wrapped;

        public CalculatorServiceWrapper(CalculatorService wrapped)
        {
            _wrapped = wrapped;
        }

        public int Add(int a, int b)
        {
            return _wrapped.Add(a, b);
        }

        public double Add(double a, double b)
        {
            return _wrapped.Add(a, b);
        }

        public int Add(int a, int b, int c)
        {
            return _wrapped.Add(a, b, c);
        }

        public decimal Add(decimal a, decimal b)
        {
            return _wrapped.Add(a, b);
        }

        public T Add<T>(T a, T b)
            where T : struct
        {
            return _wrapped.Add(a, b);
        }

        public int Multiply(int a, int b = 1)
        {
            return _wrapped.Multiply(a, b);
        }

        public double Multiply(double a, double b = 1.0)
        {
            return _wrapped.Multiply(a, b);
        }

        public void Calculate(string operation, params int[] values)
        {
            _wrapped.Calculate(operation, values);
        }
    }
}