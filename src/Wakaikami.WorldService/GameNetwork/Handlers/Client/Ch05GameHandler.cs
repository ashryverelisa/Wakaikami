using Wakaikami.Networking.HandlerStores.Attributes;
using Wakaikami.Networking.HandlerTypes;
using Wakaikami.WorldService.Configuration;
using Wakaikami.WorldService.Content.Entity.Character;
using Wakaikami.WorldService.Content.Entity.Character.Data;
using Wakaikami.WorldService.Content.Entity.Character.Enums;
using Wakaikami.WorldService.Content.Entity.Character.Interfaces;
using Wakaikami.WorldService.GameNetwork.Handlers.Server;
using Wakaikami.WorldService.GameNetwork.Listening;
using Wakaikami.WorldService.GameNetwork.Protocol.Avatar;
using Wakaikami.WorldService.GameNetwork.Protocol.Avatar.Client;
using Wakaikami.WorldService.Grpc.Handlers.Server;

namespace Wakaikami.WorldService.GameNetwork.Handlers.Client;

[GameHandlerClass(FiestaHandlerType.Avatar)]
public class Ch05GameHandler(
    ISh05GameHandler sh05GameHandler,
    ISh04ClusterHandler sh04Cluster,
    ISh03ClusterHandler sh03Cluster,
    CharacterNameValidator nameValidator,
    DefaultCharacterDataProvider defaultCharacterDataProvider,
    CharacterRosterService characterRosterService,
    IDetachedCharacterLoader detachedCharacterLoader
)
{
    [GameHandler(GameHandler05Type.NcAvatarCreateReq, typeof(NcAvatarCreateReq))]
    public async Task NcAvatarCreateReq(WorldSession session, NcAvatarCreateReq packet)
    {
        if (!nameValidator.IsValid(packet.Name))
        {
            sh05GameHandler.NcAvatarCreateFailAck(session, AvatarErrors.CharacterNameTaken);
            return;
        }

        if (session.CharacterList.Count >= WorldConstants.MaxCharacterPerWorld || session.CharacterList.Contains(packet.Slot))
        {
            sh05GameHandler.NcAvatarCreateFailAck(session, AvatarErrors.CharacterErrorMaxSlot);
            return;
        }

        if (!defaultCharacterDataProvider.TryGetDefaultCharacter(packet.Class, out var info))
        {
            sh05GameHandler.NcAvatarCreateFailAck(session, AvatarErrors.CharacterWrongClass);
            return;
        }

        if (session.CharacterList.HighestLevel < info.MinLevel)
        {
            sh05GameHandler.NcAvatarCreateFailAck(session, AvatarErrors.CharacterLv60Req);
            return;
        }

        if (!info.Hairs.Contains(packet.HairType) || !info.Faces.Contains(packet.FaceShape) || !info.HairColors.Contains(packet.HairColor))
        {
            sh05GameHandler.NcAvatarCreateFailAck(session, AvatarErrors.CharacterFailedCreate);
            return;
        }

        var character = new CreateCharacterRequest
        {
            Slot = packet.Slot,
            Name = packet.Name,
            ClassId = packet.Class,
            IsMale = packet.IsMale,
            Hair = packet.HairType,
            HairColor = packet.HairColor,
            Face = packet.FaceShape,
            MapIndex = info.StartMap.Index,
            StartPosition = info.StartPosition,
            Level = info.StartLevel,
            Money = info.Money,
            Hp = info.Hp,
            Sp = info.Sp,
            Lp = info.Lp,
            HpStones = info.HpStones,
            SpStones = info.SpStones,
            Skills = info.Skills,
        };

        var result = await characterRosterService.CreateCharacterAsync(session.Account!.Id, character, session.SessionToken);
        if (!result.Ok)
        {
            sh05GameHandler.NcAvatarCreateFailAck(session, ToCreateError(result.Outcome));
            return;
        }

        var (worldCharacter, _) = await detachedCharacterLoader.TryLoadAsync(result.CharId, session.SessionToken);

        CharacterListEntry? entry = null;
        try
        {
            if (worldCharacter is not null)
            {
                entry = new CharacterListEntry(worldCharacter);
                if (!session.CharacterList.Add(entry))
                    entry = null;
            }
        }
        finally
        {
            worldCharacter?.Dispose();
        }

        if (entry is null)
        {
            await characterRosterService.DeleteCharacterAsync(result.CharId, CancellationToken.None);
            sh05GameHandler.NcAvatarCreateFailAck(session, AvatarErrors.CharacterFailedCreate);
            return;
        }

        sh03Cluster.NcAccountCharacterCreate(session.Account!.Id);
        sh05GameHandler.NcAvatarCreateSuccessAck(session, entry);
    }

    [GameHandler(GameHandler05Type.NcAvatarEraseReq, typeof(NcAvatarEraseReq))]
    public async Task NcAvatarEraseReq(WorldSession session, NcAvatarEraseReq packet)
    {
        var character = session.CharacterList[packet.Slot];

        if (character is null)
        {
            // Handler runs on the session parse loop; DisposeAsync would self-join. Keep sync.
            // ReSharper disable once MethodHasAsyncOverload
            session.Dispose();
            return;
        }

        if (!await characterRosterService.DeleteCharacterAsync(character.Id, session.SessionToken))
        {
            sh05GameHandler.NcAvatarEraseFailAck(session, AvatarErrors.CharacterEraseFail);
            return;
        }

        if (!session.CharacterList.Remove(packet.Slot))
        {
            // Handler runs on the session parse loop; DisposeAsync would self-join. Keep sync.
            // ReSharper disable once MethodHasAsyncOverload
            session.Dispose();
            return;
        }

        sh04Cluster.NcCharacterChangeId(removed: true, character.Name, character.Id);
        sh05GameHandler.NcAvatarEraseSuccessAck(session, packet.Slot);
    }

    private static AvatarErrors ToCreateError(CharacterCreateOutcome outcome) =>
        outcome switch
        {
            CharacterCreateOutcome.NameInUse => AvatarErrors.CharacterNameInUse,
            CharacterCreateOutcome.MaxSlotReached => AvatarErrors.CharacterErrorMaxSlot,
            _ => AvatarErrors.CharacterFailedCreate,
        };
}
