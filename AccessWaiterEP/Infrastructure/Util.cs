using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using MVVrus.Utility;

namespace AccessWaiterEP.Infrastructure
{
    public struct IdAndTheRest
    {
        public int Id;
        public String Rest;
    }

    public static class Util
    {
        public static IdAndTheRest ExtractIntField(String Json, String FieldName)
        {
            //Possible exceptions are to be handled outside
            IdAndTheRest result;
            String field_string=JsonSurgery.ExtractField(Json, FieldName, out result.Rest);
            JavaScriptSerializer jss = new JavaScriptSerializer();
            int? t;
            try
            {
                t = (int?)jss.Deserialize(field_string ?? "null", typeof(Object));

            }
            catch (ArgumentException ex)
            {

                throw new FormatException("Malformed request: invalid JSON data", ex);
            }
            catch (InvalidCastException)
            {

                throw new FormatException("Malformed request: value of \""+FieldName+"\" field is not an integer");
            }
            if (null == t) throw new FormatException("Malformed request: \"instance_id\" field does not exist or has a null value");
            result.Id = t.Value;
            return result;
        }

        public static IdAndTheRest ExtractInstanceId(HttpRequestBase request)
        {
            // Json pattern for the request: {"instance_id":<id>,...}
            StreamReader rdr = new StreamReader(request.InputStream, request.ContentEncoding);
            String body = rdr.ReadToEnd();
            if (request.RequestType != "POST" || request.ContentType.ToLower() != "application/json") throw new HttpException(400,"Invalid HTTP method or content-type"); 
            return ExtractIntField(body, "instance_id");
        }
    }
}
