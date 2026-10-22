using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Wakaikami.Content.Data;
using Wakaikami.Content.Entity.Character.Data.Models;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.Core.IO.Shn;
using Wakaikami.Core.IO.Shn.Interfaces;

namespace Wakaikami.WorldService.Content.Entity.Character.Data;

public sealed class CharacterShapeProvider(IShnReader shn, ILogger<CharacterShapeProvider> logger, InitialType initialType) : IDataLoader
{
    private const string FaceInfoShnFile = "FaceInfo.shn";
    private const string HairInfoShnFile = "HairInfo.shn";
    private const string HairColorInfoShnFile = "HairColorInfo.shn";

    public IReadOnlyDictionary<byte, FaceInfo> FaceInfosById { get; private set; } = FrozenDictionary<byte, FaceInfo>.Empty;
    public IReadOnlyDictionary<byte, HairInfo> HairInfosById { get; private set; } = FrozenDictionary<byte, HairInfo>.Empty;
    public IReadOnlyDictionary<byte, HairColorInfo> HairColorInfosById { get; private set; } = FrozenDictionary<byte, HairColorInfo>.Empty;

    InitialType IModule<DataStage>.InitialType => initialType;
    DataStage IModule<DataStage>.Stage => DataStage.Character;

    bool IDataLoader.Load() =>
        ShnLoadGuard.Run(logger, FaceInfoShnFile, () => FaceInfosById = shn.Load(FaceInfoShnFile).MapTo<FaceInfo>().ToFrozenDictionary(x => x.Id))
        && ShnLoadGuard.Run(
            logger,
            HairColorInfoShnFile,
            () => HairColorInfosById = shn.Load(HairColorInfoShnFile).MapTo<HairColorInfo>().ToFrozenDictionary(x => x.Id)
        )
        && ShnLoadGuard.Run(logger, HairInfoShnFile, () => HairInfosById = shn.Load(HairInfoShnFile).MapTo<HairInfo>().ToFrozenDictionary(x => x.Id));

    /// <summary>All known face ids (unordered).</summary>
    public bool TryGetFaceInfoById(byte id, [NotNullWhen(true)] out FaceInfo? info) => FaceInfosById.TryGetValue(id, out info);

    public bool TryGetHairInfoById(byte id, [NotNullWhen(true)] out HairInfo? info) => HairInfosById.TryGetValue(id, out info);

    public bool TryGetHairColorInfoById(byte id, [NotNullWhen(true)] out HairColorInfo? info) => HairColorInfosById.TryGetValue(id, out info);
}
