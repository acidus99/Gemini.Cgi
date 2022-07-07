using System.IO;
using System.Net;

namespace Gemini.Cgi
{
    public class StaticFileModule
    {
        public string PublicRoot { get; set; } = "";

        public StaticFileModule(string publicRootPath)
        {
            PublicRoot = publicRootPath;
        }

        public bool HandleRequest(CgiWrapper cgi)
        {
            //path info can be empty if just requesting the CGI, so add a /
            var pathInfo = (cgi.PathInfo.Length > 0) ? cgi.PathInfo : "/";
            string attemptedPath = Path.GetFullPath("." + pathInfo, PublicRoot);
            attemptedPath = HandleDefaultFile(attemptedPath);
            if (!attemptedPath.StartsWith(PublicRoot))
            {
                cgi.BadRequest("Invalid request");
                return true;
            }

            if (File.Exists(attemptedPath))
            {
                SendFile(cgi, attemptedPath);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines if the requested path is a directory, and rewrites path to use default file
        /// </summary>
        private string HandleDefaultFile(string attemptedPath)
        {
            if (Directory.Exists(attemptedPath))
            {
                return attemptedPath + "index.gmi";
            }
            return attemptedPath;
        }

        /// <summary>
        /// Give a file, attempt to find a mimetype based on its file extension.
        /// TODO: Replace this with a better/extensible MIME Type config system
        /// </summary>
        private string MimeForFile(string filePath)
        {
            switch (ExtensionForFile(filePath))
            {
                case "gmi":
                    return "text/gemini";
                case "txt":
                    return "text/plain";

                case "png":
                    return "image/png";

                case "jpg":
                case "jpeg":
                    return "image/jpeg";

                case "gif":
                    return "image/gif";

                default:
                    return "application/octet-stream";
            }
        }

        private string ExtensionForFile(string filePath)
        {
            var ext = Path.GetExtension(filePath);
            return (ext.Length > 1) ? ext.Substring(1) : ext;
        }

        private void SendFile(CgiWrapper cgi, string filePath)
        {
            cgi.Success(MimeForFile(filePath));
            cgi.Out.Write(File.ReadAllBytes(filePath));
        }
    }
}