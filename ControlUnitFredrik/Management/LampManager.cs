using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClassLibraryFredrik.DataModels;
using ControlUnitFredrik.Data;
using ControlUnitFredrik.Extensions;
using ControlUnitFredrik.Services;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;

namespace ControlUnitFredrik.Management;

public class LampManager
{
    public async Task ToggleLamp_ClickMethod(object sender, RoutedEventArgs e,
        List<LampData> lampDataList, ListBox lampStatusListBox, DeviceClient deviceClient,
        bool buttonClicked, IoTDeviceManager iotDeviceManager, MessageManager messageManager)
    {
        var button = sender as Button;
        var lampName = button.Tag as string;

        var lamp = lampDataList.FirstOrDefault(data => data.LampName == lampName);

        if (lamp != null)
        {
            lamp.IsOn = !lamp.IsOn;

            UpdateListBoxMethod(lampStatusListBox, lampDataList);
            UpdateButtonAppearanceMethod(button, lamp.IsOn, buttonClicked);

            await deviceClient.UpdateReportedPropertiesAsync(
                iotDeviceManager.GetDeviceTwinProperties(lampName, lamp.IsOn));


            var lampStatus = lamp.IsOn ? "tändes" : "släcktes";


            var lampData = new LampData
            {
                Id = Guid.NewGuid().ToString(),
                Message = $"{lampName}: Lampan {lampStatus}",
                LampName = lampName,
                IsOn = lamp.IsOn,
                Timestamp = DateTime.Now
            };

            await messageManager.SendDataToHubAsyncMethod(lampData, deviceClient);
        }
    }

    public async Task TurnAllOff_ClickMethod(object sender, RoutedEventArgs e,
        IoTDeviceManager iotDeviceManager, ListBox lampStatusListBox,
        Button buttonLampa1, Button buttonLampa2, Button buttonLampa3,
        List<LampData> lampDataList, DeviceClient deviceClient, StoreInDataBase storeInDataBase,
        MessageManager messageManager)
    {
        var turnedOffLamps = new List<string>();

        foreach (var lamp in lampDataList)
            if (lamp.IsOn)
            {
                lamp.IsOn = false;
                turnedOffLamps.Add(lamp.LampName);
            }

        UpdateListBoxMethod(lampStatusListBox, lampDataList);

        foreach (var button in GetLampButtonsMethod(buttonLampa1, buttonLampa2, buttonLampa3))
            UpdateButtonAppearanceMethod(button, false, false);

        foreach (var lampName in turnedOffLamps)
        {
            await iotDeviceManager.UpdateDeviceTwinAsyncMethod(lampName, false, lampDataList, deviceClient);
            var lampData = new LampData
            {
                Id = Guid.NewGuid().ToString(),
                Message = $"{lampName}: Lampan släcktes, via huvudbrytare",
                LampName = lampName,
                IsOn = false,
                Timestamp = DateTime.Now
            };
            await messageManager.SendDataToHubAsyncMethod(lampData, deviceClient);
        }
    }

    public async Task TurnAllOn_ClickMethod(object sender, RoutedEventArgs e,
        IoTDeviceManager iotDeviceManager, ListBox lampStatusListBox,
        Button buttonLampa1, Button buttonLampa2, Button buttonLampa3,
        List<LampData> lampDataList, DeviceClient deviceClient, StoreInDataBase storeInDataBase,
        MessageManager messageManager)
    {
        var turnedOnLamps = new List<string>();

        foreach (var lamp in lampDataList)
            if (!lamp.IsOn)
            {
                lamp.IsOn = true;
                turnedOnLamps.Add(lamp.LampName);
            }

        UpdateListBoxMethod(lampStatusListBox, lampDataList);

        foreach (var button in GetLampButtonsMethod(buttonLampa1, buttonLampa2, buttonLampa3))
            UpdateButtonAppearanceMethod(button, true, true);

        foreach (var lampName in turnedOnLamps)
        {
            await iotDeviceManager.UpdateDeviceTwinAsyncMethod(lampName, true, lampDataList, deviceClient);


            var lampData = new LampData
            {
                Id = Guid.NewGuid().ToString(),
                Message = $"{lampName}: Lampan tändes, via huvudbrytare",
                LampName = lampName,
                IsOn = true,
                Timestamp = DateTime.Now
            };
            await messageManager.SendDataToHubAsyncMethod(lampData, deviceClient);
        }
    }

