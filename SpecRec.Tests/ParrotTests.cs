using System.IO;

namespace SpecRec.Tests
{
    public class ParrotTests
    {
        public class BasicWorkflow
        {
            [Fact]
            public async Task RecordPhase_ShouldCreateVerifiedFile()
            {
                // Record phase - use CallLogger to create the verified content
                var logger = new CallLogger();
                var mockCalculator = new TestCalculatorImplementation();
                var wrappedCalculator = logger.Wrap<ITestCalculator>(mockCalculator, "🧪");
                
                wrappedCalculator.Add(5, 3);
                wrappedCalculator.Multiply(4, 6);
                wrappedCalculator.Reset();
                
                // Verify the recorded content to create the .verified.txt file
                await Verify(logger.SpecBook.ToString());
            }

            [Fact]
            public void ReplayPhase_ShouldUseVerifiedFile()
            {
                // Replay phase - use Parrot with the verified file created by the record phase
                var verifiedFilePath = Path.Combine(GetTestDirectory(), "ParrotTests.BasicWorkflow.RecordPhase_ShouldCreateVerifiedFile.verified.txt");
                var callLog = CallLog.FromFile(verifiedFilePath);
                var parrot = Parrot<ITestCalculator>.Create(callLog);
                
                var result1 = parrot.Add(5, 3);
                var result2 = parrot.Multiply(4, 6);
                parrot.Reset();
                
                Assert.Equal(8, result1);
                Assert.Equal(24, result2);
                
                ((Parrot<ITestCalculator>)parrot).VerifyAllCallsWereMade();
            }
        }

        public class RepositoryExample
        {
            [Fact]
            public async Task Repository_RecordPhase_ShouldCreateVerifiedFile()
            {
                var logger = new CallLogger();
                var mockRepository = new TestRepositoryImplementation();
                var wrappedRepository = logger.Wrap<IRepository>(mockRepository, "🏪");
                
                var user = wrappedRepository.GetById(123);
                var saved = wrappedRepository.Save("UpdatedUser");
                
                await Verify(logger.SpecBook.ToString());
            }

            [Fact]
            public void Repository_ReplayPhase_ShouldUseVerifiedFile()
            {
                var verifiedFilePath = Path.Combine(GetTestDirectory(), "ParrotTests.RepositoryExample.Repository_RecordPhase_ShouldCreateVerifiedFile.verified.txt");
                var callLog = CallLog.FromFile(verifiedFilePath);
                var repository = Parrot<IRepository>.Create(callLog);
                
                var user = repository.GetById(123);
                var saved = repository.Save("UpdatedUser");
                
                Assert.Equal("User123", user);
                Assert.True(saved);
                
                ((Parrot<IRepository>)repository).VerifyAllCallsWereMade();
            }
        }

        public class ErrorHandling
        {
            [Fact]
            public async Task ErrorScenario_RecordPhase_ShouldCreateVerifiedFile()
            {
                var logger = new CallLogger();
                var mockService = new TestCalculatorImplementation();
                var wrappedService = logger.Wrap<ITestCalculator>(mockService, "🧪");
                
                wrappedService.Add(1, 2);
                wrappedService.Multiply(3, 4);
                
                await Verify(logger.SpecBook.ToString());
            }

            [Fact]
            public void ErrorScenario_ReplayPhase_WithWrongCall_ShouldThrowException()
            {
                var verifiedFilePath = Path.Combine(GetTestDirectory(), "ParrotTests.ErrorHandling.ErrorScenario_RecordPhase_ShouldCreateVerifiedFile.verified.txt");
                var callLog = CallLog.FromFile(verifiedFilePath);
                var parrot = Parrot<ITestCalculator>.Create(callLog);
                
                // Make the expected first call
                parrot.Add(1, 2);
                
                // Try to make wrong second call - this should throw
                var ex = Assert.Throws<ParrotCallMismatchException>(() =>
                    parrot.Add(99, 88)); // Expected Multiply(3, 4)
                
                Assert.Contains("Parrot<ITestCalculator> call failed", ex.Message);
                Assert.Contains("Call mismatch", ex.Message);
            }

