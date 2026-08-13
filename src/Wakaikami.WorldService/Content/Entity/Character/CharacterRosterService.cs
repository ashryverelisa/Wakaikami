using Microsoft.Extensions.Logging;
using Wakaikami.Database.World.Repositories.Interfaces;
using Wakaikami.WorldService.Content.Entity.Character.Enums;
using Wakaikami.WorldService.Content.Entity.Character.Results;

namespace Wakaikami.WorldService.Content.Entity.Character;

public sealed partial class CharacterRosterService(ICharacterRepository characterRepository, ILogger<CharacterRosterService> logger)
{
    private static class CreateCode
    {
        public const int Success = -1;
        public const int NameInUse = -2;
        public const int MaxSlotReached = -3;
    }

    private static class DeleteCode
    {
        public const int Success = 0;
        public const int AlreadyDeleted = 1;
        public const int NotFound = 2;
        public const int RenameDeleteFailed = 3;
    }

    private const byte DefaultSpawnDirection = 0;

    public async Task<CreateCharacterResult> CreateCharacterAsync(int accountId, CreateCharacterRequest req, CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await characterRepository.CreateCharacterAsync(
                accountId,
                req.Name,
                req.Slot,
                req.Level,
                req.Money,
                req.Hp,
                req.Sp,
                req.Lp,
                req.HpStones,
                req.SpStones,
                req.MapIndex,
                (int)req.StartPosition.X,
                (int)req.StartPosition.Y,
                DefaultSpawnDirection,
                req.ClassId,
                req.IsMale,
                req.Hair,
                req.HairColor,
                req.Face,
                cancellationToken
            );

            if (res.Code != CreateCode.Success)
            {
                return CreateCharacterResult.Failed(
                    res.Code switch
                    {
                        CreateCode.NameInUse => CharacterCreateOutcome.NameInUse,
                        CreateCode.MaxSlotReached => CharacterCreateOutcome.MaxSlotReached,
                        _ => CharacterCreateOutcome.Failed,
                    }
                );
            }

            LogCharacterCreated(req.Name);

            // TODO: Skills load

            return CreateCharacterResult.Created(res.CharId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogCreateFailed(ex);
            return CreateCharacterResult.Failed(CharacterCreateOutcome.Failed);
        }
    }

    public async Task<bool> DeleteCharacterAsync(int charId, CancellationToken cancellationToken = default)
    {
        try
        {
            var nRet = await characterRepository.DeleteCharacterAsync(charId, cancellationToken);

            switch (nRet)
            {
                case DeleteCode.Success:
                    return true;
                case DeleteCode.AlreadyDeleted:
                    LogAlreadyDeleted(charId);
                    return false;
                case DeleteCode.NotFound:
                    LogDeleteNotFound(charId);
                    return false;
                case DeleteCode.RenameDeleteFailed:
                    LogDeleteRenameFailed(charId);
                    return false;
                default:
                    return false;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogDeleteFailed(ex);
            return false;
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Character {CharacterName} created")]
    private partial void LogCharacterCreated(string characterName);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Granted {Count} default skills to character {Name}")]
    private partial void LogDefaultSkillsGranted(int count, string name);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Error creating character account:")]
    private partial void LogCreateFailed(Exception exception);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Error,
        Message = "Failed to grant {Count} default skills to character {Name} ({CharId}) - character was created without them"
    )]
    private partial void LogDefaultSkillsFailed(int count, string name, int charId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Character {CharId} already deleted")]
    private partial void LogAlreadyDeleted(int charId);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Deleted Character {CharId} not found")]
    private partial void LogDeleteNotFound(int charId);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "rename deleting Character {CharId} failed")]
    private partial void LogDeleteRenameFailed(int charId);

    [LoggerMessage(EventId = 7, Level = LogLevel.Error, Message = "Error deleting character account")]
    private partial void LogDeleteFailed(Exception exception);
}
