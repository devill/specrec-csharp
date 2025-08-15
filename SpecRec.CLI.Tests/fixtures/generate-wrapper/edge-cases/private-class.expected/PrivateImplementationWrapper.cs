using System;

namespace FailureCases
{
    private class PrivateImplementationWrapper : IPrivateImplementation
    {
        private readonly PrivateImplementation _wrapped;

        public PrivateImplementationWrapper(PrivateImplementation wrapped)
        {
            _wrapped = wrapped;
        }

        public void DoSomething()
        {
            _wrapped.DoSomething();
        }

        public string GetValue()
        {
            return _wrapped.GetValue();
        }
    }
}