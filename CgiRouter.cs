using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using System.Linq;

namespace Gemini.Cgi;

using RequestCallback = Action<CgiWrapper>;

public class CgiRouter
{
    private readonly List<Tuple<string, RequestCallback>> routeCallbacks = new List<Tuple<string, RequestCallback>>();
    private StaticFileModule? staticModule;
    private Action<CgiWrapper>? ParsingCallback;

    /// <summary>
    /// Provide an optional callback which is called on incoming requests
    /// helpful for parsing and the nsetting additional variables
    /// </summary>
    /// <param name="parsingCallback"></param>
    public CgiRouter(Action<CgiWrapper>? parsingCallback = null)
    {
        ParsingCallback = parsingCallback;
    }

    public void OnRequest(string route, RequestCallback callback)
    {
        if(string.IsNullOrWhiteSpace(route))
        {
            throw new ArgumentException("route not be empty or null", nameof(route));
        }
        routeCallbacks.Add(new Tuple<string, RequestCallback>(route.ToLower(), callback));
    }

    public void SetStaticRoot(string relativeDir)
    {
        staticModule = new StaticFileModule(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), relativeDir));
    }

    public void ProcessRequest()
    {
        using (var cgiWrapper = new CgiWrapper())
        {
            try
            {
                //always have a / as the base route.
                //requests to "/cgi-bin/example.cgi" will be redirected to "/cgi-bin/example.cgi/"
                if (cgiWrapper.PathInfo == "")
                {
                    cgiWrapper.RedirectPermanent(cgiWrapper.RequestUrl.AbsolutePath + "/");
                    return;
                }

                if(ParsingCallback != null)
                {
                    ParsingCallback(cgiWrapper);
                }
                //find the route
                var callback = FindExactRoute(cgiWrapper.PathInfo);
                if (callback != null)
                {
                    callback(cgiWrapper);
                    return;
                }
                //do we have a static module registered, and was it able to service the request?
                if(staticModule != null && staticModule.HandleRequest(cgiWrapper))
                {
                    return;
                }

                //find the route
                callback = FindRoute(cgiWrapper.PathInfo);
                if (callback != null)
                {
                    callback(cgiWrapper);
                    return;
                }

                HandleMissedRoute(cgiWrapper);
            } catch(Exception ex)
            {
                HandleException(cgiWrapper, ex.Message);
                cgiWrapper.Writer.WriteLine(ex.StackTrace);
            }
        }
    }

    private void HandleMissedRoute(CgiWrapper cgiWrapper)
    {
        cgiWrapper.Missing("No routes for request");
    }

    private void HandleException(CgiWrapper cgiWrapper, string msg)
    {
        cgiWrapper.Failure($"Encountered an exception: {msg}");
    }

    /// <summary>
    /// Finds a callback that exactly matches the routeregistered for a route
    /// </summary>
    /// <param name="route"></param>
    private RequestCallback? FindExactRoute(string route)
        => routeCallbacks.Where(x => route == x.Item1)
            .Select(x => x.Item2).FirstOrDefault();

    /// <summary>
    /// Finds the first callback that registered for a route
    /// We use "starts with" because we need to support routes that use parts of the path
    /// to pass variables/state (e.g. /search/{language}/{other-options}?search-term
    /// </summary>
    /// <param name="route"></param>
    private RequestCallback? FindRoute(string route)
        => routeCallbacks.Where(x => route.StartsWith(x.Item1))
            .Select(x => x.Item2).FirstOrDefault();

}
