using Wakaikami.Networking.Grpc;
using Wakaikami.Networking.Grpc.Messages.ZonePush;
using Wakaikami.WorldService.Grpc.Interfaces;

namespace Wakaikami.WorldService.Grpc;

public sealed class ZonePushHub : PushHub<ZonePush>, IZonePushHub;
