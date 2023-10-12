using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ClassLibraryFredrik.DataModels;
using ControlUnitFredrik.Data;
using ControlUnitFredrik.Management;
using ControlUnitFredrik.Services;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Message = Microsoft.Azure.Devices.Message;

namespace ControlUnitFredrik;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const string DeviceId = "MyIOTDevice";
    private const string ApiBaseUrl = "https://fredrikiotapi.azurewebsites.net/api/Device/";
    private readonly RegistryManager checkDeviveRegManager;

    private readonly IConfiguration configuration;
    private readonly string connectionString;
    private readonly string connectionStringHub;


    private readonly int dataSendingIntervalInSeconds = 60;
    private readonly Timer? dataSendingTimer;
    private readonly DeviceClient deviceClient;
    private readonly string hiddenConnection;
    private readonly string hiddenId;
    private readonly IoTDeviceManager iotDeviceManager;
    private readonly List<LampData> lampDataList = new();

    private readonly LampManager lampManager;
    private readonly MessageManager messageManager;
    private readonly RegistryManager registryManager;
    private readonly StoreInDataBase storeInDataBase;
    private readonly bool buttonClicked = false;
    private ServiceClient serviceClient;
    private readonly DispatcherTimer timer;
    private SemaphoreSlim twinUpdateSemaphore = new(1, 1);
    private readonly DispatcherTimer weatherUpdateTimer;


    public MainWindow()
    {
        InitializeComponent();


        configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettingsiot.json")
            .Build();


        connectionString = configuration.GetSection("AppSettings")["ConnectionString"];
        connectionStringHub = configuration.GetSection("AppSettings")["ConnectionStringHub"];
        hiddenId = configuration.GetSection("ConnectionInfo")["DeviceId"];
        hiddenConnection = configuration.GetSection("ConnectionInfo")["DeviceConnectionString"];

        deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
        serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
        registryManager = RegistryManager.CreateFromConnectionString(connectionString);
        checkDeviveRegManager = RegistryManager.CreateFromConnectionString(connectionStringHub);
        iotDeviceManager = new IoTDeviceManager();
        messageManager = new MessageManager();
        lampManager = new LampManager();
        storeInDataBase = new StoreInDataBase();


        lampDataList.Add(new LampData { LampName = "Sovrum", IsOn = false });
        lampDataList.Add(new LampData { LampName = "Kök", IsOn = false });
        lampDataList.Add(new LampData { LampName = "Hall", IsOn = false });


        timer = new DispatcherTimer();
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += Timer_Tick;
        timer.Start();

        weatherUpdateTimer = new DispatcherTimer();
        weatherUpdateTimer.Interval = TimeSpan.FromMinutes(1); // Uppdatera vädret var 5:e minut
        weatherUpdateTimer.Tick += WeatherUpdateTimer_Tick;
        weatherUpdateTimer.Start();

        UpdateDate();
        UpdateWeather();

        RegisterDeviceTwinUpdateHandler();


        RegisterDirectMethodHandlers();


        UpdateListBox();

        UpdateListBoxDevice();

        ShowWeatherInformation();

        deviceListBox.ItemsSource = DeviceInfo;
    }

    private ObservableCollection<DeviceInfo> DeviceInfo { get; } = new();

    private bool IsDeviceRegistered { get; }

    private void RegisterDeviceTwinUpdateHandler()
    {
        iotDeviceManager.RegisterDeviceTwinUpdateHandlerMethod(lampDataList, deviceClient);
    }

    private async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
    {
        await iotDeviceManager.OnDesiredPropertyChangedMethod(desiredProperties, userContext);
    }

    private void RegisterDirectMethodHandlers()
    {
        deviceClient.SetMethodHandlerAsync("TurnAllOn", TurnOnAllIfAnyOffAsync, null);


        deviceClient.SetMethodHandlerAsync("TurnAllOff", TurnOffAllIfAnyOnAsync, null);


        deviceClient.SetMethodHandlerAsync("DataSendingStatus", SetDataSendingStatusAsync, null);


        deviceClient.SetMethodHandlerAsync("DataSendingInterval", SetDataSendingIntervalAsync, null);

        deviceClient.SetMethodHandlerAsync("TurnOnBedroom", TurnOnBedroomLampAsync, null);

        deviceClient.SetMethodHandlerAsync("TurnOffBedroom", TurnOffBedroomLampAsync, null);

        deviceClient.SetMethodHandlerAsync("TurnOnKitchen", TurnOnKitchenLampAsync, null);

        deviceClient.SetMethodHandlerAsync("TurnOffKitchen", TurnOffKitchenLampAsync, null);

        deviceClient.SetMethodHandlerAsync("TurnOnHall", TurnOnHallLampAsync, null);

        deviceClient.SetMethodHandlerAsync("TurnOffHall", TurnOffHallLampAsync, null);
    }

    private async Task<MethodResponse> SetDataSendingStatusAsync(MethodRequest methodRequest, object userContext)
    {
        var response = await iotDeviceManager.SetDataSendingStatusAsyncMethod(methodRequest, userContext);


        return response;
    }

    public void CheckDeviceRegistrationButton_Click(object sender, RoutedEventArgs e)
    {
        _ = HandleCheckDeviceRegistrationAsync(sender, e);
    }

    public async Task HandleCheckDeviceRegistrationAsync(object sender, RoutedEventArgs e)
    {
        await SendDeviceDataToHubAsync($"Kollade Registreringen av IoT-Enheten. Datum: {DateTime.Now}");

        await iotDeviceManager.CheckDeviceRegistrationButton_ClickMethod(sender, e, IsDeviceRegistered,
            buttonCheckRegistration, hiddenId, hiddenConnection);
    }

    private async Task<MethodResponse> TurnOnBedroomLampAsync(MethodRequest methodRequest, object userContext)
    {
        var respons = await iotDeviceManager.TurnOnLampAsyncMethod(methodRequest, userContext, lampManager,
            lampDataList,
            buttonLampa1, buttonLampa2, buttonLampa3, "Sovrum", lampStatusListBox, messageManager, deviceClient);

        return respons;
    }

    private async Task<MethodResponse> TurnOnKitchenLampAsync(MethodRequest methodRequest, object userContext)
    {
        var respons = await iotDeviceManager.TurnOnLampAsyncMethod(methodRequest, userContext, lampManager,
            lampDataList,
            buttonLampa1, buttonLampa2, buttonLampa3, "Kök", lampStatusListBox, messageManager, deviceClient);

        return respons;
    }

    private async Task<MethodResponse> TurnOnHallLampAsync(MethodRequest methodRequest, object userContext)
    {
        var respons = await iotDeviceManager.TurnOnLampAsyncMethod(methodRequest, userContext, lampManager,
            lampDataList,
            buttonLampa1, buttonLampa2, buttonLampa3, "Hall", lampStatusListBox, messageManager, deviceClient);

        return respons;
    }


    private async Task<MethodResponse> TurnOffBedroomLampAsync(MethodRequest methodRequest, object userContext)
    {
        var respons = await iotDeviceManager.TurnOffLampAsyncMethod(methodRequest, userContext, lampManager,
            lampDataList,
            buttonLampa1, buttonLampa2, buttonLampa3, "Sovrum", lampStatusListBox, messageManager, deviceClient);

        return respons;
    }

    private async Task<MethodResponse> TurnOffKitchenLampAsync(MethodRequest methodRequest, object userContext)
    {
        var respons = await iotDeviceManager.TurnOffLampAsyncMethod(methodRequest, userContext, lampManager,
            lampDataList,
            buttonLampa1, buttonLampa2, buttonLampa3, "Kök", lampStatusListBox, messageManager, deviceClient);

        return respons;
    }

    private async Task<MethodResponse> TurnOffHallLampAsync(MethodRequest methodRequest, object userContext)
    {
        var respons = await iotDeviceManager.TurnOffLampAsyncMethod(methodRequest, userContext, lampManager,
            lampDataList,
            buttonLampa1, buttonLampa2, buttonLampa3, "Hall", lampStatusListBox, messageManager, deviceClient);

        return respons;
    }

    private async Task ShowWeatherInformation()
    {
        APIServices.ShowWeatherInformationMethod(CityTextBlock, TemperatureTextBlock, WeatherDescriptionTextBlock,
            WeatherIconImage);
    }


    private async Task<MethodResponse> SetDataSendingIntervalAsync(MethodRequest methodRequest, object userContext)
    {
        var response = await iotDeviceManager.SetDataSendingIntervalAsyncMethod(methodRequest, userContext,
            dataSendingTimer, dataSendingIntervalInSeconds);
        return response;
    }

    private void UpdateDataSendingInterval(int newInterval)
    {
        iotDeviceManager.UpdateDataSendingIntervalMethod(newInterval, dataSendingTimer, dataSendingIntervalInSeconds);
    }

    private void SendDataCallback(object state)
    {
        iotDeviceManager.SendDataCallbackMethod(state);
    }


    private async Task<MethodResponse> TurnOnAllIfAnyOffAsync(MethodRequest methodRequest, object userContext)
    {
        var response = await iotDeviceManager.TurnOnAllIfAnyOffAsyncMethod(methodRequest, userContext, lampDataList,
            lampManager, lampStatusListBox, buttonLampa1, buttonLampa2, buttonLampa3, registryManager, messageManager,
            deviceClient);


        return response;
    }

    private async Task<MethodResponse> TurnOffAllIfAnyOnAsync(MethodRequest methodRequest, object userContext)
    {
        var response = await iotDeviceManager.TurnOffAllIfAnyOnAsyncMethod(methodRequest, userContext,
            lampDataList, lampManager, lampStatusListBox, buttonLampa1, buttonLampa2, buttonLampa3, registryManager,
            messageManager, deviceClient);

        return response;
    }

    private async Task UpdateDeviceTwinDesiredPropertiesAsync(string lampName, bool isOn)
    {
        await iotDeviceManager.UpdateDeviceTwinDesiredPropertiesAsyncMethod(lampName, isOn, registryManager);
    }


    private void ToggleLamp_Click(object sender, RoutedEventArgs e)
    {
        lampManager.ToggleLamp_ClickMethod(sender, e, lampDataList, lampStatusListBox,
            deviceClient, buttonClicked, iotDeviceManager, messageManager);
    }

    private string GetOperationalStatus(string lampName)
    {
        return iotDeviceManager.GetOperationalStatusMethod(lampName, lampDataList);
    }

    private async Task SaveLatestMessageToDeviceTwinAsync(string lampName, string latestMessage)
    {
        await messageManager.SaveLatestMessageToDeviceTwinAsyncMethod(lampName, latestMessage, deviceClient);
    }

    private async Task UpdateDeviceTwinAsync(string lampName, bool isOn)
    {
        await iotDeviceManager.UpdateDeviceTwinAsyncMethod(lampName, isOn, lampDataList, deviceClient);
    }

    private void RemoteUpdateButtonAppearance(Button button, bool isOn)
    {
        lampManager.RemoteUpdateButtonAppearanceMethod(button, isOn);
    }

    private void UpdateButtonAppearance(Button button, bool isOn)
    {
        lampManager.UpdateButtonAppearanceMethod(button, isOn, buttonClicked);
    }

    private void UpdateListBox()
    {
        lampManager.UpdateListBoxMethod(lampStatusListBox, lampDataList);
    }

    private void TurnAllOff_Click(object sender, RoutedEventArgs e)
    {
        lampManager.TurnAllOff_ClickMethod(sender, e, iotDeviceManager, lampStatusListBox,
            buttonLampa1, buttonLampa2, buttonLampa3, lampDataList, deviceClient, storeInDataBase, messageManager);
    }

    private void TurnAllOn_Click(object sender, RoutedEventArgs e)
    {
        lampManager.TurnAllOn_ClickMethod(sender, e, iotDeviceManager, lampStatusListBox,
            buttonLampa1, buttonLampa2, buttonLampa3, lampDataList, deviceClient, storeInDataBase, messageManager);
    }

    private IEnumerable<Button> GetLampButtons()
    {
        return lampManager.GetLampButtonsMethod(buttonLampa1, buttonLampa2, buttonLampa3);
    }


    private async Task UpdateListBoxDevice()
    {
        await lampManager.UpdateListBoxDeviceMethod(checkDeviveRegManager, DeviceInfo);
    }


    private async Task UpdateOperationalStatusAsync(string status)
    {
        await iotDeviceManager.UpdateOperationalStatusAsyncMethod(status, deviceClient);
    }

    private async Task ReceiveMessageAsync(Message message)
    {
        await messageManager.ReceiveMessageAsyncMethod(message, lampManager, iotDeviceManager, lampDataList,
            buttonLampa1, buttonLampa2, buttonLampa3, lampStatusListBox, deviceClient);
    }

    private async Task UpdateDeviceTwinMessagesAsync(List<LampData> lampDataList)
    {
        await messageManager.UpdateDeviceTwinMessagesAsyncMethod(lampDataList, deviceClient);
    }

    private void ToggleAllLamps(bool isOn)
    {
        lampManager.ToggleAllLampsMethod(isOn, iotDeviceManager, lampStatusListBox,
            buttonLampa1, buttonLampa2, buttonLampa3, lampDataList, deviceClient);
    }

    private async Task SendDataToHubAsync(LampData data)
    {
        await messageManager.SendDataToHubAsyncMethod(data, deviceClient);
    }

    private async Task SendDeviceDataToHubAsync(string data)
    {
        await messageManager.SendDeviceDataToHubAsyncMethod(data, deviceClient);
    }

    private void UpdateTime()
    {
        timeTextBlock.Text = DateTime.Now.ToString("HH:mm:ss");
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        UpdateTime();
    }

    private void UpdateDate()
    {
        var now = DateTime.Now;
        var swedishCulture = new CultureInfo("sv-SE");
        var formattedDate = now.ToString("dddd d'/'M yyyy", swedishCulture);
        formattedDate = char.ToUpper(formattedDate[0]) + formattedDate.Substring(1);
        dateTextBlock.Text = formattedDate;
    }

    private void WeatherUpdateTimer_Tick(object sender, EventArgs e)
    {
        UpdateWeather();
    }

    private async Task UpdateWeather()
    {
        try
        {
            var weatherData = await GetWeatherData();

            if (!string.IsNullOrEmpty(weatherData))
            {
                var weatherInfo = JsonConvert.DeserializeObject<WeatherInfo>(weatherData);


                UpdateWeatherInfo(weatherInfo);
            }
            else
            {
                CityTextBlock.Text = "Fel vid hämtning av väderdata.";
                TemperatureTextBlock.Text = "";
                WeatherDescriptionTextBlock.Text = "";
                WeatherIconImage.Source = null;
            }
        }
        catch (Exception ex)
        {
            CityTextBlock.Text = "Fel vid uppdatering av väder.";
            TemperatureTextBlock.Text = "";
            WeatherDescriptionTextBlock.Text = "";
            WeatherIconImage.Source = null;
        }
    }

    private async Task<string> GetWeatherData()
    {
        var weatherApiClient = new WeatherApiClient("6ba62436dd00318990437058362d6a82");
        var city = "Ciudad Quesada";

        return await weatherApiClient.GetWeatherData(city);
    }

    private void UpdateWeatherInfo(WeatherInfo weatherInfo)
    {
        CityTextBlock.Text = $"Stad: {weatherInfo.Name}";
        TemperatureTextBlock.Text = $"Temperatur: {weatherInfo.Main.Temp:F1}°C";
        WeatherDescriptionTextBlock.Text = $"Väder: {weatherInfo.Weather[0].Description.ToUpper()}";


        var iconUrl = $"http://openweathermap.org/img/w/{weatherInfo.Weather[0].Icon}.png";
        var iconBitmap = new BitmapImage(new Uri(iconUrl));
        WeatherIconImage.Source = iconBitmap;
    }
}