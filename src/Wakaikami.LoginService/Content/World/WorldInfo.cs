namespace Wakaikami.LoginService.Content.World;

public sealed class WorldInfo
{
    public sbyte Id { get; init; }
    public required string WorldName { get; init; }
    public required string Ip { get; init; }
    public required ushort Port { get; init; }
    public bool IsTestServer { get; init; }
}
