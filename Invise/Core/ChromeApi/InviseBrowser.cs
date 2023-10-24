using CefSharp;
using CefSharp.Wpf;
using Invise.Core.ChromeApi.Handlers;
using System;

namespace Invise.Core.ChromeApi;
public class InviseBrowser : ChromiumWebBrowser
{
    public InviseBrowser(RequestContext context) 
    {
        RequestContext = context;
        MenuHandler = new MenuHandler();
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        var context = (RequestContext)RequestContext;

        if (!context.IsDisposed)
            context.Dispose();
    }

    internal void LoadUrl(Uri uri)
    {
        throw new NotImplementedException();
    }
}