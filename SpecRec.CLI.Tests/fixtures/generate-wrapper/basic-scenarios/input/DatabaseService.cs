using System;

namespace TestProject
{
    public class DatabaseService
    {
        public void Connect(string connectionString)
        {
            Console.WriteLine($"Connecting to: {connectionString}");
        }
    }
}