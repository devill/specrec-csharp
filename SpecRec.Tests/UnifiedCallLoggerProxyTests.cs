using System.Text;
using Xunit;

namespace SpecRec.Tests
{
    public class UnifiedCallLoggerProxyTests
    {
        public interface ITestService
        {
            int Calculate(int x, int y);
            string GetName();
            void DoWork();
        }

        public class TestServiceImpl : ITestService
        {
            public int Calculate(int x, int y) => x + y;
            public string GetName() => "TestService";
            public void DoWork() { }
        }

        [Fact]
        public void CallLoggerProxy_WithTarget_ShouldActAsLogger()
        {
            var callLog = new CallLog();
            var logger = new CallLogger(callLog);
            var target = new TestServiceImpl();
            
            // Create proxy with target (logging mode)
            var proxy = CallLoggerProxy<ITestService>.Create(target, logger, "🔧");
            
            var result = proxy.Calculate(5, 3);
            
            Assert.Equal(8, result); // Should return actual result from target
            Assert.Contains("Calculate", callLog.ToString());
            Assert.Contains("x: 5", callLog.ToString());
            Assert.Contains("y: 3", callLog.ToString());
            Assert.Contains("Returns: 8", callLog.ToString());
        }

        [Fact]
        public void CallLoggerProxy_WithoutTarget_ShouldActAsParrot()
        {
            var verifiedContent = """
                🦜 Calculate:
                  🔸 x: 5
                  🔸 y: 3
                  🔹 Returns: 42

                """;
            
            var callLog = new CallLog(verifiedContent);
            var logger = new CallLogger(callLog);
            
            // Create proxy without target (parrot mode)
            var proxy = CallLoggerProxy<ITestService>.Create(null, logger, "🦜");
            
            var result = proxy.Calculate(5, 3);
            
            Assert.Equal(42, result); // Should return value from verified file
        }

        [Fact]
        public void CallLoggerProxy_SwitchingBetweenModes_ShouldWorkCorrectly()
        {
            var verifiedContent = """
                🦜 GetName:
                  🔹 Returns: "ParrotName"

                """;
            
            var callLog = new CallLog(verifiedContent);
            var logger = new CallLogger(callLog);
            
            // First create as parrot (no target)
            var parrot = CallLoggerProxy<ITestService>.Create(null, logger, "🦜");
            Assert.Equal("ParrotName", parrot.GetName());
            
            // Then create as logger (with target)
            var target = new TestServiceImpl();
            var loggingProxy = CallLoggerProxy<ITestService>.Create(target, logger, "🔧");
            Assert.Equal("TestService", loggingProxy.GetName());
        }

        [Fact]
        public void CallLoggerProxy_InParrotMode_WithMissingReturnValue_ShouldThrow()
        {
            var verifiedContent = """
                🦜 Calculate:
                  🔸 x: 5
                  🔸 y: 3
                  🔹 Returns: <missing_value>

                """;
            
            var callLog = new CallLog(verifiedContent);
            var logger = new CallLogger(callLog);
            var proxy = CallLoggerProxy<ITestService>.Create(null, logger, "🦜");
            
            Assert.Throws<ParrotMissingReturnValueException>(() => proxy.Calculate(5, 3));
        }

        [Fact]
        public void CallLoggerProxy_InParrotMode_WithVoidMethod_ShouldNotRequireReturnValue()
        {
            var verifiedContent = """
                🦜 DoWork:

                """;
            
            var callLog = new CallLog(verifiedContent);
            var logger = new CallLogger(callLog);
            var proxy = CallLoggerProxy<ITestService>.Create(null, logger, "🦜");
            
            // Should not throw
            proxy.DoWork();
        }

