using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

namespace Gemini.Cgi;

public class CgiWrapper : IDisposable
{
    public string ExecutingPath { get; private set; }

    public bool HasQuery
        => (Query.Length > 0);

    public Stream Out { get; private set; }

    public StreamWriter Writer { get; private set; }

    /// <summary>
    /// Is this CGI executing on localhost? Checks by looking at the hostname of the incoming URL.
    /// </summary>
    public bool IsLocalHost
        /*
         * TODO: This isn't the most robust check. Any 127.* address is technically local host.
         * It also doesn't work for IPv6. I'm a little limited here since the URL comes in as a string.
         * This is good enough for what I'm doing (Allowing CGI's to show extra debug info if running
         * on local host
         */
        => RequestUrl.Host == "127.0.0.1" || RequestUrl.Host == "localhost";

    public string PathInfo
        => Environment.GetEnvironmentVariable("PATH_INFO") ?? "";

    public string Query
        => WebUtility.UrlDecode(RawQuery);

    public string RawQuery
        => Environment.GetEnvironmentVariable("QUERY_STRING") ?? "";

    public string RemoteAddress
        => Environment.GetEnvironmentVariable("REMOTE_ADDR") ?? "";

    public Uri RequestUrl { get; private set; }

    public string ScriptName
        => Environment.GetEnvironmentVariable("SCRIPT_NAME") ?? "";

    public CgiWrapper()
    {
        Out = Console.OpenStandardOutput();
        Writer = new StreamWriter(Out, new UTF8Encoding(false));
        //Writer.AutoFlush = true;
        RequestUrl = new Uri(Environment.GetEnvironmentVariable("GEMINI_URL") ?? "about:blank");
        ExecutingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    }

    //for gemini, its just about removing the new lines
    public string SantiziedQuery
        => Query.Trim().Replace("\n", " ").Replace("\r", " ");

    public void Input(string prompt)
        => WriteStatusLine(10, prompt);

    public void Success(string mimeType = "text/gemini")
        => WriteStatusLine(20, mimeType);

    public void Redirect(string url)
        => WriteStatusLine(30, url);

    public void RedirectPermanent(string url)
        => WriteStatusLine(31, url);

    public void Failure(string msg)
        => WriteStatusLine(50, msg);

    public void Missing(string msg)
        => WriteStatusLine(51, msg);

    public void BadRequest(string msg)
        => WriteStatusLine(59, msg);

    private void WriteStatusLine(int statusCode, string msg)
        //implement as directly writing bytes
        => Out.Write(Encoding.UTF8.GetBytes($"{statusCode} {msg}\r\n"));

    public void Dispose()
    {
        Writer.Flush();
        Writer.Dispose();
    }

    public static bool IsRunningAsCgi
        => (Environment.GetEnvironmentVariable("GEMINI_URL") != null);
}