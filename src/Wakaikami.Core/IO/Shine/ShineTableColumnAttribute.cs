namespace Wakaikami.Core.IO.Shine;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ShineTableColumnAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
