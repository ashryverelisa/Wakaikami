namespace Wakaikami.Networking.HandlerStores.Attributes;

/// <summary>
/// Binds a handler method to a packet opcode and its packet class; consumed by the source
/// generator, never read at runtime.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class GameHandlerAttribute(ushort handlerType, Type classType) : Attribute
{
    public ushort HandleType { get; } = handlerType;

    public Type ClassType { get; } = classType;
}
