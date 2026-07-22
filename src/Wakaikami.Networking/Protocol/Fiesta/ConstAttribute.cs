namespace Wakaikami.Networking.Protocol.Fiesta;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ConstAttribute(int order, object value) : Attribute
{
    public int Order { get; } = order;
    public object Value { get; } = value;
}
