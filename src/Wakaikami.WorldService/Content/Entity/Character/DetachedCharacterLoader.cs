using System.Data;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Wakaikami.Content.Entity.Character.Data;
using Wakaikami.Database.World.Repositories.Interfaces;
using Wakaikami.WorldService.Content.Entity.Character.Interfaces;

namespace Wakaikami.WorldService.Content.Entity.Character;

public sealed partial class DetachedCharacterLoader(ICharacterRepository characterRepository, ILogger<DetachedCharacterLoader> logger)
    : IDetachedCharacterLoader
{
    public async Task<(WorldCharacter? character, CharacterErrors error)> TryLoadAsync(int characterId, CancellationToken cancellationToken = default)
    {
        try
        {
            var character = await characterRepository.WithCharacterDataAsync(characterId, ReadCharacter, cancellationToken);
            if (character is not null)
                return (character, CharacterErrors.None);

            LogCharacterNotFound(characterId);
            return (null, CharacterErrors.Info);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogLoadFailed(ex, characterId);
            return (null, CharacterErrors.DataErr);
        }
    }

    private static WorldCharacter? ReadCharacter(IDataReader reader)
    {
        if (!reader.Read())
            return null;

        var character = new WorldCharacter
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Slot = reader.GetByte(reader.GetOrdinal("Slot")),
            Level = reader.GetByte(reader.GetOrdinal("Level")),
            Position = new Vector2(reader.GetInt32(reader.GetOrdinal("PosX")), reader.GetInt32(reader.GetOrdinal("PosY"))),
        };

        character.SetMap(reader.GetString(reader.GetOrdinal("MapIndex")));
        character.Shape.Apply(
            new CharacterShapeData(
                ClassId: reader.GetByte(reader.GetOrdinal("Class")),
                RaceId: reader.GetByte(reader.GetOrdinal("Race")),
                IsMale: reader.GetByte(reader.GetOrdinal("IsMale")) != 0,
                HairTypeId: reader.GetByte(reader.GetOrdinal("Hair")),
                HairColorId: reader.GetByte(reader.GetOrdinal("HairColor")),
                FaceId: reader.GetByte(reader.GetOrdinal("Face"))
            )
        );

        return character;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Character {CharId} not found or deleted")]
    private partial void LogCharacterNotFound(int charId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to load character {CharId}")]
    private partial void LogLoadFailed(Exception exception, int charId);
}
