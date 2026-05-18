using System.Net;
using System.Net.Http;

namespace Boj.Rackspace.UnitTest.Mocks
{
    internal class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;
        public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        { 
            _handler = handler; 
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }

        public static HttpClient Create(HttpStatusCode statusCode, string content)
        {
            var handler = new MockHttpMessageHandler((_, _) =>
            {
                var response = new HttpResponseMessage(statusCode)
                { Content = new StringContent(content) }; return Task.FromResult(response);
            });
            return new HttpClient(handler);
        }
    }
}
