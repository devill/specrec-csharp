using System;

namespace EdgeCases
{
    // Private wrapper generated alongside the original private class
    private class PrivateImplementationWrapper : IPrivateImplementation
    {
        private readonly ContainerClass.PrivateImplementation _wrapped;

        internal PrivateImplementationWrapper(ContainerClass.PrivateImplementation wrapped)
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