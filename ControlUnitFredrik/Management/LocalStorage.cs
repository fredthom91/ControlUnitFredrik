using System;
using System.IO;
using ControlUnitFredrik.Data;
using Newtonsoft.Json;

namespace ControlUnitFredrik.Management;

public static class LocalStorage
{
    private static readonly string ConfigFilePath = "config.json";

    public static ConnectionInfo LoadConnectionInfo(string hiddenId, string hiddenConnection)
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                return JsonConvert.DeserializeObject<ConnectionInfo>(json);
            }
            else
            {
                var connectionInfo = new ConnectionInfo
                {
                    DeviceId = hiddenId,
                    DeviceConnectionString = hiddenConnection
                };


                var json = JsonConvert.SerializeObject(connectionInfo);
                File.WriteAllText(ConfigFilePath, json);

                return connectionInfo;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading/saving connection info: {ex.Message}");
        }

        return null;
    }

    public static void SaveConnectionInfo(ConnectionInfo connectionInfo)
    {
        try
        {
            var json = JsonConvert.SerializeObject(connectionInfo);
            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving connection info: {ex.Message}");
        }
    }
}