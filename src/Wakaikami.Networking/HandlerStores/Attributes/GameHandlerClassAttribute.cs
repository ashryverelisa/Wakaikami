using Wakaikami.Networking.HandlerTypes;

namespace Wakaikami.Networking.HandlerStores.Attributes;

/// <summary>
/// Marks a handler class whose <c>[GameHandler]</c> methods the source generator
/// (PacketHandlerGenerator) registers as game packet handlers.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class GameHandlerClassAttribute(FiestaHandlerType type) : Attribute
{
    public FiestaHandlerType Type { get; } = type;
}