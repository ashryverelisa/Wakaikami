using Wakaikami.Content.World.Enums;

namespace Wakaikami.Content.World;

public static class GameServerStatus
{
    public static GameServerState Calculate(int nowCount, int maxCount)
    {
        if (maxCount <= 0)
            return GameServerState.Full;

        return (nowCount * 100 / maxCount) switch
        {
            < 25 => GameServerState.Low,
            < 50 => GameServerState.Medium,
            < 100 => GameServerState.High,
            _ => GameServerState.Full,
        };
    }
}
