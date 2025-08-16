using System;

namespace TestProject
{
    public interface IDatabaseServiceWrapper
    {
        void Connect(string connectionString);
    }
}