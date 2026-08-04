using Wakaikami.Content.World;
using Wakaikami.Content.World.Enums;

namespace Wakaikami.WorldService.Tests.Content.World;

public sealed class GameServerStatusTests
{
    [Theory]
    [InlineData(0, 100, GameServerState.Low)] // 0%
    [InlineData(24, 100, GameServerState.Low)] // 24%
    [InlineData(25, 100, GameServerState.Medium)] // 25%
    [InlineData(49, 100, GameServerState.Medium)] // 49%
    [InlineData(50, 100, GameServerState.High)] // 50%
    [InlineData(99, 100, GameServerState.High)] // 99%
    [InlineData(100, 100, GameServerState.Full)] // 100%
    [InlineData(150, 100, GameServerState.Full)] // over capacity
    public void CalculateVariousLoadsReturnsExpectedState(int now, int max, GameServerState expected)
    {
        Assert.Equal(expected, GameServerStatus.Calculate(now, max));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CalculateNonPositiveCapacityReturnsFull(int max)
    {
        Assert.Equal(GameServerState.Full, GameServerStatus.Calculate(50, max));
    }
}
