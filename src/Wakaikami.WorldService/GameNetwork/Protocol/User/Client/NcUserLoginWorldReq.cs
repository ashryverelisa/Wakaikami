using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.WorldService.GameNetwork.Protocol.User.Client;

public class NcUserLoginWorldReq(byte[] buffer) : FiestaClientPacket(buffer)
{
    public Guid TransferId => _transferId;
    public string AccountName => _accountName.ToUpperInvariant();
    private string _accountName = null!;
    private Guid _transferId;
    private byte[]? _validateTail;
    private const int ValidateLength = 64;

    public override bool Read()
    {
        var nameLength = (int)Reader.Length - ValidateLength;

        if (nameLength > 0)
        {
            return Reader.ReadString(out _accountName, nameLength)
                && Reader.ReadGuid(out _transferId)
                && Reader.ReadBytes(ValidateLength - 32, out _validateTail);
        }

        Reader.LastFailedField = nameof(_accountName);
        return false;
    }

    protected override void DisposeInternal()
    {
        base.DisposeInternal();
        _accountName = null!;
        _validateTail = null;
    }
}
