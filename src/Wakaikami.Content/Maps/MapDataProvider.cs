using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Wakaikami.Content.Maps.Data.Models;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.Core.IO.Shn;
using Wakaikami.Core.IO.Shn.Interfaces;

namespace Wakaikami.Content.Maps;

public partial class MapDataProvider(ILogger<MapDataProvider> logger, InitialType initialType, IShnReader shn) : IDataLoader
{
    private const string MapInfoShnFile = "MapInfo.shn";
    private FrozenDictionary<ushort, MapInfo> _mapInfosById = FrozenDictionary<ushort, MapInfo>.Empty;
    private FrozenDictionary<string, MapInfo> _mapInfosByIndex = FrozenDictionary<string, MapInfo>.Empty;

    InitialType IModule<DataStage>.InitialType => initialType;
    DataStage IModule<DataStage>.Stage => DataStage.Maps;

    bool IDataLoader.Load() => LoadCore();

    protected virtual bool LoadCore() => LoadMapInfosShn();

    private bool Guard(string source, Func<bool> load)
    {
        try
        {
            return load();
        }
        catch (Exception ex)
        {
            LogLoadFailed(ex, source);
            return false;
        }
    }

    private bool LoadMapInfosShn() =>
        Guard(
            MapInfoShnFile,
            () =>
            {
                var mapInfosById = new Dictionary<ushort, MapInfo>();
                var mapInfosByIndex = new Dictionary<string, MapInfo>(StringComparer.Ordinal);

                foreach (var info in shn.Load(MapInfoShnFile).MapTo<MapInfo>())
                {
                    if (!mapInfosById.TryAdd(info.Id, info) || !mapInfosByIndex.TryAdd(info.Index, info))
                    {
                        LogDuplicateMap(info.Id);
                        mapInfosById.Remove(info.Id);
                        mapInfosByIndex.Remove(info.Index);
                    }
                }

                _mapInfosById = mapInfosById.ToFrozenDictionary();
                _mapInfosByIndex = mapInfosByIndex.ToFrozenDictionary();
                return true;
            }
        );

    public bool TryGetMapInfoById(ushort id, [NotNullWhen(true)] out MapInfo? info) => _mapInfosById.TryGetValue(id, out info);

    public bool TryGetMapInfoByIndex(string index, [NotNullWhen(true)] out MapInfo? info) => _mapInfosByIndex.TryGetValue(index, out info);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Duplicate map found: {Id}")]
    private partial void LogDuplicateMap(ushort id);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Error loading map data from {Source}")]
    private partial void LogLoadFailed(Exception exception, string source);
}
