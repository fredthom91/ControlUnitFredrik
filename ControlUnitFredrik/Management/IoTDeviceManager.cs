using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClassLibraryFredrik.DataModels;
using ControlUnitFredrik.Services;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ControlUnitFredrik.Management;

public class IoTDeviceManager
{
    public readonly StoreInDataBase storeInDataBase;

    public IoTDeviceManager()
    {
        storeInDataBase = new StoreInDataBase();
    }

    public async Task RegisterDeviceTwinUpdateHandlerMethod(List<LampData> lampData, DeviceClient deviceClient)
    {
        var twinProperties = new TwinCollection();
        var sensorDataJson = JsonConvert.SerializeObject(lampData);
        twinProperties["lastMessage"] = sensorDataJson;
        twinProperties["operationalStatus"] = "Running";
        await deviceClient.UpdateReportedPropertiesAsync(twinProperties);


        await deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChangedMethod, null);
    }

    public async Task OnDesiredPropertyChangedMethod(TwinCollection desiredProperties, object userContext)
    {
        if (desiredProperties.Contains("operationalStatus"))
        {
            var operationalStatus = desiredProperties["operationalStatus"].ToString();
        }
    }

    public async Task CheckDeviceRegistrationButton_ClickMethod(object sender, RoutedEventArgs e,
        bool IsDeviceRegistered, Button buttonCheckRegistration, string hiddenId, string hiddenConnection)
    {
        var isDeviceRegistered =
            await APIServices.CheckDeviceRegistrationAndRegisterIfNecessary(hiddenId, hiddenConnection);

        if (isDeviceRegistered)
        {
            IsDeviceRegistered = true;
            buttonCheckRegistration.Background = Brushes.Green;
            buttonCheckRegistration.Content = "Registrerad";

            await storeInDataBase.UpdateDeviceRegistrationStatusAsync("MyIOTDevice", "Registrerad");
        }
        else
        {
            IsDeviceRegistered = false;
            buttonCheckRegistration.Background = Brushes.Red;
            buttonCheckRegistration.Content = "Ej Registrerad";

            await storeInDataBase.UpdateDeviceRegistrationStatusAsync("MyIOTDevice", "Ej Registrerad");
        }
    }


    public async Task<MethodResponse> SetDataSendingStatusAsyncMethod(MethodRequest methodRequest, object userContext)
    {
        try
        {
            var payload = Encoding.UTF8.GetString(methodRequest.Data);


            var jsonObject = JObject.Parse(payload);


            if (jsonObject.TryGetValue("enableDataSending", out var enableDataSendingToken) &&
                enableDataSendingToken.Type == JTokenType.Boolean)
            {
                var enableDataSending =
                    enableDataSendingToken.Value<bool>();

                return new MethodResponse(
                    Encoding.UTF8.GetBytes(
                        $"{{ \"message\": \"Dataskickning är nu {(enableDataSending ? "aktiverad" : "inaktiverad")}\" }}"),
                    200);
            }


            return new MethodResponse(Encoding.UTF8.GetBytes("{\"error\": \"Ogiltigt värde för enableDataSending.\"}"),
                400);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fel i SetDataSendingStatusAsync: {ex.Message}");

            return new MethodResponse(Encoding.UTF8.GetBytes($"{{ \"error\": \"{ex.Message}\" }}"), 500);
        }
    }

    public async Task<MethodResponse> SetDataSendingIntervalAsyncMethod(MethodRequest methodRequest,
        object userContext, Timer timer, int timerInterval)
    {
        try
        {
            var payload = Encoding.UTF8.GetString(methodRequest.Data);


            var jsonObject = JObject.Parse(payload);


            if (jsonObject.TryGetValue("interval", out var intervalToken) && intervalToken.Type == JTokenType.Integer)
            {
                var newInterval = intervalToken.Value<int>();
                UpdateDataSendingIntervalMethod(newInterval, timer, timerInterval);

                return new MethodResponse(
                    Encoding.UTF8.GetBytes(
                        $"{{ \"message\": \"Skickningsintervall ändrat till {newInterval} sekunder\" }}"), 200);
            }


            return new MethodResponse(Encoding.UTF8.GetBytes("{\"error\": \"Ogiltigt intervalvärde.\"}"), 400);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fel i SetDataSendingIntervalAsync: {ex.Message}");

            return new MethodResponse(Encoding.UTF8.GetBytes($"{{ \"error\": \"{ex.Message}\" }}"), 500);
        }
    }

    public void UpdateDataSendingIntervalMethod(int newInterval, Timer timer, int timerInterval)
    {
        if (timer != null) timer.Dispose();


        timerInterval = newInterval;


        timer = new Timer(SendDataCallbackMethod, null, 0, timerInterval * 1000);
    }

    public void SendDataCallbackMethod(object state)
    {
        Console.WriteLine("Skickar data till Azure IoT Hub...");
    }

    public async Task<MethodResponse> TurnOnLampAsyncMethod(MethodRequest methodRequest, object userContext,
        LampManager lampManager, List<LampData> lampDataList, Button buttonLampa1, Button buttonLampa2,
        Button buttonLampa3, string lampName, ListBox lampStatusListBox, MessageManager messageManager,
        DeviceClient deviceClient)
    {
        try
        {
            var lamp = lampDataList.FirstOrDefault(l => l.LampName == lampName);

            if (lamp != null)
            {
                if (!lamp.IsOn)
                {
                    lamp.Id = Guid.NewGuid().ToString();
                    lamp.IsOn = true;
                    lamp.Message = $"{lamp.LampName} tändes.";
                    lamp.LampName = lampName;
                    lamp.Timestamp = DateTime.Now;


                    await messageManager.SendDataToHubAsyncMethod(lamp, deviceClient);


                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        lampManager.UpdateListBoxMethod(lampStatusListBox, lampDataList);
                    });


                    var button = lampManager.GetButtonForLamp(lamp.LampName, buttonLampa1, buttonLampa2, buttonLampa3);
                    if (button != null) lampManager.RemoteUpdateButtonAppearanceMethod(button, true);

                    return new MethodResponse(Encoding.UTF8.GetBytes($"{{ \"message\": \"{lampName} är nu tänd.\" }}"),
                        200);
                }

                return new MethodResponse(Encoding.UTF8.GetBytes($"{{ \"message\": \"{lampName} är redan tänd.\" }}"),
                    200);
            }

            return new MethodResponse(
                Encoding.UTF8.GetBytes($"{{ \"error\": \"Lampan med namnet '{lampName}' hittades inte.\" }}"), 404);
        }
        catch (Exception ex)
        {
            return new MethodResponse(Encoding.UTF8.GetBytes($"{{ \"error\": \"{ex.Message}\" }}"), 500);
        }
    }

    public async Task<MethodResponse> TurnOffLampAsyncMethod(MethodRequest methodRequest, object userContext,
        LampManager lampManager, List<LampData> lampDataList, Button buttonLampa1, Button buttonLampa2,
        Button buttonLampa3, string lampName, ListBox lampStatusListBox, MessageManager messageManager,
        DeviceClient deviceClient)
    {
        try
        {
            var lamp = lampDataList.FirstOrDefault(l => l.LampName == lampName);

            if (lamp != null)
            {
                if (lamp.IsOn)
                {
                    lamp.Id = Guid.NewGuid().ToString();
                    lamp.IsOn = false;
                    lamp.Message = $"{lamp.LampName} släcktes.";
                    lamp.LampName = lampName;
                    lamp.Timestamp = DateTime.Now;


                    await messageManager.SendDataToHubAsyncMethod(lamp, deviceClient);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        lampManager.UpdateListBoxMethod(lampStatusListBox, lampDataList);
                    });


                    var button = lampManager.GetButtonForLamp(lamp.LampName, buttonLampa1, buttonLampa2, buttonLampa3);
                    if (button != null) lampManager.RemoteUpdateButtonAppearanceMethod(button, false);

                    return new MethodResponse(
                        Encoding.UTF8.GetBytes($"{{ \"message\": \"{lampName} är nu släckt.\" }}"), 200);
                }

                return new MethodResponse(Encoding.UTF8.GetBytes($"{{ \"message\": \"{lampName} är redan släckt.\" }}"),
                    200);
            }

            return new MethodResponse(
                Encoding.UTF8.GetBytes($"{{ \"error\": \"Lampan med namnet '{lampName}' hittades inte.\" }}"), 404);
        }
        catch (Exception ex)
        {
            return new MethodResponse(Encoding.UTF8.GetBytes($"{{ \"error\": \"{ex.Message}\" }}"), 500);
        }
    }


    public async Task<MethodResponse> TurnOnAllIfAnyOffAsyncMethod(MethodRequest methodRequest, object userContext,
        List<LampData> lampDataList, LampManager lampManager, ListBox lampStatusListBox, Button buttonLampa1,
        Button buttonLampa2, Button buttonLampa3, RegistryManager registryManager, MessageManager messageManager,
        DeviceClient deviceClient)
    {
        try
        {
            var alreadyOffLamps = new List<string>();

            foreach (var lamp in lampDataList)
                if (!lamp.IsOn)
                {
                    alreadyOffLamps.Add(lamp.LampName);
                    lamp.IsOn = true;
                }


            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                lampManager.UpdateListBoxMethod(lampStatusListBox, lampDataList);

                foreach (var button in lampManager.GetLampButtonsMethod(buttonLampa1, buttonLampa2, buttonLampa3))
                    lampManager.RemoteUpdateButtonAppearanceMethod(button, true);
            });

            var updateTasks = new List<Task>();

            foreach (var lampName in alreadyOffLamps)
            {
                var onLampData = new LampData
                {
                    Id = Guid.NewGuid().ToString(),
                    Message = $"{lampName}: Lampan tändes, via remote",
                    LampName = lampName,
                    IsOn = true,
                    Timestamp = DateTime.Now
                };


                await messageManager.SendDataToHubAsyncMethod(onLampData, deviceClient);


                var updateTask = UpdateDeviceTwinDesiredPropertiesAsyncMethod(lampName, true, registryManager);
                updateTasks.Add(updateTask);
            }

            await Task.WhenAll(updateTasks);

            if (alreadyOffLamps.Any())
                return new MethodResponse(
                    Encoding.UTF8.GetBytes(
                        $"{{ \"message\": \"Följande lampor var släckta: {string.Join(", ", alreadyOffLamps)}\" }}"),
                    200);

            return new MethodResponse(Encoding.UTF8.GetBytes("{\"message\": \"Alla lampor är tända.\"}"), 200);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fel i TurnOnAllIfAnyOffAsync: {ex.Message}");

            return new MethodResponse(Encoding.UTF8.GetBytes($"{{ \"error\": \"{ex.Message}\" }}"), 500);
        }
    }

    public async Task<MethodResponse> TurnOffAllIfAnyOnAsyncMethod(MethodRequest methodRequest, object userContext,
        List<LampData> lampDataList, LampManager lampManager, ListBox lampStatusListBox, Button buttonLampa1,
        Button buttonLampa2, Button buttonLampa3, RegistryManager registryManager, MessageManager messageManager,
        DeviceClient deviceClient)
    {
        try
        {
            var alreadyOffLamps = new List<string>();

            foreach (var lamp in lampDataList)
                if (lamp.IsOn)
                {
                    alreadyOffLamps.Add(lamp.LampName);
                    lamp.IsOn = false;
                }


            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                lampManager.UpdateListBoxMethod(lampStatusListBox, lampDataList);

                foreach (var button in lampManager.GetLampButtonsMethod(buttonLampa1, buttonLampa2, buttonLampa3))
                    lampManager.RemoteUpdateButtonAppearanceMethod(button, false);
            });

            var updateTasks = new List<Task>();

            foreach (var lampName in alreadyOffLamps)
            {
                var offLampData = new LampData
                {
                    Id = Guid.NewGuid().ToString(),
                    Message = $"{lampName}: Lampan släcktes, via remote",
                    LampName = lampName,
                    IsOn = false,
                    Timestamp = DateTime.Now
                };


                await messageManager.SendDataToHubAsyncMethod(offLampData, deviceClient);


                var updateTask = UpdateDeviceTwinDesiredPropertiesAsyncMethod(lampName, false, registryManager);
                updateTasks.Add(updateTask);
            }

            await Task.WhenAll(updateTasks);

            if (alreadyOffLamps.Any())
                return new MethodResponse(
                    Encoding.UTF8.GetBytes(
                        $"{{ \"message\": \"Följande lampor var tända: {string.Join(", ", alreadyOffLamps)}\" }}"),
                    200);

            return new MethodResponse(Encoding.UTF8.GetBytes("{\"message\": \"Alla lampor är släckta.\"}"), 200);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fel i TurnOffAllIfAnyOnAsync: {ex.Message}");

            return new MethodResponse(Encoding.UTF8.GetBytes($"{{ \"error\": \"{ex.Message}\" }}"), 500);
        }
    }


    public TwinCollection GetDeviceTwinProperties(string lampName, bool isOn)
    {
        var reportedProperties = new TwinCollection();
        reportedProperties["LampName"] = lampName;
        reportedProperties["IsOn"] = isOn;

        return reportedProperties;
    }

    public async Task UpdateDeviceTwinDesiredPropertiesAsyncMethod(string lampName,
        bool isOn, RegistryManager registryManager)
    {
        var buttonColor = isOn ? "white" : "black";


        var desiredProperties = new TwinCollection();
        desiredProperties[$"ButtonColor-{lampName}"] = buttonColor;


        var twin = await registryManager.GetTwinAsync("MyIOTDevice");
        twin.Properties.Desired = desiredProperties;


        await registryManager.UpdateTwinAsync("MyIOTDevice", twin, twin.ETag);
    }

    public async Task UpdateDeviceTwinAsyncMethod(string lampName, bool isOn, List<LampData> lampDataList,
        DeviceClient deviceClient)
    {
        var twinProperties = new TwinCollection();


        twinProperties["operationalStatus"] = GetOperationalStatusMethod(lampName, lampDataList);


        var sensorDataJson = JsonConvert.SerializeObject(lampDataList);
        twinProperties["lastMessage"] = sensorDataJson;


        await deviceClient.UpdateReportedPropertiesAsync(twinProperties);
    }

    public async Task UpdateOperationalStatusAsyncMethod(string status, DeviceClient deviceClient)
    {
        var twinProperties = new TwinCollection();
        twinProperties["operationalStatus"] = status;

        await deviceClient.UpdateReportedPropertiesAsync(twinProperties);
    }

    public string GetOperationalStatusMethod(string lampName, List<LampData> lampDataList)
    {
        var allLampsOn = lampDataList.All(lamp => lamp.IsOn);
        var allLampsOff = lampDataList.All(lamp => !lamp.IsOn);

        if (allLampsOn)
            return "Running";
        if (allLampsOff)
            return "Stopped";
        return
            "Partial";
    }
}