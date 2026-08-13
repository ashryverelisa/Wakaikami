using Microsoft.Extensions.Logging;

namespace Wakaikami.Content.Data;

public static class ShnLoadGuard
{
    public static bool Run(ILogger logger, string what, Action load)
    {
        try
        {
            load();
            return true;
        }
        catch (Exception ex)
        {
            logger.ShnLoadFailed(ex, what);
            return false;
        }
    }
}
