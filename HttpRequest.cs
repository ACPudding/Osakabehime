using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Osakabehime
{
    internal static class HttpRequest
    {
        private const string METHOD_GET = "GET";
        private const string METHOD_POST = "POST";

        public static WebResponse Get(string url)
        {
            var request = SetupRequest(url);
            request.Method = METHOD_GET;
            return request.GetResponse();
        }

        private static HttpWebRequest SetupRequest(string url)
        {
            var request = (HttpWebRequest) WebRequest.Create(url);
            request.CookieContainer = new CookieContainer();
            request.AllowAutoRedirect = true;
            request.KeepAlive = true;
            request.ServicePoint.Expect100Continue = false;
            request.Accept = "gzip, identity";
            request.ContentType = "application/x-www-form-urlencoded";
            request.UserAgent = "fategrandorder/2.42.0 CFNetword/1312 Darwin/21.0.0";
            request.Timeout = 10000;
            return request;
        }

        public static string ToText(this WebResponse response)
        {
            using (var stream = response.GetResponseStream())
            {
                var streamReader = new StreamReader(stream, Encoding.UTF8);
                return streamReader.ReadToEnd();
            }
        }

        public static JObject ToJson(this WebResponse response)
        {
            var text = response.ToText();
            return JObject.Parse(text);
        }

        public static byte[] ToBinary(this WebResponse response)
        {
            using (var stream = response.GetResponseStream())
            {
                var readCount = 0;

                var bufferSize = 1 << 17;

                var buffer = new byte[bufferSize];
                using (var memory = new MemoryStream())
                {
                    while ((readCount = stream.Read(buffer, 0, bufferSize)) > 0) memory.Write(buffer, 0, readCount);
                    return memory.ToArray();
                }
            }
        }

        public static string GetApplicationUpdateJson()
        {
            var api = "https://api.github.com/repos/ACPudding/Osakabehime/releases/latest";
            ServicePointManager.SecurityProtocol = (SecurityProtocolType) 3072; //TLS1.2=3702
            var result = "";
            var req = WebRequest.Create(api) as HttpWebRequest;
            HttpWebResponse res = null;
            if (req == null) return result;
            req.Method = "GET";
            req.ContentType = @"application/octet-stream";
            req.UserAgent =
                @"Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.133 Safari/537.36";
            var postData = Encoding.GetEncoding("UTF-8").GetBytes("");
            if (postData.Length > 0)
            {
                req.ContentLength = postData.Length;
                req.Timeout = 15000;
                var outputStream = req.GetRequestStream();
                outputStream.Write(postData, 0, postData.Length);
                outputStream.Flush();
                outputStream.Close();
                try
                {
                    res = (HttpWebResponse) req.GetResponse();
                    var InputStream = res.GetResponseStream();
                    var encoding = Encoding.GetEncoding("UTF-8");
                    var sr = new StreamReader(InputStream, encoding);
                    result = sr.ReadToEnd();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return result;
                }
            }
            else
            {
                try
                {
                    res = (HttpWebResponse) req.GetResponse();
                    var InputStream = res.GetResponseStream();
                    var encoding = Encoding.GetEncoding("UTF-8");
                    var sr = new StreamReader(InputStream, encoding);
                    result = sr.ReadToEnd();
                    sr.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return result;
                }
            }

            return result;
        }

        public static string ReadableFilesize(double size)
        {
            string[] units = {"B", "KB", "MB", "GB", "TB", "PB"};
            var mod = 1024.0;
            var i = 0;
            while (size >= mod)
            {
                size /= mod;
                i++;
            }

            return Math.Round(size) + units[i];
        }
    }
}