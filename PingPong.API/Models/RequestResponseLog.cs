using System.ComponentModel.DataAnnotations;

namespace PingPong.Models;

public class RequestResponseLog 
{
    [Key]
    public Guid CorrelatedId { get; set; }
    public string RequestPayload { get; set; }
    public int ResponseCode { get; set; }
}