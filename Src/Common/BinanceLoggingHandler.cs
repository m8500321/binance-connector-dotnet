namespace Binance.Common
{
    using System.Net.Http;
    using System.Threading;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Binance message processing logging handler.<para/>
    /// A middlewear to listen and log any request or response.
    /// </summary>
    public class BinanceLoggingHandler : MessageProcessingHandler
    {
        private ILogger logger;

        public BinanceLoggingHandler(ILogger logger)
        : base(new HttpClientHandler())
        {
            // (base.InnerHandler as HttpClientHandler).ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            // (base.InnerHandler as HttpClientHandler).SslProtocols = System.Security.Authentication.SslProtocols.Ssl3;
            // var handler = new HttpClientHandler();
            // (base.InnerHandler as HttpClientHandler).ClientCertificateOptions = ClientCertificateOption.Manual;
            // ServicePointManager.ServerCertificateValidationCallback =  delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) 
            // {
            //     return true;
            // };
            this.logger = logger;
        }

        public BinanceLoggingHandler(ILogger logger, HttpMessageHandler innerHandler)
        : base(innerHandler)
        {
            this.logger = logger;
        }

        protected override HttpRequestMessage ProcessRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.logger.LogInformation(request.ToString());

            if (null != request.Content)
            {
                this.logger.LogInformation(request.Content.ReadAsStringAsync().Result);
            }

            return request;
        }

        protected override HttpResponseMessage ProcessResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            this.logger.LogInformation(response.ToString());

            if (null != response.Content)
            {
                this.logger.LogInformation(response.Content.ReadAsStringAsync().Result);
            }

            return response;
        }
    }
}