using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Wakaikami.Content.Entity.Character.Data.Models.Enums;
using Wakaikami.Content.Maps;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.Core.IO.Shine;
using Wakaikami.Core.IO.Shine.Interfaces;
using Wakaikami.WorldService.Content.Entity.Character.Data.Models;

namespace Wakaikami.WorldService.Content.Entity.Character.Data;

public sealed partial class DefaultCharacterDataProvider(
    IShineTableReader shineTableReader,
    CharacterShapeProvider characterShapeProvider,
    CharacterClassProvider characterClassProvider,
    MapDataProvider mapDataProvider,
    ILogger<DefaultCharacterDataProvider> logger,
    InitialType initialType
) : IDataLoader
{
    InitialType IModule<DataStage>.InitialType => initialType;
    DataStage IModule<DataStage>.Stage => DataStage.DefaultData;

    private FrozenDictionary<byte, DefaultCharacterData> _characterInfoByClassId = FrozenDictionary<byte, DefaultCharacterData>.Empty;

    bool IDataLoader.Load() => LoadDefaultCharacterInfo();

    private bool LoadDefaultCharacterInfo()
    {
        try
        {
            var shineTableFile = shineTableReader.Load("DefaultCharacterData.txt");
            if (shineTableFile["CHARACTER"] is not { } characterTable)
            {
                LogMissingCharacterTable();
                return false;
            }

            var infos = new Dictionary<byte, DefaultCharacterData>();
            foreach (var info in characterTable.MapTo<DefaultCharacterData>())
            {
                if (!mapDataProvider.TryGetMapInfoByIndex(info.StartMapIndex, out var startMap))
                {
                    LogStartMapMissing(info.StartMapIndex, info.Class);
                    continue;
                }

                if (!characterClassProvider.TryGetClassInfo((UseClassType)info.Class, out var classInfo))
                {
                    LogClassInfoMissing(info.Class);
                    continue;
                }

                info.StartMap = startMap;
                info.ClassId = classInfo;
                infos[info.Class] = info;
            }

            LoadDefaultCharacterItems(shineTableFile, infos);
            LoadDefaultCharacterSkills(shineTableFile, infos);
            LoadDefaultCharacterShortcuts(shineTableFile, infos);
            LoadDefaultCharacterMaxShape(shineTableFile, infos);

            _characterInfoByClassId = infos.ToFrozenDictionary();
            return true;
        }
        catch (Exception ex)
        {
            LogLoadFailed(ex);
            return false;
        }
    }

    private void LoadDefaultCharacterItems(ShineTableFile file, Dictionary<byte, DefaultCharacterData> characters)
    {
        if (file["ITEM"] is not { } itemTable)
            return;

        var itemsByClass = new Dictionary<byte, Dictionary<ushort, byte>>();
        foreach (var item in itemTable.MapTo<DefaultCharacterItem>())
        {
            if (!characters.ContainsKey(item.Class))
            {
                LogItemUnknownClass(item.ItemId, item.Class);
                continue;
            }

            if (!itemsByClass.TryGetValue(item.Class, out var classItems))
            {
                classItems = new Dictionary<ushort, byte>();
                itemsByClass[item.Class] = classItems;
            }

            if (!classItems.TryAdd(item.ItemId, item.ItemAmount))
                LogDuplicateItem(item.ItemId, item.Class);
        }

        foreach (var (classId, classItems) in itemsByClass)
            characters[classId].Items = classItems.ToFrozenDictionary();
    }

    private void LoadDefaultCharacterSkills(ShineTableFile file, Dictionary<byte, DefaultCharacterData> characters)
    {
        if (file["SKILL"] is not { } skillTable)
            return;

        var skillsByClass = new Dictionary<byte, HashSet<ushort>>();
        foreach (var skill in skillTable.MapTo<DefaultCharacterSkill>())
        {
            if (!characters.ContainsKey(skill.Class))
                continue;

            if (!skillsByClass.TryGetValue(skill.Class, out var classSkills))
            {
                classSkills = [];
                skillsByClass[skill.Class] = classSkills;
            }

            if (!classSkills.Add(skill.SkillId))
                LogDuplicateSkill(skill.SkillId, skill.Class);
        }

        foreach (var (classId, classSkills) in skillsByClass)
            characters[classId].Skills = classSkills.ToFrozenSet();
    }

    private void LoadDefaultCharacterShortcuts(ShineTableFile file, Dictionary<byte, DefaultCharacterData> characters)
    {
        if (file["SHORTCUT"] is not { } shortcutTable)
            return;

        var shortcutsByClass = new Dictionary<byte, List<DefaultCharacterShortcut>>();
        foreach (var shortcut in shortcutTable.MapTo<DefaultCharacterShortcut>())
        {
            if (!characters.ContainsKey(shortcut.Class))
            {
                LogShortcutUnknownClass(shortcut.Class);
                continue;
            }

            if (!shortcutsByClass.TryGetValue(shortcut.Class, out var classShortcuts))
            {
                classShortcuts = [];
                shortcutsByClass[shortcut.Class] = classShortcuts;
            }

            classShortcuts.Add(shortcut);
        }

        foreach (var (classId, classShortcuts) in shortcutsByClass)
            characters[classId].Shortcuts = classShortcuts;
    }

    private void LoadDefaultCharacterMaxShape(ShineTableFile file, Dictionary<byte, DefaultCharacterData> characters)
    {
        if (file["MAXSHAPE"] is not { } maxShapeTable)
            return;

        var faces = NormalGradeIds(characterShapeProvider.FaceInfosById, x => x.Grade);
        var hairs = NormalGradeIds(characterShapeProvider.HairInfosById, x => x.Grade);
        var hairColors = NormalGradeIds(characterShapeProvider.HairColorInfosById, x => x.Grade);

        foreach (var maxShape in maxShapeTable.MapTo<DefaultCharacterMaxShape>())
        {
            if (!characters.TryGetValue(maxShape.Class, out var character))
                continue;

            character.Faces = faces;
            character.Hairs = hairs;
            character.HairColors = hairColors;
        }
    }

    private static FrozenSet<byte> NormalGradeIds<TInfo>(IReadOnlyDictionary<byte, TInfo> pool, Func<TInfo, BodyShopGrade> grade) =>
        pool.Where(x => grade(x.Value) == BodyShopGrade.Normal).Select(x => x.Key).ToFrozenSet();

    public bool TryGetDefaultCharacter(byte classId, [NotNullWhen(true)] out DefaultCharacterData? info) =>
        _characterInfoByClassId.TryGetValue(classId, out info);

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Missing CHARACTER table in DefaultCharacterData.txt")]
    private partial void LogMissingCharacterTable();

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Cant find Start Mapinfo {MapIndex} for class {ClassId}")]
    private partial void LogStartMapMissing(string mapIndex, byte classId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Cant find ClassInfo for class {ClassId}")]
    private partial void LogClassInfoMissing(byte classId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Failed Load DefaultCharacterData")]
    private partial void LogLoadFailed(Exception exception);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Default item {Item} references unknown class {Class}")]
    private partial void LogItemUnknownClass(ushort item, byte @class);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Duplicate default item {Item} for class {Class}")]
    private partial void LogDuplicateItem(ushort item, byte @class);

    [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "Duplicate default skill {Skill} for class {Class}")]
    private partial void LogDuplicateSkill(ushort skill, byte @class);

    [LoggerMessage(EventId = 8, Level = LogLevel.Warning, Message = "Default shortcut references unknown class {Class}")]
    private partial void LogShortcutUnknownClass(byte @class);
}
