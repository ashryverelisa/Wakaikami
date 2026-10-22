namespace Wakaikami.Core.IO.Shn;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ShnColumnAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}