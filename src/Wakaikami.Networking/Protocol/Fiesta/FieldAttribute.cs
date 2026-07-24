namespace Wakaikami.Networking.Protocol.Fiesta;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class FieldAttribute(int order) : Attribute
{
    public int Order { get; } = order;
    public int Length { get; set; }
    public Type? As { get; set; }
}
