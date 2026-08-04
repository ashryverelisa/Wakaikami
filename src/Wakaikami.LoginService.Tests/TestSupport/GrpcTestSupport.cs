using Grpc.Core;
using Grpc.Core.Testing;

namespace Wakaikami.LoginService.Tests.TestSupport;

internal static class GrpcTest
{
    public static ServerCallContext Context(CancellationToken cancellationToken = default) =>
        TestServerCallContext.Create(
            method: "test",
            host: null,
            deadline: DateTime.MaxValue,
            requestHeaders: [],
            cancellationToken: cancellationToken,
            peer: "ipv4:127.0.0.1",
            authContext: null,
            contextPropagationToken: null,
            writeHeadersFunc: _ => Task.CompletedTask,
            writeOptionsGetter: () => null,
            writeOptionsSetter: _ => { }
        );
}

internal sealed class TestServerStreamWriter<T> : IServerStreamWriter<T>
{
    public List<T> Written { get; } = [];

    public WriteOptions? WriteOptions { get; set; }

    public Task WriteAsync(T message)
    {
        Written.Add(message);
        return Task.CompletedTask;
    }

    public Task WriteAsync(T message, CancellationToken cancellationToken) => WriteAsync(message);
}
