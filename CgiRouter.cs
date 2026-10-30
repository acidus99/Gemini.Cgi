using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Gemini.Cgi;

using RequestCallback = Action<CgiWrapper>;

public class CgiRouter
{
    private readonly List<Tuple<string, RequestCallback>> _routeCallbacks = new();
    private RequestCallback? _parsingCallback;
    private StaticFileModule? _staticModule;

    /// <summary>
    /// Provide an optional callback which is called on incoming requests
    /// helpful for parsing and then setting additional variables
    /// </summary>
    /// <param name="parsingCallback"></param>
    public CgiRouter(Action<CgiWrapper>? parsingCallback = null)
    {
        _parsingCallback = parsingCallback;
    }

    public void OnRequest(string route, RequestCallback callback)
    {
        if (string.IsNullOrWhiteSpace(route)) throw new ArgumentException("route not be empty or null", nameof(route));
        _routeCallbacks.Add(new Tuple<string, RequestCallback>(route.ToLower(), callback));
    }

    public void SetStaticRoot(string relativeDir)
    {
        _staticModule =
            new StaticFileModule(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                relativeDir));
    }

    public void TestRequest(string cgiName, string url)
    {
        if (string.IsNullOrWhiteSpace(cgiName))
            throw new ArgumentException("CGI program name cannot be null or empty.");

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty.");

        var uri = new Uri(url);

        if (!string.Equals(uri.Scheme, "gemini", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("URL must use the gemini scheme.");

        string fullPath = uri.AbsolutePath; // includes leading '/'
        string queryString = string.IsNullOrEmpty(uri.Query)
            ? string.Empty
            : uri.Query.TrimStart('?');

        string serverName = uri.Host;

        // Locate cgiName in the path
        int programIndex = fullPath.IndexOf(
            "/" + cgiName,
            StringComparison.OrdinalIgnoreCase);

        if (programIndex < 0)
            throw new InvalidOperationException(
                $"CGI program '{cgiName}' not found in URL path.");

        int scriptEnd = programIndex + cgiName.Length + 1;

        // SCRIPT_NAME = path up to and including CGI program
        string scriptName = fullPath.Substring(0, scriptEnd);

        // PATH_INFO = remainder after CGI program
        string pathInfo = fullPath.Length > scriptEnd
            ? fullPath.Substring(scriptEnd)
            : string.Empty;

        Environment.SetEnvironmentVariable("GATEWAY_INTERFACE", "CGI/1.1");
        Environment.SetEnvironmentVariable("SERVER_PROTOCOL", "GEMINI");
        Environment.SetEnvironmentVariable("GEMINI_URL", url);
        Environment.SetEnvironmentVariable("SCRIPT_NAME", scriptName);
        Environment.SetEnvironmentVariable("PATH_INFO", pathInfo);
        Environment.SetEnvironmentVariable("QUERY_STRING", queryString);
        Environment.SetEnvironmentVariable("SERVER_NAME", serverName);
        
        ProcessRequest();
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

                if (_parsingCallback != null) _parsingCallback(cgiWrapper);
                //find the route
                var callback = FindExactRoute(cgiWrapper.PathInfo);
                if (callback != null)
                {
                    callback(cgiWrapper);
                    return;
                }

                //do we have a static module registered, and was it able to service the request?
                if (_staticModule != null && _staticModule.HandleRequest(cgiWrapper)) return;

                //find the route
                callback = FindRoute(cgiWrapper.PathInfo);
                if (callback != null)
                {
                    callback(cgiWrapper);
                    return;
                }

                HandleMissedRoute(cgiWrapper);
            }
            catch (Exception ex)
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
    {
        return _routeCallbacks.Where(x => route == x.Item1)
            .Select(x => x.Item2).FirstOrDefault();
    }

    /// <summary>
    /// Finds the first callback that registered for a route
    /// We use "starts with" because we need to support routes that use parts of the path
    /// to pass variables/state (e.g. /search/{language}/{other-options}?search-term
    /// </summary>
    /// <param name="route"></param>
    private RequestCallback? FindRoute(string route)
    {
        return _routeCallbacks.Where(x => route.StartsWith(x.Item1))
            .Select(x => x.Item2).FirstOrDefault();
    }
}