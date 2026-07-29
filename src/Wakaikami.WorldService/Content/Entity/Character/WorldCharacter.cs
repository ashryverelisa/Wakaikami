using Wakaikami.Content.Account;
using Wakaikami.Content.Entity.Character;
using Wakaikami.Networking.Session;

namespace Wakaikami.WorldService.Content.Entity.Character;

public class WorldCharacter : CharacterBase
{
    public byte Slot { get; set; }

    public AccountSession? AccountSession { get; private set; }

    /// <inheritdoc />
    public override FiestaSession? Session => AccountSession;

    /// <inheritdoc />
    protected override void ClearSession() => AccountSession = null;

    public override ushort SessionId => AccountSession is { IsDisposed: false } session ? session.SessionId : (ushort)0;
}
