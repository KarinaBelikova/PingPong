using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace PingClient;

public class Program
{
    static async Task<int> Main(string[] args)
    {
        var numberOfCallsOption = new Option<int>("--numberOfCalls", () => 4, "Number of calls to make");
        var numberOfRetriesOption = new Option<int>("--numberOfRetries", () => 3, "Number of retries for failed requests");
        var deltaRetryTimeOption = new Option<int>("--deltaRetryTime", () => 500, "Time to wait between retries in milliseconds");
        var serviceUrlOption = new Option<string>("--serviceUrl", () => "https://localhost:7031/ping", "URL of the service to call");

        var rootCommand = new RootCommand
        {
            numberOfCallsOption,
            numberOfRetriesOption,
            deltaRetryTimeOption,
            serviceUrlOption
        };

        rootCommand.SetHandler(async (numberOfCalls, numberOfRetries, deltaRetryTime, serviceUrl) =>
        {
            await RunClient(numberOfCalls, numberOfRetries, deltaRetryTime, serviceUrl);
        },
        numberOfCallsOption,
        numberOfRetriesOption,
        deltaRetryTimeOption,
        serviceUrlOption);

        return await rootCommand.InvokeAsync(args);
    }

    public static async Task RunClient(int numberOfCalls, int numberOfRetries, int deltaRetryTime, string serviceUrl)
    {
        // Define the retry policy
        AsyncRetryPolicy<HttpResponseMessage> retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => r.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            .WaitAndRetryAsync(
                numberOfRetries,
                retryAttempt => TimeSpan.FromMilliseconds(deltaRetryTime),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    Console.WriteLine($"Retrying {retryAttempt} time(s) after {timespan.TotalMilliseconds} ms delay due to {outcome.Result.StatusCode}");
                });

        var services = new ServiceCollection();

        services.AddHttpClient<PingClient>(client =>
        {
            client.BaseAddress = new Uri(serviceUrl);
        });

        services.AddLogging(configure =>
        {
            configure.AddConsole();
            configure.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
        });
        
        var serviceProvider = services.BuildServiceProvider();

        var pingClient = serviceProvider.GetRequiredService<PingClient>();

        for (int i = 1; i < numberOfCalls+1; i++)
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                Console.WriteLine($"{i} call");
                return await pingClient.CallService();
            });
        }
    }
}