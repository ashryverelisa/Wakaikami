using System.Diagnostics.CodeAnalysis;

namespace Wakaikami.LoginService.Content.World.Interfaces;

public interface IWorldServerManager
{
    public IReadOnlyList<WorldServer> ToList();
    public bool GetWorldById(sbyte worldId, [NotNullWhen(true)] out WorldServer? server);

    public WorldServer Register(WorldInfo info);
}
