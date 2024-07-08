using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PingClient;

public class PingClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PingClient> _logger;

    public PingClient(HttpClient httpClient, ILogger<PingClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<HttpResponseMessage> CallService()
    {
        var request = new
        {
            correlatedId = Guid.NewGuid().ToString(),
            request = "ping"
        };

        _logger.LogInformation($"Content: {request}");
        var response = await _httpClient.PostAsJsonAsync("", request);

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        var formattedResponseContent = $"{{ correlatedId = {jsonResponse.GetProperty("correlatedId")}, response = {jsonResponse.GetProperty("response")} }}";
        _logger.LogInformation($"Content: {formattedResponseContent}");
        return response;
    }
}