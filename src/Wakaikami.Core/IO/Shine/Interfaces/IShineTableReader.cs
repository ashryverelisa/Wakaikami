namespace Wakaikami.Core.IO.Shine.Interfaces;

public interface IShineTableReader
{
    public ShineTableFile Load(string relativePath);
}
