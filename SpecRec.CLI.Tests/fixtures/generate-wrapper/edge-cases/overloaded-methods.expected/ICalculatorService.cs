using System;

namespace EdgeCases
{
    public interface ICalculatorService
    {
        int Add(int a, int b);
        double Add(double a, double b);
        int Add(int a, int b, int c);
        decimal Add(decimal a, decimal b);
        T Add<T>(T a, T b)
            where T : struct;
        int Multiply(int a, int b = 1);
        double Multiply(double a, double b = 1.0);
        void Calculate(string operation, params int[] values);
    }
}