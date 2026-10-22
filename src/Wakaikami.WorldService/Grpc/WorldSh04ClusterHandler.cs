using Microsoft.Extensions.Logging;
using Wakaikami.WorldService.Content.Entity.Character;
using Wakaikami.WorldService.Content.Zone;
using Wakaikami.WorldService.Grpc.Handlers.Server;

namespace Wakaikami.WorldService.Grpc;

public sealed partial class WorldSh04ClusterHandler(ILogger<WorldSh04ClusterHandler> logger) : ISh04ClusterHandler
{
    public void NcCharacterOnline(WorldCharacter character, ZoneServer spawnServer) => LogCharacterOnline(character.Name, character.Id);

    public void NcCharacterOffline(WorldCharacter character) => LogCharacterOffline(character.Name, character.Id);

    public void NcCharacterChangeId(bool removed, string name, int characterId) => LogCharacterChangeId(name, characterId, removed);

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Character {Name} ({CharId}) online")]
    private partial void LogCharacterOnline(string name, int charId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Character {Name} ({CharId}) offline")]
    private partial void LogCharacterOffline(string name, int charId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Character {Name} ({CharId}) id changed, removed: {Removed}")]
    private partial void LogCharacterChangeId(string name, int charId, bool removed);
}
