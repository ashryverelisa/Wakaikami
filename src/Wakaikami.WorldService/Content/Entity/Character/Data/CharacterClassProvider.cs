using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Wakaikami.Content.Data;
using Wakaikami.Content.Entity.Character.Data.Models;
using Wakaikami.Content.Entity.Character.Data.Models.Enums;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.Core.IO.Shn;
using Wakaikami.Core.IO.Shn.Interfaces;

namespace Wakaikami.WorldService.Content.Entity.Character.Data;

public sealed class CharacterClassProvider(IShnReader shn, ILogger<CharacterClassProvider> logger, InitialType initialType) : IDataLoader
{
    private const string ClassNameShnFile = "ClassName.shn";
    private const string UseClassTypeInfoShnFile = "UseClassTypeInfo.shn";

    private FrozenDictionary<UseClassType, ClassName> _classInfosById = FrozenDictionary<UseClassType, ClassName>.Empty;
    private FrozenDictionary<string, ClassName> _classInfosByName = FrozenDictionary<string, ClassName>.Empty;
    private FrozenDictionary<UseClassType, UseClassTypeInfo> _useClassTypeInfoByType = FrozenDictionary<UseClassType, UseClassTypeInfo>.Empty;
    public IReadOnlyCollection<ClassName> ClassList { get; private set; } = [];

    InitialType IModule<DataStage>.InitialType => initialType;
    DataStage IModule<DataStage>.Stage => DataStage.Character;

    bool IDataLoader.Load() =>
        ShnLoadGuard.Run(
            logger,
            ClassNameShnFile,
            () =>
            {
                var list = shn.Load(ClassNameShnFile).MapTo<ClassName>().ToList();
                _classInfosById = list.ToFrozenDictionary(x => (UseClassType)x.Id);

                _classInfosByName = list.DistinctBy(x => x.EngName, StringComparer.Ordinal).ToFrozenDictionary(x => x.EngName, StringComparer.Ordinal);
                ClassList = list;
            }
        )
        && ShnLoadGuard.Run(
            logger,
            UseClassTypeInfoShnFile,
            () => _useClassTypeInfoByType = shn.Load(UseClassTypeInfoShnFile).MapTo<UseClassTypeInfo>().ToFrozenDictionary(x => x.UseClass)
        );

    public bool TryGetClassInfo(UseClassType classId, [NotNullWhen(true)] out ClassName? info) => _classInfosById.TryGetValue(classId, out info);

    public bool TryGetUseClassTypeInfo(UseClassType type, [NotNullWhen(true)] out UseClassTypeInfo? info) =>
        _useClassTypeInfoByType.TryGetValue(type, out info);

    public bool TryGetPromoteClass(byte currentClassId, out byte nextClassId)
    {
        nextClassId = 0;
        if (!_classInfosById.TryGetValue((UseClassType)currentClassId, out var current))
            return false;

        if (!PromoteNameGraph.TryGetValue(current.EngName, out var nextEngName))
            return false;

        var target = _classInfosByName.GetValueOrDefault(nextEngName);
        if (target is null)
            return false;

        nextClassId = target.Id;
        return true;
    }

    // Tier-1 -> tier-2 first job-change promotions keyed by EngName (stable across rosters). Cross-checked against
    // the 8-entry cc_PromoteClass graph captured from the 2016 server; only the unambiguous base->tier-2 edges are
    // modelled here (tier-3 branches are quest-scenario-driven on the original and remain out of scope).
    private static readonly FrozenDictionary<string, string> PromoteNameGraph = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Fighter"] = "CleverFighter",
        ["Cleric"] = "HighCleric",
        ["Archer"] = "HawkArcher",
        ["Mage"] = "WizMage",
        ["Joker"] = "Chaser",
    }.ToFrozenDictionary(StringComparer.Ordinal);
}
