using System;

namespace FailureCases
{
    private interface IPrivateImplementation
    {
        void DoSomething();
        string GetValue();
    }
}