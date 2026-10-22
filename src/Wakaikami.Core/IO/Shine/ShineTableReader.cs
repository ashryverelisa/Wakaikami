using Microsoft.Extensions.Options;
using Wakaikami.Core.IO.Shine.Interfaces;
using Wakaikami.Core.IO.Shn;

namespace Wakaikami.Core.IO.Shine;

public sealed class ShineTableReader(IOptions<ShnOptions> options) : IShineTableReader
{
    private readonly string _baseDirectory = options.Value.BaseDirectory;

    public ShineTableFile Load(string relativePath)
    {
        var fullPath = string.IsNullOrEmpty(_baseDirectory) ? relativePath : Path.Combine(_baseDirectory, relativePath);
        return ShineTableReaderCore.Load(fullPath);
    }
}
