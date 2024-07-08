using Microsoft.AspNetCore.Mvc;
using PingPong.Models;

namespace PingPong.Controllers;

[ApiController]
[Route("[controller]")]
public class PingController : ControllerBase
{
    private readonly Random _random = new Random();
    private static int _requestCount = 0;
    private readonly RequestResponseContext _context;

    public PingController(RequestResponseContext context)
    {
        _context = context;
    }

    [HttpPost("/ping")]
    public async Task<IActionResult> Post([FromBody] PingRequest request)
    {
        var responseCode = (_requestCount % _random.Next(1, 10) == 0) ? 500 : 200;
        _requestCount++;

        var pingResponse = new PingResponse
        {
            CorrelatedId = request.CorrelatedId,
            Response = responseCode == 200 ? "pong" : "error"
        };

        var record = new RequestResponseLog
        {
            CorrelatedId = request.CorrelatedId,
            RequestPayload = request.Request,
            ResponseCode = responseCode
        };

        _context.RequestResponseLogs.Add(record);
        await _context.SaveChangesAsync();
        
        if (responseCode == 200)
            return Ok(pingResponse);

        return StatusCode(500, pingResponse);
    }
}

public class PingRequest
{
    public Guid CorrelatedId { get; set; }
    public string Request { get; set; }
}

public class PingResponse
{
    public Guid CorrelatedId { get; set; }
    public string Response { get; set; }
}