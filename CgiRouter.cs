﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Gemini.Cgi
{
    using RequestCallback = System.Action<CgiWrapper>;

    public class CgiRouter
    {

        private readonly List<Tuple<string, RequestCallback>> routeCallbacks = new List<Tuple<string, RequestCallback>>();

        public void OnRequest(string route, RequestCallback callback)
            => routeCallbacks.Add(new Tuple<string, RequestCallback>(route.ToLower(), callback));

        public void ProcessRequest()
        {
            var cgiWrapper = new CgiWrapper();

            //find the route
            var callback = FindRoute(cgiWrapper.PathInfo);
            if (callback != null)
            {
                callback(cgiWrapper);
                return;
            }
            HandleMissedRoute(cgiWrapper);
        }

        private void HandleMissedRoute(CgiWrapper cgiWrapper)
        {
            cgiWrapper.Success();
            cgiWrapper.WriteLine("No routes");
        }

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
}