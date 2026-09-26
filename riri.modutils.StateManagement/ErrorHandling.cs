namespace riri.modutils.StateManagement;

/// <summary>
/// Exception used when a module is not found
/// </summary>
public class ModuleConstructorNotFoundException : Exception
{
    /// <inheritdoc/>
    public ModuleConstructorNotFoundException() {}

    /// <inheritdoc/>
    public ModuleConstructorNotFoundException(Type type) : base(type.Name) {}

    /// <inheritdoc/>
    public ModuleConstructorNotFoundException(string message) : base(message) {}

    /// <inheritdoc/>
    public ModuleConstructorNotFoundException(string message, Exception inner) : base(message, inner) {}
}