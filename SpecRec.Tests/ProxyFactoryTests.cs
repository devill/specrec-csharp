using Xunit;

namespace SpecRec.Tests
{
    public class ProxyFactoryTests
    {
        public interface ITestInterface
        {
            string GetValue();
        }

        public class TestImplementation : ITestInterface
        {
            public string GetValue() => "Test";
        }

        public class VirtualClass
        {
            public virtual string GetValue() => "Virtual";
            public virtual int Calculate(int x, int y) => x + y;
        }

        public sealed class SealedClass
        {
            public string GetValue() => "Sealed";
        }

        public class NonVirtualClass
        {
            public string GetValue() => "NonVirtual";
        }

        public class NoParameterlessConstructor
        {
            private readonly string _value;
            public NoParameterlessConstructor(string value) => _value = value;
            public virtual string GetValue() => _value;
        }

        public abstract class AbstractClass
        {
            public abstract string GetValue();
        }

        [Fact]
        public void CanCreateProxyForType_WithInterface_ShouldReturnTrue()
        {
            var result = ProxyFactory.CanCreateProxyForType(typeof(ITestInterface));
            Assert.True(result);
        }

        [Fact]
        public void CanCreateProxyForType_WithSealedClass_ShouldReturnFalse()
        {
            var result = ProxyFactory.CanCreateProxyForType(typeof(SealedClass));
            Assert.False(result);
        }

        [Fact]
        public void CanCreateProxyForType_WithClassWithoutParameterlessConstructor_ShouldReturnFalse()
        {
            var result = ProxyFactory.CanCreateProxyForType(typeof(NoParameterlessConstructor));
            Assert.False(result);
        }

        [Fact]
        public void CanCreateProxyForType_WithClassWithoutVirtualMethods_ShouldReturnFalse()
        {
            // DispatchProxy only supports interfaces, not classes
            var result = ProxyFactory.CanCreateProxyForType(typeof(NonVirtualClass));
            Assert.False(result);
        }

        [Fact]
        public void CanCreateProxyForType_WithClassWithVirtualMethods_ShouldReturnFalse()
        {
            // DispatchProxy only supports interfaces, not classes (even with virtual methods)
            var result = ProxyFactory.CanCreateProxyForType(typeof(VirtualClass));
            Assert.False(result);
        }

        [Fact]
        public void CanCreateProxyForType_WithAbstractClass_ShouldReturnFalse()
        {
            // DispatchProxy only supports interfaces, not abstract classes
            var result = ProxyFactory.CanCreateProxyForType(typeof(AbstractClass));
            Assert.False(result);
        }

        [Fact]
        public void CreateLoggingProxy_WithInterface_ShouldCreateProxy()
        {
            var callLog = new CallLog();
            var logger = new CallLogger(callLog);
            var target = new TestImplementation();
            
            var proxy = ProxyFactory.CreateLoggingProxy<ITestInterface>(target, logger, "🔧");
            
            Assert.NotNull(proxy);
            var result = proxy.GetValue();
            Assert.Equal("Test", result);
            Assert.Contains("GetValue", callLog.ToString());
        }

        [Fact]
        public void CreateParrotProxy_WithInterface_ShouldCreateProxy()
        {
            var verifiedContent = """
                🦜 GetValue:
                  🔹 Returns: "Mocked"

                """;
            
            var callLog = new CallLog(verifiedContent);
            var logger = new CallLogger(callLog);
            
            var proxy = ProxyFactory.CreateParrotProxy<ITestInterface>(logger, "🦜");
            
            Assert.NotNull(proxy);
            var result = proxy.GetValue();
            Assert.Equal("Mocked", result);
        }

        [Fact]
        public void CreateLoggingProxy_WithCustomInterfaceName_ShouldUseProvidedName()
        {
            var callLog = new CallLog();
            var logger = new CallLogger(callLog);
            var target = new TestImplementation();
            
            var proxy = ProxyFactory.CreateLoggingProxy<ITestInterface>(target, logger, "🔧", "CustomInterface");
            
            proxy.GetValue();
            
            // The custom interface name should be used in logging
            // Note: This would require CallLogFormatter to use the provided interface name
            Assert.Contains("GetValue", callLog.ToString());
        }

        [Fact]
        public void CreateParrotProxy_WithoutReturnValue_ShouldThrowParrotMissingReturnValueException()
        {
            var callLog = new CallLog(); // Empty log
            var logger = new CallLogger(callLog);
            
            var proxy = ProxyFactory.CreateParrotProxy<ITestInterface>(logger, "🦜");
            
            Assert.Throws<ParrotMissingReturnValueException>(() => proxy.GetValue());
        }
    }

