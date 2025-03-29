using System;

public class Order
{
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public double Total { get; set; }
    public string CanalOrigen { get; set; }
}
