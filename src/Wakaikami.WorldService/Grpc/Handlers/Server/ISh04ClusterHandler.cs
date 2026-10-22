using Wakaikami.WorldService.Content.Entity.Character;
using Wakaikami.WorldService.Content.Zone;

namespace Wakaikami.WorldService.Grpc.Handlers.Server;

public interface ISh04ClusterHandler
{
    public void NcCharacterOnline(WorldCharacter character, ZoneServer spawnServer);
    public void NcCharacterOffline(WorldCharacter character);
    public void NcCharacterChangeId(bool removed, string name, int characterId);
}
