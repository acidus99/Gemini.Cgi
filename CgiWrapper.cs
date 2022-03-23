using System;
using System.IO;
using System.Net;
using System.Text;

namespace Gemini.Cgi
{
	public class CgiWrapper
	{
		public bool HasQuery
			=> (Query.Length > 0);

		public TextWriter Out { get; private set; }

		public string PathInfo
			=> Environment.GetEnvironmentVariable("PATH_INFO") ?? "";

		public string Query
			=> WebUtility.UrlDecode(RawQuery);

		public string RawQuery
			=> Environment.GetEnvironmentVariable("QUERY_STRING") ?? "";

		public string RemoteAddress
			=> Environment.GetEnvironmentVariable("REMOTE_ADDR") ?? "";

		public Uri RequestUrl { get; private set; }

        public CgiWrapper()
		{
            Out = Console.Out;
			RequestUrl = new Uri(Environment.GetEnvironmentVariable("GEMINI_URL") ?? "");
		}

        public void Input(string prompt)
            => WriteStatusLine(10, prompt);

        public void Success(string mimeType = "text/gemini")
            => WriteStatusLine(20, mimeType);

        public void Redirect(string url)
            => WriteStatusLine(30, url);

        public void Missing(string msg)
            => WriteStatusLine(51, msg);

        public void BadRequest(string msg)
            => WriteStatusLine(59, msg);

        private void WriteStatusLine(int statusCode, string msg)
            => Write($"{statusCode} {msg}\r\n");

        public void Write(string text)
            => Out.Write(text);

        public void WriteLine(string text = "")
            => Out.WriteLine(text);
    }
}