﻿using System;
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