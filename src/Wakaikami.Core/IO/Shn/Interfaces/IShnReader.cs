namespace Wakaikami.Core.IO.Shn.Interfaces;

public interface IShnReader
{
    public ShnTable Load(string relativePath);
}