            [Fact]
            public void ErrorScenario_ReplayPhase_WithMissedCalls_ShouldThrowException()
            {
                var verifiedFilePath = Path.Combine(GetTestDirectory(), "ParrotTests.ErrorHandling.ErrorScenario_RecordPhase_ShouldCreateVerifiedFile.verified.txt");
                var callLog = CallLog.FromFile(verifiedFilePath);
                var parrot = Parrot<ITestCalculator>.Create(callLog);
                
                // Only make the first call, miss the second
                parrot.Add(1, 2);
                
                var ex = Assert.Throws<InvalidOperationException>(() =>
                    ((Parrot<ITestCalculator>)parrot).VerifyAllCallsWereMade());
                
                Assert.Contains("Not all expected calls were made", ex.Message);
            }
        }

        public class ComplexScenarios
        {
            [Fact]
            public async Task Service_RecordPhase_WithMultipleTypes()
            {
                var logger = new CallLogger();
                var mockService = new TestServiceImplementation();
                var wrappedService = logger.Wrap<IParrotTestService>(mockService, "🔧");
                
                var message = wrappedService.GetMessage(200);
                Assert.Equal("Success", message);
                var isValid = wrappedService.IsValid("test");
                Assert.True(isValid);
                var optionalValue = wrappedService.GetOptionalValue("missing");
                Assert.Null(optionalValue);
                
                await Verify(logger.SpecBook.ToString());
            }

            [Fact]
            public void Service_ReplayPhase_WithMultipleTypes()
            {
                var verifiedFilePath = Path.Combine(GetTestDirectory(), "ParrotTests.ComplexScenarios.Service_RecordPhase_WithMultipleTypes.verified.txt");
                var callLog = CallLog.FromFile(verifiedFilePath);
                var parrot = Parrot<IParrotTestService>.Create(callLog);
                
                var message = parrot.GetMessage(200);
                var isValid = parrot.IsValid("test");
                var optionalValue = parrot.GetOptionalValue("missing");
                
                Assert.Equal("Success", message);
                Assert.True(isValid);
                Assert.Null(optionalValue);
                
                ((Parrot<IParrotTestService>)parrot).VerifyAllCallsWereMade();
            }
        }

        private static string GetTestDirectory()
        {
            // Use the source file location to find the test directory
            var currentDirectory = Directory.GetCurrentDirectory();
            
            // Look for the verified files in the current working directory first
            if (Directory.GetFiles(currentDirectory, "*.verified.txt").Length > 0)
                return currentDirectory;
            
            // Navigate up to find the test project directory
            var directory = new DirectoryInfo(currentDirectory);
            while (directory != null && !Directory.GetFiles(directory.FullName, "*.verified.txt").Any())
            {
                directory = directory.Parent;
            }
            
            return directory?.FullName ?? currentDirectory;
        }
    }

    public interface ITestCalculator
    {
        int Add(int a, int b);
        int Multiply(int x, int y);
        void Reset();
    }

    public class TestCalculatorImplementation : ITestCalculator
    {
        public int Add(int a, int b) => a + b;
        public int Multiply(int x, int y) => x * y;
        public void Reset() { }
    }

    public interface IParrotTestService
    {
        string GetMessage(int code);
        bool IsValid(string input);
        string? GetOptionalValue(string key);
    }

    public class TestServiceImplementation : IParrotTestService
    {
        public string GetMessage(int code) => "Success";
        public bool IsValid(string input) => true;
        public string? GetOptionalValue(string key) => null;
    }

    public interface IRepository
    {
        string GetById(int id);
        bool Save(string entity);
    }

    public class TestRepositoryImplementation : IRepository
    {
        public string GetById(int id) => "User123";
        public bool Save(string entity) => true;
    }
}