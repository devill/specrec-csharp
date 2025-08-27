namespace SpecRec
{
    public class Context
    {
        public CallLog CallLog { get; }
        internal ObjectFactory Factory { get; }
        internal CallLogger CallLogger { get; }
        internal Parrot ParrotFactory { get; }
        internal string? TestCaseName { get; }
        
        public Context(CallLog callLog, ObjectFactory factory, string? testCaseName = null)
        {
            CallLog = callLog ?? throw new ArgumentNullException(nameof(callLog));
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
            CallLogger = new CallLogger(callLog, factory);
            ParrotFactory = new Parrot(callLog, factory);
            TestCaseName = testCaseName;
        }

        // Substitute - sets up ObjectFactory to create new parrots automatically for T
        public Context Substitute<T>(string icon = "🔧") where T : class
        {
            Factory.SetAutoParrot<T>(CallLog, icon);
            return this;
        }

        // CallLogger operations - work with existing objects
        public T Wrap<T>(T obj, string icon = "🔧") where T : class
        {
            return CallLogger.Wrap<T>(obj, icon);
        }

        public T Parrot<T>(string icon = "🦜") where T : class
        {
            return ParrotFactory.Create<T>(icon);
        }

        // ObjectFactory operations - register existing objects
        public Context SetAlways<T>(T obj, string? id = null) where T : class
        {
            Factory.SetAlways<T>(obj, id);
            return this;
        }

        public Context SetOne<T>(T obj, string? id = null) where T : class
        {
            Factory.SetOne<T>(obj, id);
            return this;
        }

        public Context Register<T>(T obj, string id) where T : class
        {
            Factory.Register<T>(obj, id);
            return this;
        }

        // Display name for test parameters
        public override string ToString() => TestCaseName ?? "DefaultCase";
    }
}