        [Fact]
        public void CallLoggerProxy_InLoggingMode_WithException_ShouldLogAndRethrow()
        {
            var callLog = new CallLog();
            var logger = new CallLogger(callLog);
            
            var target = new ThrowingService();
            var proxy = CallLoggerProxy<ITestService>.Create(target, logger, "🔧");
            
            var ex = Assert.Throws<InvalidOperationException>(() => proxy.Calculate(1, 2));
            Assert.Equal("Calculation failed", ex.Message);
            
            Assert.Contains("Calculate", callLog.ToString());
            Assert.Contains("Exception: Calculation failed", callLog.ToString());
        }

        [Fact]
        public void CallLoggerProxy_WithIgnoredCall_ShouldNotLog()
        {
            var callLog = new CallLog();
            var logger = new CallLogger(callLog);
            
            // Use a service that ignores its own call
            var target = new TestServiceWithIgnoredCall();
            var proxy = CallLoggerProxy<ITestService>.Create(target, logger, "🔧");
            
            proxy.Calculate(5, 3);
            
            // The call should not be logged because the method itself calls IgnoreCall()
            Assert.DoesNotContain("Calculate", callLog.ToString());
        }

        [Fact]
        public void CallLoggerProxy_WithIgnoredArgument_ShouldNotLogSpecificArgument()
        {
            var callLog = new CallLog();
            var logger = new CallLogger(callLog);
            var target = new TestServiceWithIgnoredArgs();
            
            var proxy = CallLoggerProxy<ITestService>.Create(target, logger, "🔧");
            
            proxy.Calculate(5, 3);
            
            // First argument should be ignored
            Assert.DoesNotContain("x: 5", callLog.ToString());
            Assert.Contains("y: 3", callLog.ToString());
        }

        [Fact]
        public void CallLoggerProxy_WithIgnoredReturnValue_ShouldNotLogReturn()
        {
            var callLog = new CallLog();
            var logger = new CallLogger(callLog);
            var target = new TestServiceWithIgnoredReturn();
            
            var proxy = CallLoggerProxy<ITestService>.Create(target, logger, "🔧");
            
            var result = proxy.Calculate(5, 3);
            
            Assert.Equal(8, result); // Should still return the value
            Assert.Contains("Calculate", callLog.ToString());
            Assert.DoesNotContain("Returns:", callLog.ToString());
        }

        [Fact]
        public void CallLoggerProxy_UnifiedBehavior_ShouldMaintainBackwardCompatibility()
        {
            var callLog = new CallLog();
            var logger = new CallLogger(callLog);
            var target = new TestServiceImpl();
            
            // Old way using Wrap (should still work)
            var wrappedProxy = logger.Wrap<ITestService>(target, "🔧");
            var result1 = wrappedProxy.Calculate(2, 3);
            Assert.Equal(5, result1);
            
            // New way using CallLoggerProxy.Create
            var directProxy = CallLoggerProxy<ITestService>.Create(target, logger, "🔨");
            var result2 = directProxy.Calculate(4, 5);
            Assert.Equal(9, result2);
            
            // Both should be logged
            Assert.Contains("🔧 Calculate", callLog.ToString());
            Assert.Contains("🔨 Calculate", callLog.ToString());
        }

        private class ThrowingService : ITestService
        {
            public int Calculate(int x, int y) => throw new InvalidOperationException("Calculation failed");
            public string GetName() => "Throwing";
            public void DoWork() { }
        }

        private class TestServiceWithIgnoredArgs : ITestService
        {
            public int Calculate(int x, int y)
            {
                CallLogFormatterContext.IgnoreArgument(0); // Ignore first argument
                return x + y;
            }
            public string GetName() => "Test";
            public void DoWork() { }
        }

        private class TestServiceWithIgnoredReturn : ITestService
        {
            public int Calculate(int x, int y)
            {
                CallLogFormatterContext.IgnoreReturnValue();
                return x + y;
            }
            public string GetName() => "Test";
            public void DoWork() { }
        }

        private class TestServiceWithIgnoredCall : ITestService
        {
            public int Calculate(int x, int y)
            {
                CallLogFormatterContext.IgnoreCall();
                return x + y;
            }
            public string GetName() => "Test";
            public void DoWork() { }
        }
    }
}