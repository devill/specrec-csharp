namespace SpecRec
{
    /// <summary>
    /// Interface for test doubles that need to log constructor arguments to the storybook.
    /// When a fake implements this interface, ObjectFactory will call 
    /// ConstructorCalledWith with the exact arguments passed to Create.
    /// </summary>
    public interface IConstructorCalledWith
    {
        /// <summary>
        /// Called by ObjectFactory with the constructor arguments before object creation.
        /// Implement this method to log constructor arguments to your storybook for test verification.
        /// </summary>
        /// <param name="args">The constructor arguments that will be passed to the real implementation</param>
        void ConstructorCalledWith(params object[] args);
    }
}