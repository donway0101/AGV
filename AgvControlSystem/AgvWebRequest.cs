using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace AgvControlSystem
{

    //Todo custom exception.
    public struct HttpMethod
    {
        public const string GET = "GET";
        public const string PUT = "PUT";
        public const string POST = "POST";
        public const string DELETE = "DELETE";
    }

    /// <summary>
    /// Web communication with Mir Web server.
    /// </summary>
    public class AgvWebRequest
    {
        /// <summary>
        /// Contain information of user and password.
        /// </summary>
        public const string DefaultAuth =
            "Basic YWRtaW46OGM2OTc2ZTViNTQxMDQxNWJkZTkwOGJkNGRlZTE1ZGZiMTY3YTljODczZmM0YmI4YTgxZjZmMmFiNDQ4YTkxOA==";

        /// <summary>
        /// Get information from server.
        /// </summary>
        /// <param name="apiPath">Api path of Mir</param>
        /// <param name="timeoutSec">Request timeout</param>
        /// <returns></returns>
        public static string Get(string apiPath, string authorization = DefaultAuth, int timeoutSec = 5)
        {
            HttpWebRequest request = null;
            try
            {
                request = (HttpWebRequest)HttpWebRequest.Create(apiPath);              
            }
            catch (Exception)
            {
                throw ;
            }

            request.Method = HttpMethod.GET;
            request.ContentType = "application/json";
            request.Headers[HttpRequestHeader.Authorization] = authorization;
            request.Timeout = timeoutSec*1000;

            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (Exception)
            {
                throw new AgvDisconnectException(); ;
            }

            //Not Successfully retrieve the specified element
            if (response.StatusCode!= HttpStatusCode.OK)
            {
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new Exception("Invalid ordering or Invalid filters or Wrong output fields or Invalid limits");
                }
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new Exception("Not found");
                }
            }

            string responseString = string.Empty;
            try
            {
                responseString = GetResponseString(response);
            }
            catch (WebException)
            {
                throw new AgvDisconnectException();
            }
            catch (Exception)
            {
                throw;
            }

            return responseString;
        }

        private static string GetResponseString(HttpWebResponse response)
        {
            string responseString = string.Empty;
            try
            {
                Stream responseStream = response.GetResponseStream();
                StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8);
                responseString = streamReader.ReadToEnd();

                streamReader.Close();
                responseStream.Close();
                response.Close();
            }
            catch (Exception)
            {
                throw;
            }

            return responseString;
        }

        public static string Post(string apiPath, string body, string authorization = DefaultAuth, int timeoutSec = 5)
        {
            HttpWebRequest request = null;
            try
            {
                request = (HttpWebRequest)HttpWebRequest.Create(apiPath);
            }
            catch (Exception)
            {
                throw;
            }

            request.Method = HttpMethod.POST;
            request.ContentType = "application/json";
            request.Headers[HttpRequestHeader.Authorization] = authorization;
            request.Timeout = timeoutSec * 1000;

            try
            {
                byte[] BodyByte = Encoding.UTF8.GetBytes(body);
                request.ContentLength = BodyByte.Length;
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(BodyByte, 0, BodyByte.Length);
                requestStream.Close();
            }
            catch (Exception)
            {
                throw;
            }

            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException)
            {
                throw new AgvDisconnectException();
            }
            catch (Exception)
            {
                throw;
            }

            //The element has not been created successfully
            if (response.StatusCode != HttpStatusCode.Created)
            {
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new Exception("Argument error or Missing content type application/json on the header or Bad request or Invalid JSON");
                }
                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    throw new Exception("Duplicate entry");
                }
            }

            string responseString = string.Empty;
            try
            {
                responseString = GetResponseString(response);
            }
            catch (Exception)
            {
                throw; 
            }

            return responseString;            
        }

        public static string Put(string apiPath, string body, string authorization = DefaultAuth, int timeoutSec = 5)
        {
            HttpWebRequest request = null;
            try
            {
                request = (HttpWebRequest)HttpWebRequest.Create(apiPath);
            }
            catch (Exception)
            {
                throw;
            }

            request.Method = HttpMethod.PUT;
            request.ContentType = "application/json";
            request.Headers[HttpRequestHeader.Authorization] = authorization;
            request.Timeout = timeoutSec * 1000;

            try
            {
                byte[] BodyByte = Encoding.UTF8.GetBytes(body);
                request.ContentLength = BodyByte.Length;
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(BodyByte, 0, BodyByte.Length);
                requestStream.Close();
            }
            catch (Exception)
            {

                throw;
            }

            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException)
            {
                throw new AgvDisconnectException();
            }
            catch (Exception)
            {
                throw;
            }

            //Not Successfully retrieve the specified element
            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new Exception("Invalid ordering or Invalid filters or Wrong output fields or Invalid limits");
                }
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new Exception("Not found");
                }
            }

            string responseString = string.Empty;
            try
            {
                responseString = GetResponseString(response);
            }
            catch (Exception)
            {
                throw;
            }

            return responseString;
        }

        public static string Delete(string apiPath, string body = null, string authorization = DefaultAuth, int timeoutSec = 5)
        {
            HttpWebRequest request = null;
            try
            {
                request = (HttpWebRequest)HttpWebRequest.Create(apiPath);
            }
            catch (Exception)
            {
                throw;
            }

            request.Method = HttpMethod.DELETE;
            request.ContentType = "application/json";
            request.Headers[HttpRequestHeader.Authorization] = authorization;
            request.Timeout = timeoutSec * 1000;

            if (!String.IsNullOrEmpty(body))
            {
                try
                {
                    byte[] BodyByte = Encoding.UTF8.GetBytes(body);
                    request.ContentLength = BodyByte.Length;
                    Stream requestStream = request.GetRequestStream();
                    requestStream.Write(BodyByte, 0, BodyByte.Length);
                    requestStream.Close();
                }
                catch (Exception)
                {

                    throw;
                } 
            }

            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException)
            {
                throw new AgvDisconnectException();
            }
            catch (Exception)
            {
                throw;
            }

            //The element has not been deleted successfully
            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new Exception("Invalid filters or Invalid JSON or Argument error or Missing content type application / json on the header or Bad request or No fields");
                }
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new Exception("Not found");
                }
            }

            string responseString = string.Empty;
            try
            {
                responseString = GetResponseString(response);
            }
            catch (Exception)
            {
                throw;
            }

            return responseString;
        }
    }
}
