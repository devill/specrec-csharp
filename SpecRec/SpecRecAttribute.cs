using System.Reflection;
using Xunit;
using Xunit.Sdk;

namespace SpecRec
{
    /// <summary>
    /// Unified SpecRec test attribute that provides a single, consistent API for all SpecRec operations.
    /// Supports auto-parrot substitution, object registration, CallLogger wrapping, and comprehensive test case discovery.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [DataDiscoverer("SpecRec.SpecRecDiscoverer", "SpecRec")]
    public class SpecRecAttribute : DataAttribute
    {
        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            // This method is called by xUnit for data discovery
            // We'll implement the logic in the DataDiscoverer for better control
            return new List<object[]>();
        }
    }
}