using System;
using System.Net;
using System.Text;

namespace ManualMF
{
    class HttpListenerRequestParams: IHttpRequestParams
    {
        HttpListenerRequest m_Request;
        public HttpListenerRequestParams(HttpListenerRequest Request) { m_Request = Request; }

        public string[] AcceptTypes
        {
            get { return m_Request.AcceptTypes; }
        }

        public Encoding ContentEncoding
        {
            get { return m_Request.ContentEncoding; }
        }

        public long ContentLength64
        {
            get { return m_Request.ContentLength64; }
        }

        public string ContentType
        {
            get { return m_Request.ContentType; }
        }

        public System.Collections.ICollection Cookies
        {
            get { return m_Request.Cookies; }
        }

        public System.Collections.Specialized.NameValueCollection Headers
        {
            get { return m_Request.Headers; }
        }

        public string HttpMethod
        {
            get { return m_Request.HttpMethod; }
        }

        public System.IO.Stream InputStream
        {
            get { return m_Request.InputStream; }
        }

        public bool IsAuthenticated
        {
            get { return m_Request.IsAuthenticated; }
        }

        public bool IsLocal
        {
            get { return m_Request.IsLocal; }
        }

        public bool IsSecureConnection
        {
            get { return m_Request.IsSecureConnection; }
        }

        public System.Collections.Specialized.NameValueCollection QueryString
        {
            get { return m_Request.QueryString; }
        }

        public System.Net.IPAddress RemoteAddress
        {
            get { return m_Request.RemoteEndPoint.Address; }
        }

        public Uri Url
        {
            get { return m_Request.Url; }
        }

        public Uri UrlReferrer
        {
            get { return m_Request.UrlReferrer; }
        }

        public string UserAgent
        {
            get { return m_Request.UserAgent; }
        }

        public string[] UserLanguages
        {
            get { return m_Request.UserLanguages; }
        }
    }
}
