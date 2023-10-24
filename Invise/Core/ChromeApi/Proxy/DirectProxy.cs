using System.Collections.Generic;

namespace Invise.Core.ChromeApi.Proxy;
public class DirectProxy : ChromeProxy
{
    public override Dictionary<string, object> GetContextPreference()
    {
        return new Dictionary<string, object>() { { "mode", "direct" } };
    }

    public override string GetProxyString()
    {
        return "";
    }

    public DirectProxy() : base(EProxyType.Direct, "", 0)
    {
    }
}