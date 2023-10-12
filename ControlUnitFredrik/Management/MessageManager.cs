using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using ClassLibraryFredrik.DataModels;
using ControlUnitFredrik.Utilities;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Message = Microsoft.Azure.Devices.Message;

namespace ControlUnitFredrik.Management;

public class MessageManager
{
    public async Task SaveLatestMessageToDeviceTwinAsyncMethod(string lampName, string latestMessage,
        DeviceClient deviceClient)
    {
        var cleanedPropertyName = StringUtilities.CleanPropertyName($"latestMessage-{lampName}");

        var twinProperties = new TwinCollection();
        twinProperties[cleanedPropertyName] = latestMessage;


        var twin = await deviceClient.GetTwinAsync();


        await deviceClient.UpdateReportedPropertiesAsync(twinProperties);
    }

    public async Task ReceiveMessageAsyncMethod(Message message,
        LampManager lampManager, IoTDeviceManager iotDeviceManager, List<LampData> lampDataList,
        Button buttonLampa1, Button buttonLampa2, Button buttonLampa3,
        ListBox lampStatusListBox, DeviceClient deviceClient)
    {
        var messageData = Encoding.ASCII.GetString(message.GetBytes());
        Console.WriteLine($"Mottaget meddelande: {messageData}");


        var actionProperty = message.Properties["Action"];


        if (!string.IsNullOrEmpty(actionProperty))
        {
            if (actionProperty == "TurnOn")
                lampManager.ToggleAllLampsMethod(true, iotDeviceManager, lampStatusListBox,
                    buttonLampa1, buttonLampa2, buttonLampa3, lampDataList, deviceClient);
            else if (actionProperty == "TurnOff")
                lampManager.ToggleAllLampsMethod(false, iotDeviceManager, lampStatusListBox,
                    buttonLampa1, buttonLampa2, buttonLampa3, lampDataList, deviceClient);
            else
                Console.WriteLine($"Okänd åtgärd: {actionProperty}");
        }
        else
        {
            Console.WriteLine("Meddelandet innehåller ingen egenskap 'Action'.");
        }

        var newMessage = new LampData { Message = messageData, Timestamp = DateTime.Now };
        lampDataList.Add(newMessage);


        await UpdateDeviceTwinMessagesAsyncMethod(lampDataList, deviceClient);
    }

    public async Task UpdateDeviceTwinMessagesAsyncMethod(List<LampData> lampDataList, DeviceClient deviceClient)
    {
        try
        {
            var twinProperties = new TwinCollection();
            var sensorDataJson = JsonConvert.SerializeObject(lampDataList);
            twinProperties["messageHistory"] = sensorDataJson;


            var twin = await deviceClient.GetTwinAsync();


            await deviceClient.UpdateReportedPropertiesAsync(twinProperties);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fel i UpdateDeviceTwinMessagesAsync: {ex.Message}");
        }
    }

    public async Task SendDataToHubAsyncMethod(LampData data, DeviceClient deviceClient)
    {
        try
        {
            var messageString = JsonConvert.SerializeObject(data);


            var messageBytes = Encoding.UTF8.GetBytes(messageString);

            var message = new Microsoft.Azure.Devices.Client.Message(messageBytes);

            await deviceClient.SendEventAsync(message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fel i SendDataToHubAsyncMethod: {ex.Message}");
        }
    }

    public async Task SendDeviceDataToHubAsyncMethod(string data, DeviceClient deviceClient)
    {
        var messageBytes = Encoding.UTF8.GetBytes(data);

        var message = new Microsoft.Azure.Devices.Client.Message(messageBytes);

        await deviceClient.SendEventAsync(message);
    }
}