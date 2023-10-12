using System.ComponentModel.DataAnnotations;

namespace ClassLibraryFredrik.DataModels;

public class LampData
{
    [Key] public string Id { get; set; }

    public bool IsOn { get; set; }
    public string? LampName { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; }
}