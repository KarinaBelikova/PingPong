using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Polly;
using Polly.Retry;
using Xunit;
using Assert = Xunit.Assert;

namespace PingClient.Tests;

public class PingClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

    public PingClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://localhost:7031/ping")
        };
        new Mock<ILogger<PingClient>>();
    }
    
    [Fact]
    public async Task RunClient_RetriesOnInternalServerError()
    {
        // Arrange
        int numberOfCalls = 3;
        int numberOfRetries = 2;
        int deltaRetryTime = 500;
        string serviceUrl = "https://localhost:7031/ping";
        
        var services = new ServiceCollection();
        var loggerMock = new Mock<ILogger<PingClient>>();

        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var random = new Random();

        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                // Randomly return 500 error or 200 OK response
                if (random.Next(2) == 0)
                { 
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"correlatedId\":\"test-id\",\"response\":\"pong\"}")
                    };
                }
            });

        services.AddHttpClient<PingClient>(client =>
        {
            client.BaseAddress = new Uri(serviceUrl);
        }).ConfigurePrimaryHttpMessageHandler(() => httpMessageHandlerMock.Object);

        services.AddLogging(configure =>
        {
            configure.AddConsole();
            configure.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
        });

        var serviceProvider = services.BuildServiceProvider();
        var pingClient = serviceProvider.GetRequiredService<PingClient>();

        // Act
        await Program.RunClient(numberOfCalls, numberOfRetries, deltaRetryTime, serviceUrl);

        // Assert
        // Each call might need up to (numberOfRetries + 1) requests due to retries
        httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.AtMost(numberOfCalls * (numberOfRetries + 1)),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
            );
    }        
}
