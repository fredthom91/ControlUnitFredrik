using System.ComponentModel.DataAnnotations;

namespace ClassLibraryFredrik.DataModels;

public class DeviceRegistration
{
    [Key] public int Id { get; set; }

    public string DeviceName { get; set; }

    public string Status { get; set; }
}