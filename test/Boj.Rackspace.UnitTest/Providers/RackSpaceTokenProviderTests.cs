using Boj.Rackspace.Infrastructure.Authentication;
using Boj.Rackspace.Infrastructure.Options;
using Boj.Rackspace.UnitTest.Fixtures;
using Boj.Rackspace.UnitTest.Mocks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;

namespace Boj.Rackspace.UnitTest.Providers
{
    [TestFixture]
    public class RackSpaceTokenProviderTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task GetTokenAsync_ShouldReturnAccessToken_WhenCredentialIsValid()
        { // Arrange
            var httpClient = MockHttpMessageHandler.Create(HttpStatusCode.OK, RackSpaceTokenFixture.SuccessResponse);
            var provider = CreateProvider(httpClient);
            // Act
            var token = await provider.GetTokenAsync();

            token.Should().NotBeNullOrWhiteSpace();
            token.Should().Be("fake-access-token");
        }
        [Test]
        public async Task GetTokenAsync_ShouldThrowException_WhenAuthenticationFails()
        { // Arrange
            var httpClient = MockHttpMessageHandler.Create(HttpStatusCode.Unauthorized, RackSpaceTokenFixture.UnauthorizedResponse);
            var provider = CreateProvider(httpClient);
            // Act

            Func<Task> act = async () => await provider.GetTokenAsync();
            // Assert

            await act.Should().ThrowAsync<HttpRequestException>();
        }
        [Test]
        public async Task GetTokenAsync_ShouldThrowTaskCanceledException_WhenTimeoutOccurs()
        {
            // Arrange

            var handler = new MockHttpMessageHandler(
                async (_, cancellationToken) =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(RackSpaceTokenFixture.SuccessResponse)
                    };
                });
            var httpClient = new HttpClient(handler)
            { Timeout = TimeSpan.FromMilliseconds(100) };
            var provider = CreateProvider(httpClient);
            // Act

            Func<Task> act = async () => await provider.GetTokenAsync();
            // Assert

            await act.Should().ThrowAsync<TaskCanceledException>();
        }
        private static RackSpaceTokenProvider CreateProvider(HttpClient httpClient)
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var options = Options.Create(new RackSpaceTokenOptions
            {
                Username = "demo-user",
                ApiKey = "demo-api-key",
                AuthEndpoint = "https://identity.api.rackspacecloud.com/v2.0/tokens"
            });
            var loggerMock = new Mock<ILogger<RackSpaceTokenProvider>>();

            ILogger<RackSpaceTokenProvider> logger = new Logger<RackSpaceTokenProvider>(new LoggerFactory());
            return new RackSpaceTokenProvider(httpClient, cache, options, logger);
        }
    }
}
