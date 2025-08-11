namespace TYT.Models;

public class AuditWeb
{
    public int Id { get; set; }
    public string? JsonData { get; set; }
    public DateTime? LastUpdate { get; set; }
    public DateTime? AuditStartDate { get; set; }
    public DateTime? AuditEndDate { get; set; }
    public string? IpAddress { get; set; }
    public string? Method { get; set; }
    public string? Endpoint { get; set; }
    public string? UserAgent { get; set; }
    public string? UserName { get; set; }
    public int? StatusCode { get; set; }
}
