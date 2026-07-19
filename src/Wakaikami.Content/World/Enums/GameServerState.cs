namespace Wakaikami.Content.World.Enums;

public enum GameServerState : byte
{
    Offline = 0,
    Maintenance = 1,
    Reserved = 3,
    Full = 5,
    OK = 6,
    Low = 8,
    Medium = 9,
    High = 10,
}
