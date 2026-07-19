using Wakaikami.LoginService.Grpc.Interfaces;
using Wakaikami.Networking.Grpc;
using Wakaikami.Networking.Grpc.Messages.WorldPush;

namespace Wakaikami.LoginService.Grpc;

public sealed class WorldPushHub : PushHub<WorldPush>, IWorldPushHub;
