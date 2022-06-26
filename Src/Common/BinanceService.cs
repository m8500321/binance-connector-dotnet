namespace Binance.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using Newtonsoft.Json;

    /// <summary>
    /// Binance base class for REST sections of the API.
    /// </summary>
    public abstract class BinanceService
    {
        private const string V = "binance-connector-dotnet";
        private string apiKey;
        private string apiSecret;
        private string baseUrl;
        private HttpClient httpClient;
        static int logCount = 0;
        private static readonly object LOCK = new object();
        static public string dataDir = "";

        static public void LogMsg(params string[] msg)
        {
            string output = "\n\n[" + (logCount++) + "]:" + DateTime.Now + "\n";
            foreach (var item in msg)
            {
                output += (item + "\n");
            }
            lock (LOCK)
            {
                var curStr = "";
                using (StreamReader sr = new StreamReader(dataDir + "net_log.txt"))
                {
                    curStr = sr.ReadToEnd();
                }
                if (curStr.Length > 10000000)
                {
                    var newFile = "net_log__" + DateTime.Now.ToString("MM-dd__HH-mm-ss");
                    using (StreamWriter sw = new StreamWriter(dataDir + "backup/" + newFile + ".txt"))
                    {
                        sw.Write(curStr);
                    }
                }
                using (StreamWriter sw = new StreamWriter(dataDir + "net_log.txt", curStr.Length <= 10000000))
                {
                    sw.Write(output);
                }
            }

        }

        public BinanceService(HttpClient httpClient, string baseUrl, string apiKey, string apiSecret)
        {

            var curDir = System.Environment.CurrentDirectory;
            var idx = curDir.IndexOf("binance-connector-dotnet");
            dataDir = curDir.Substring(0, idx).Replace("\\", "/") + "binance-connector-dotnet/datas/";

            this.httpClient = httpClient;
            this.baseUrl = baseUrl;
            this.apiKey = apiKey;
            this.apiSecret = apiSecret;
        }

        protected async Task<T> SendPublicAsync<T>(string requestUri, HttpMethod httpMethod, Dictionary<string, object> query = null, object content = null)
        {
            if (!(query is null))
            {
                StringBuilder queryStringBuilder = this.BuildQueryString(query, new StringBuilder());

                if (queryStringBuilder.Length > 0)
                {
                    requestUri += "?" + queryStringBuilder.ToString();
                }
            }

            return await this.SendAsync<T>(requestUri, httpMethod, content);
        }

        protected async Task<T> SendSignedAsync<T>(string requestUri, HttpMethod httpMethod, Dictionary<string, object> query = null, object content = null)
        {
            StringBuilder queryStringBuilder = new StringBuilder();

            if (!(query is null))
            {
                queryStringBuilder = this.BuildQueryString(query, queryStringBuilder);
            }

            string signature = Sign(queryStringBuilder.ToString(), this.apiSecret);

            if (queryStringBuilder.Length > 0)
            {
                queryStringBuilder.Append("&");
            }

            queryStringBuilder.Append("signature=").Append(signature);

            requestUri += "?" + queryStringBuilder.ToString();

            return await this.SendAsync<T>(requestUri, httpMethod, content);
        }

        private static string Sign(string source, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            using (HMACSHA256 hmacsha256 = new HMACSHA256(keyBytes))
            {
                byte[] sourceBytes = Encoding.UTF8.GetBytes(source);

                byte[] hash = hmacsha256.ComputeHash(sourceBytes);

                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
            }
        }

        private StringBuilder BuildQueryString(Dictionary<string, object> queryParameters, StringBuilder builder)
        {
            foreach (KeyValuePair<string, object> queryParameter in queryParameters)
            {
                string queryParameterValue = Convert.ToString(queryParameter.Value, CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(queryParameterValue))
                {
                    if (builder.Length > 0)
                    {
                        builder.Append("&");
                    }

                    builder
                        .Append(queryParameter.Key)
                        .Append("=")
                        .Append(HttpUtility.UrlEncode(queryParameterValue));
                }
            }

            return builder;
        }

        private async Task<T> SendAsync<T>(string requestUri, HttpMethod httpMethod, object content = null)
        {
            using (var request = new HttpRequestMessage(httpMethod, this.baseUrl + requestUri))
            {
                if (!(content is null))
                {
                    request.Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");
                }

                if (!(this.apiKey is null))
                {
                    request.Headers.Add("X-MBX-APIKEY", this.apiKey);
                }

                LogMsg($"Request: ->>>\n{request}\n");
                HttpResponseMessage response = await this.httpClient.SendAsync(request);
                LogMsg($"Response: <<<-\n{response}\n");

                if (response.IsSuccessStatusCode)
                {
                    using (HttpContent responseContent = response.Content)
                    {

                        string jsonString = await responseContent.ReadAsStringAsync();
                        LogMsg($"Response content: <<<-\n{jsonString}\n");

                        if (typeof(T) == typeof(string))
                        {
                            return (T)(object)jsonString;
                        }
                        else
                        {
                            try
                            {
                                T data = JsonConvert.DeserializeObject<T>(jsonString);

                                return data;
                            }
                            catch (JsonReaderException ex)
                            {
                                var clientException = new BinanceClientException($"Failed to map server response from '${requestUri}' to given type", -1, ex);

                                clientException.StatusCode = (int)response.StatusCode;
                                clientException.Headers = response.Headers.ToDictionary(a => a.Key, a => a.Value);

                                throw clientException;
                            }
                        }
                    }
                }
                else
                {
                    using (HttpContent responseContent = response.Content)
                    {

                        BinanceHttpException httpException = null;
                        string contentString = await responseContent.ReadAsStringAsync();
                        LogMsg($"Response content: <<<-\n{contentString}\n");
                        int statusCode = (int)response.StatusCode;
                        if (400 <= statusCode && statusCode < 500)
                        {
                            if (string.IsNullOrWhiteSpace(contentString))
                            {
                                httpException = new BinanceClientException("Unsuccessful response with no content", -1);
                            }
                            else
                            {
                                try
                                {
                                    httpException = JsonConvert.DeserializeObject<BinanceClientException>(contentString);
                                }
                                catch (JsonReaderException ex)
                                {
                                    httpException = new BinanceClientException(contentString, -1, ex);
                                }
                            }
                        }
                        else
                        {
                            httpException = new BinanceServerException(contentString);
                        }

                        httpException.StatusCode = statusCode;
                        httpException.Headers = response.Headers.ToDictionary(a => a.Key, a => a.Value);

                        throw httpException;
                    }
                }
            }
        }
    }
}