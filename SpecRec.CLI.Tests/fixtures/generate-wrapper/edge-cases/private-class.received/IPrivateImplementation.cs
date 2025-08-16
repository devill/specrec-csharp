using System;

namespace FailureCases
{
    private interface IPrivateImplementationWrapper
    {
        void DoSomething();
        string GetValue();
    }
}