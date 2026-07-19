using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Wakaikami.Networking.Grpc.Extensions;

public static class GrpcCallExtensions
{
    public static void Forget<TReply>(this AsyncUnaryCall<TReply> call, ILogger logger) => _ = AwaitAndLogAsync(call, logger);

    private static async Task AwaitAndLogAsync<TReply>(AsyncUnaryCall<TReply> call, ILogger logger)
    {
        using (call)
        {
            try
            {
                await call.ResponseAsync;
            }
            catch (Exception ex)
            {
                logger.GrpcFireAndForgetFailed(ex);
            }
        }
    }
}
