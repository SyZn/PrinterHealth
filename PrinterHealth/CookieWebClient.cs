using System;
using System.Net;

namespace PrinterHealth
{
    public class CookieWebClient : WebClient
    {
        public readonly CookieContainer CookieJar = new CookieContainer();

        public bool IgnoreCookiePaths { get; set; }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var webRequest = base.GetWebRequest(address);
            var httpRequest = webRequest as HttpWebRequest;
            if (httpRequest != null)
            {
                httpRequest.CookieContainer = CookieJar;
            }
            return webRequest;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            var response = base.GetWebResponse(request);
            PutUnpathedCookiesIntoJar(response);
            return response;
        }

        protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            var response = base.GetWebResponse(request);
            PutUnpathedCookiesIntoJar(response);
            return response;
        }

        /// <summary>
        /// Adds copies of the received cookies, but with a path set to "/".
        /// </summary>
        /// <param name="webResponse">The response from which to read the cookies.</param>
        private void PutUnpathedCookiesIntoJar(WebResponse webResponse)
        {
            if (!IgnoreCookiePaths)
            {
                return;
            }

            var response = webResponse as HttpWebResponse;
            if (response == null)
            {
                return;
            }
            foreach (Cookie cookie in response.Cookies)
            {
                CookieJar.Add(new Cookie(
                    cookie.Name,
                    cookie.Value,
                    "/",
                    cookie.Domain
                ));
            }
        }
    }
}
