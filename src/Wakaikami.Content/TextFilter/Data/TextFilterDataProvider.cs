using System.Collections.Frozen;
using Microsoft.Extensions.Logging;
using Wakaikami.Content.TextFilter.Data.Models;
using Wakaikami.Content.TextFilter.Interfaces;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.Core.IO.Shn;
using Wakaikami.Core.IO.Shn.Interfaces;

namespace Wakaikami.Content.TextFilter.Data;

public sealed partial class TextFilterDataProvider(IShnReader shnReader, ILogger<TextFilterDataProvider> logger, InitialType initialType)
    : IDataLoader,
        INameFilter
{
    private FrozenSet<string> _badNames = FrozenSet<string>.Empty;

    InitialType IModule<DataStage>.InitialType => initialType;
    DataStage IModule<DataStage>.Stage => DataStage.TextFilter;

    bool IDataLoader.Load() => LoadBadNameFilter();

    private bool LoadBadNameFilter()
    {
        try
        {
            var shnTable = shnReader.Load("BadNameFilter.shn");
            _badNames = shnTable.MapTo<BadNameFilter>().Select(x => x.Name).ToFrozenSet(StringComparer.Ordinal);
            return true;
        }
        catch (Exception ex)
        {
            LogLoadFailed(ex);
            return false;
        }
    }

    public bool IsBadName(string name) => _badNames.Contains(name);

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to load BadNameFilter.")]
    private partial void LogLoadFailed(Exception exception);
}
