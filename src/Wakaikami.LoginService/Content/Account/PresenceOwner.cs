namespace Wakaikami.LoginService.Content.Account;

public readonly record struct PresenceOwner
{
    private const sbyte LoginWorldId = 0;

    private PresenceOwner(sbyte worldId) => WorldId = worldId;

    public sbyte WorldId { get; }

    public static PresenceOwner Login => new(LoginWorldId);

    public static PresenceOwner World(sbyte worldId) => new(worldId);

    public static bool TryWorld(int worldId, out PresenceOwner owner)
    {
        if (worldId is <= LoginWorldId or > sbyte.MaxValue)
        {
            owner = Login;
            return false;
        }

        owner = World((sbyte)worldId);
        return true;
    }
}
