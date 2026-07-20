using System.Net.Sockets;

namespace Wakaikami.Networking.Extensions;

public static class SocketExtensions
{
    public static void Kill(this Socket socket)
    {
        try
        {
            socket.Shutdown(SocketShutdown.Both);
        }
        catch
        {
            // already disconnected or shut down - nothing to do
        }
        finally
        {
            socket.Dispose();
        }
    }
}
