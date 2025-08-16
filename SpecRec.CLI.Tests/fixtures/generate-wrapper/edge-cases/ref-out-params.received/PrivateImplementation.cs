using System;

namespace FailureCases
{
    public class ContainerClass
    {
        private class PrivateImplementation
        {
            public void DoSomething()
            {
                Console.WriteLine("Private class method");
            }

            public string GetValue()
            {
                return "private value";
            }
        }

        public void UsePrivateClass()
        {
            var impl = new PrivateImplementation();
            impl.DoSomething();
        }
    }
}