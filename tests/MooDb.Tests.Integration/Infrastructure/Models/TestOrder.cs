namespace MooDb.Tests.Integration.Infrastructure.Models;

public sealed class TestOrder
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime CreatedUtc { get; set; }
}