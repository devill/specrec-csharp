namespace ObjectFactory
{
    /// <summary>
    /// Interface for test doubles that need to log constructor arguments to the specbook.
    /// When a fake implements this interface, ObjectFactory will call
    /// ConstructorCalledWith with detailed parameter information including names, types, and values.
    /// </summary>
    public interface IConstructorCalledWith
    {
        /// <summary>
        /// Called by ObjectFactory with detailed constructor parameter information before object creation.
        /// This provides parameter names, types, and values for each constructor parameter.
        /// </summary>
        /// <param name="parameters">Array of parameter information including names, types, and values</param>
        void ConstructorCalledWith(ConstructorParameterInfo[] parameters);
    }
}
