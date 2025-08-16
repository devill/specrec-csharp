using System;

namespace TestProject
{
    public interface IDatabaseService
    {
        void Connect(string connectionString);
    }
}