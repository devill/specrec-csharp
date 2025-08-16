using System;

namespace EdgeCases
{
    public class CalculatorService
    {
        public int Add(int a, int b)
        {
            return a + b;
        }

        public double Add(double a, double b)
        {
            return a + b;
        }

        public int Add(int a, int b, int c)
        {
            return a + b + c;
        }

        public decimal Add(decimal a, decimal b)
        {
            return a + b;
        }

        public T Add<T>(T a, T b) where T : struct
        {
            dynamic da = a;
            dynamic db = b;
            return da + db;
        }

        public int Multiply(int a, int b = 1)
        {
            return a * b;
        }

        public double Multiply(double a, double b = 1.0)
        {
            return a * b;
        }

        public void Calculate(string operation, params int[] values)
        {
            Console.WriteLine($"Calculating {operation} with {values.Length} values");
        }
    }
}