    public Button GetButtonForLamp(string lampName, Button buttonLampa1, Button buttonLampa2, Button buttonLampa3)
    {
        switch (lampName)
        {
            case "Sovrum":
                return buttonLampa1;
            case "Kök":
                return buttonLampa2;
            case "Hall":
                return buttonLampa3;
            default:
                return null;
        }
    }

    public void ToggleAllLampsMethod(bool isOn, IoTDeviceManager iotDeviceManager, ListBox lampSatusListBox,
        Button buttonLampa1, Button buttonLampa2, Button buttonLampa3,
        List<LampData> lampDataList, DeviceClient deviceClient)
    {
        foreach (var lamp in lampDataList) lamp.IsOn = isOn;


        iotDeviceManager.UpdateDeviceTwinAsyncMethod("All", isOn, lampDataList, deviceClient);


        Application.Current.Dispatcher.Invoke(() =>
        {
            UpdateListBoxMethod(lampSatusListBox, lampDataList);

            foreach (var button in GetLampButtonsMethod(buttonLampa1, buttonLampa2, buttonLampa3))
                RemoteUpdateButtonAppearanceMethod(button, isOn);
        });
    }

    public IEnumerable<Button> GetLampButtonsMethod(Button buttonLampa1, Button buttonLampa2, Button buttonLampa3)
    {
        return buttonLampa1.GetVisualAncestors().OfType<Button>()
            .Concat(buttonLampa2.GetVisualAncestors().OfType<Button>())
            .Concat(buttonLampa3.GetVisualAncestors().OfType<Button>());
    }

    public void RemoteUpdateButtonAppearanceMethod(Button button, bool isOn)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (isOn)
                {
                    button.Foreground = Brushes.Black;
                    button.Background = Brushes.PaleGreen;
                }
                else
                {
                    button.Foreground = Brushes.Black;
                    button.Background = Brushes.IndianRed;
                }

                button.Opacity = 0.8;
            });
        });
    }

    public void UpdateButtonAppearanceMethod(Button button, bool isOn, bool buttonClicked)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (isOn)
            {
                button.Foreground = Brushes.Black;
                button.Background = Brushes.PaleGreen;
            }
            else
            {
                button.Foreground = Brushes.Black;
                button.Background = buttonClicked ? Brushes.PaleGreen : Brushes.IndianRed;
            }

            button.Opacity = 0.8;
        });
    }


    public async Task UpdateListBoxDeviceMethod(RegistryManager registryManager,
        ObservableCollection<DeviceInfo> deviceInfo)
    {
        await Application.Current.Dispatcher.Invoke(async () =>
        {
            try
            {
                var device = await registryManager.GetDeviceAsync("MyIOTDevice");


                if (device != null)
                    deviceInfo.Add(new DeviceInfo
                    {
                        DeviceId = $"Device: {device.Id}",
                        Status = $"Status: {device.Status.ToString()}"
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        });
    }

    public void UpdateListBoxMethod(ListBox lampStatusListBox, List<LampData> lampDataList)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            lampStatusListBox.Items.Clear();


            foreach (var data in lampDataList)
            {
                var listBoxItem = new ListBoxItem();
                listBoxItem.Content = $"{data.LampName} - {(data.IsOn ? "Status: PÅ" : "Status: AV")}";
                listBoxItem.Background = data.IsOn ? Brushes.PaleGreen : Brushes.Black;
                listBoxItem.Foreground = data.IsOn ? Brushes.PaleGreen : Brushes.Black;
                listBoxItem.BorderBrush = data.IsOn ? Brushes.PaleGreen : Brushes.IndianRed;
                listBoxItem.FontWeight = FontWeights.Bold;

                lampStatusListBox.Items.Add(listBoxItem);
            }
        });
    }
}