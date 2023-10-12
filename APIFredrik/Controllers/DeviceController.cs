using APIFredrik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Newtonsoft.Json;

namespace APIFredrik.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DeviceController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly string iotHubConnectionString;
    private readonly RegistryManager registryManager;

    public DeviceController(IConfiguration configuration)
    {
        _configuration = configuration;
        iotHubConnectionString =
            "HostName=IoTHubFredrik.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=n28KmYyKu+hZuReQT3FKNfRS6+9UV4vyzAIoTCK3UEQ=";
        registryManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
    }

    [HttpGet("{deviceId}")]
    public async Task<IActionResult> CheckRegistration(string deviceId)
    {
        try
        {
            var device = await registryManager.GetDeviceAsync(deviceId);

            if (device != null)
                return Ok($"Enhetsstatus för {deviceId}: Registrerad i Azure IoT Hub");
            return NotFound($"Enhetsstatus för {deviceId}: Ej registrerad i Azure IoT Hub");
        }
        catch (DeviceNotFoundException)
        {
            return NotFound($"Enhetsstatus för {deviceId}: Ej registrerad i Azure IoT Hub");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ett fel inträffade: {ex.Message}");
        }
    }

    [HttpPost("{deviceId}")]
    public async Task<IActionResult> RegisterDevice(string deviceId)
    {
        try
        {
            var existingDevice = await registryManager.GetDeviceAsync(deviceId);

            if (existingDevice != null) return Conflict($"Enhetsregistrering för {deviceId} finns redan.");

            var newDevice = new Device(deviceId)
            {
                Authentication = new AuthenticationMechanism
                {
                    Type = AuthenticationType.Sas,
                    SymmetricKey = new SymmetricKey
                    {
                        PrimaryKey =
                            "x7htAceMuWp0TmEQJseW06pxIQbhQcHICmCByIgBHo4=",
                        SecondaryKey = 
                            "vSsckpH71sVK9dNajnhy1DFieh/CI2EVrPBQwYEXMTM="
                    }
                }
            };

            await registryManager.AddDeviceAsync(newDevice);

            SaveConnectionInfoLocally(newDevice);

            return Ok($"Enhetsregistrering för {deviceId} lyckades.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ett fel inträffade: {ex.Message}");
        }
    }

    private void SaveConnectionInfoLocally(Device device)
    {
        var connectionInfo = new DeviceConnectionInfo
        {
            DeviceId = device.Id,
            PrimaryKey = device.Authentication.SymmetricKey.PrimaryKey,
            SecondaryKey = device.Authentication.SymmetricKey.SecondaryKey
        };


        var filePath = "connectionInfo.json";

        try
        {
            using (var writer = new StreamWriter(filePath))
            {
                var json = JsonConvert.SerializeObject(connectionInfo);
                writer.Write(json);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ett fel inträffade: {ex.Message}");
        }
    }
}