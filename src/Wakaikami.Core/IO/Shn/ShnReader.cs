using Microsoft.Extensions.Options;
using Wakaikami.Core.IO.Shn.Interfaces;

namespace Wakaikami.Core.IO.Shn;

public sealed class ShnReader(IOptions<ShnOptions> options) : IShnReader
{
    private readonly string _baseDirectory = options.Value.BaseDirectory;

    public ShnTable Load(string relativePath)
    {
        var fullPath = string.IsNullOrEmpty(_baseDirectory) ? relativePath : Path.Combine(_baseDirectory, relativePath);
        var table = ShnReaderCore.Load(fullPath);
        return table;
    }
}