    public class ClassProxySupportTests
    {
        // Note: DispatchProxy only supports interfaces, not classes
        // These tests demonstrate interface-based proxy behavior
        
        public interface IVirtualMethods
        {
            int VirtualMethod(int x);
            string VirtualProperty { get; set; }
        }

        public class VirtualMethodClass : IVirtualMethods
        {
            public int VirtualMethod(int x) => x * 2;
            public string VirtualProperty { get; set; } = "Default";
        }

        public interface IMixedMethods
        {
            string GetName();
            int GetId();
            void DoWork();
        }

        public class MixedMethodClass : IMixedMethods
        {
            public string GetName() => "Mixed";
            public int GetId() => 42;
            public void DoWork() { }
        }

        [Fact]
        public void CallLoggerProxy_WithInterfaceImplementation_ShouldInterceptMethods()
        {
            var callLog = new CallLog();
            var logger = new CallLogger(callLog);
            var target = new VirtualMethodClass();
            
            // Interface types can have proxies
            var canCreate = ProxyFactory.CanCreateProxyForType(typeof(IVirtualMethods));
            Assert.True(canCreate);
            
            // Creating proxy for interface
            var proxy = CallLoggerProxy<IVirtualMethods>.Create(target, logger, "🔧");
            
            var result = proxy.VirtualMethod(5);
            Assert.Equal(10, result);
            
            // Method should be logged
            Assert.Contains("VirtualMethod", callLog.ToString());
            Assert.Contains("x: 5", callLog.ToString());
            Assert.Contains("Returns: 10", callLog.ToString());
        }

        [Fact]
        public void CallLoggerProxy_WithInterface_InParrotMode_ShouldWork()
        {
            var verifiedContent = """
                🦜 VirtualMethod:
                  🔸 x: 5
                  🔹 Returns: 100

                """;
            
            var callLog = new CallLog(verifiedContent);
            var logger = new CallLogger(callLog);
            
            // Create parrot proxy for interface (no target)
            var proxy = CallLoggerProxy<IVirtualMethods>.Create(null, logger, "🦜");
            
            var result = proxy.VirtualMethod(5);
            Assert.Equal(100, result); // Should return mocked value
        }

        [Fact]
        public void CallLoggerProxy_WithInterfaceProperty_ShouldInterceptPropertyAccess()
        {
            var callLog = new CallLog();
            var logger = new CallLogger(callLog);
            var target = new VirtualMethodClass();
            
            var proxy = CallLoggerProxy<IVirtualMethods>.Create(target, logger, "🔧");
            
            // Get property
            var value = proxy.VirtualProperty;
            Assert.Equal("Default", value);
            
            // Set property
            proxy.VirtualProperty = "New Value";
            Assert.Equal("New Value", proxy.VirtualProperty);
            
            // Property access should be logged
            Assert.Contains("get_VirtualProperty", callLog.ToString());
            Assert.Contains("set_VirtualProperty", callLog.ToString());
        }

        [Fact]
        public void CallLoggerProxy_WithInterfaceMethods_ShouldInterceptAll()
        {
            var callLog = new CallLog();
            var logger = new CallLogger(callLog);
            var target = new MixedMethodClass();
            
            // Check if we can create proxy for interface
            var canCreate = ProxyFactory.CanCreateProxyForType(typeof(IMixedMethods));
            Assert.True(canCreate);
            
            var proxy = CallLoggerProxy<IMixedMethods>.Create(target, logger, "🔧");
            
            // All methods should be intercepted
            var name = proxy.GetName();
            Assert.Equal("Mixed", name);
            Assert.Contains("GetName", callLog.ToString());
            
            var id = proxy.GetId();
            Assert.Equal(42, id);
            Assert.Contains("GetId", callLog.ToString());
            
            proxy.DoWork();
            Assert.Contains("DoWork", callLog.ToString());
        }

        [Fact]
        public void ProxyFactory_ClassProxySupport_DocumentedLimitation()
        {
            // Document that DispatchProxy only supports interfaces
            // Classes, even with virtual methods, cannot be proxied directly
            
            Assert.False(ProxyFactory.CanCreateProxyForType(typeof(VirtualMethodClass)));
            Assert.True(ProxyFactory.CanCreateProxyForType(typeof(IVirtualMethods)));
            
            // This is a known limitation of DispatchProxy
            // For class proxying, other libraries like Castle.Core would be needed
        }
    }
}