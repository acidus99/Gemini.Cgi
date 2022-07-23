# Gemini.Cgi
A CGI Wrapper for the Gemini protocol that:

* Automatically parses and normalizes the CGI environment variables.
* Functions to set responses like Input, successes, redirects, and errors
* Output stream buffering, with wrappers for sending UTF-8 text or binary content


Here is simple `hello.cgi` example:
```
var cgi = new CgiWrapper();
if(!cgi.HasQuery)
{
  cgi.Input("What is your name?");
  return;
}
cgi.Success();
cgi.Writer.WriteLine($"Well, hello there '{cgi.Query}'!");
```

Also includes a simple router, based on the `PATH_INFO` variable:
```
//example.cgi
static void Main(string[] args)
{
  var router = new CgiRouter();
  router.OnRequest("/hello", Hello);
  router.OnRequest("/time", );
  router.ProcessRequest()
}

//called on incoming requests to "/example.cgi/hello"
static void Hello(CgiWrapper cgi)
{ }

//called on incoming requests to "/example.cgi/time"
static void Hello(CgiWrapper cgi)
{ }
```
