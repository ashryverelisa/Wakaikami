using Wakaikami.Core.Time;
using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.WorldService.GameNetwork.Protocol.Misc.Server;

public class NcServerTimeNotifyCmd() : FiestaServerPacket(FiestaHandlerType.Misc, GameHandler02Type.NcServerTimeNotifyCmd)
{
    private readonly DateTime _time = ServerClock.LocalNow;

    public override void Write()
    {
        Writer.Write(_time.Second);
        Writer.Write(_time.Minute);
        Writer.Write(_time.Hour);
        Writer.Write(_time.Day);
        Writer.Write(_time.Month - 1);
        Writer.Write(_time.Year - 1900);
        Writer.Write((int)_time.DayOfWeek);
        Writer.Write(_time.DayOfYear);
        Writer.Write<int>(_time.IsDaylightSavingTime() ? 1 : 0);
        Writer.Write((byte)TimeZoneInfo.Local.GetUtcOffset(_time).TotalHours);
    }
}
