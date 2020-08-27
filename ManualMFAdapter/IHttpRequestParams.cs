using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;

namespace ManualMF
{
    //Gives common way to access properties for both System.Net.HttpRequest and System.Web.HttpRequestBase
    public interface IHttpRequestParams
    {
        string[] AcceptTypes { get; }
        Encoding ContentEncoding { get; }
        long ContentLength64 { get; }
        string ContentType { get; }
        ICollection Cookies { get; }
        NameValueCollection Headers { get; }
        string HttpMethod { get; }
        Stream InputStream { get; }
        bool IsAuthenticated { get; }
        bool IsLocal { get; }
        bool IsSecureConnection { get; }
        NameValueCollection QueryString { get; }
        IPAddress RemoteAddress { get; }
        Uri Url { get; }
        Uri UrlReferrer { get; }
        string UserAgent { get; }
        string[] UserLanguages { get; }
    }
}
