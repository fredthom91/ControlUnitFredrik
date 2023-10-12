using System;

namespace IOTControlUnit.Data;

public class LampMessage
{
    public string? LampName { get; set; }
    public string? Status { get; set; }
    public DateTime Timestamp { get; set; }
}