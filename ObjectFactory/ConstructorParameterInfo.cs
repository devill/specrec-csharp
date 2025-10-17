namespace ObjectFactory
{
    /// <summary>
    /// Contains information about a constructor parameter including its name, type, and value.
    /// </summary>
    public class ConstructorParameterInfo
    {
        /// <summary>
        /// The name of the parameter as defined in the constructor.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The type of the parameter.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// The actual value passed for this parameter.
        /// </summary>
        public object? Value { get; }

        public ConstructorParameterInfo(string name, Type type, object? value)
        {
            Name = name;
            Type = type;
            Value = value;
        }

        public override string ToString()
        {
            return $"{Name}: {Type.Name} = {Value}";
        }
    }
}
