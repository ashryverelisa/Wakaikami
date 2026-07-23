namespace Wakaikami.Networking.HandlerTypes;

public static class GameHandler03Type
{
    public const ushort NcUserXTrapReq = 4;
    public const ushort NcUserXTrapAck = 5;
    public const ushort NcUserLoginFailAck = 9;
    public const ushort NcUserLoginAck = 10;
    public const ushort NcUserWorldSelectReq = 11;
    public const ushort NcUserWorldSelectAck = 12;
    public const ushort NcUserLoginWorldReq = 15;
    public const ushort NcUserLoginWorldFailAck = 16;
    public const ushort NcUserLoginWorldAck = 20;
    public const ushort NcUserConnectCutCmd = 23;
    public const ushort NcUserNormalLogoutCmd = 24;
    public const ushort NcUserWorldStatusReq = 27;
    public const ushort NcUserWorldStatusAck = 28;
    public const ushort NcUserAvatarListReq = 31;
    public const ushort NcUserWillWorldSelectReq = 51;
    public const ushort NcUserWillWorldSelectAck = 52;
    public const ushort NcUserLoginWithOtpReq = 55;
    public const ushort NcUserUsLoginReq = 90;
    public const ushort NcUserClientVersionCheckReq = 101;
    public const ushort NcUserClientWrongVersionCheckAck = 102;
    public const ushort NcUserClientRightVersionCheckAck = 103;
}
