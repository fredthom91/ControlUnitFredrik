using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ControlUnitFredrik.Data;
using ControlUnitFredrik.Management;
using Newtonsoft.Json;

namespace ControlUnitFredrik.Services;

public class APIServices
{
    private const string DeviceId = "MyIOTDevice";
    private const string ApiBaseUrl = "https://fredrikiotapi.azurewebsites.net/api/Device/";

    public static async Task<bool> CheckDeviceRegistrationAndRegisterIfNecessary(string hiddenId, string hiddenConnection)
    {
        try
        {
            var isDeviceRegistered = await IsDeviceRegisteredAsync(DeviceId);

            if (!isDeviceRegistered)
            {
                var registrationInfo = await RegisterDeviceAsync(DeviceId);

                if (registrationInfo != null)
                {
                    LocalStorage.SaveConnectionInfo(registrationInfo);
                }
                else
                {
                    Console.WriteLine("Enhetsregistrering misslyckades.");

                    return false;
                }
            }

            var connectionInfo = LocalStorage.LoadConnectionInfo(hiddenId, hiddenConnection);

            var apiUrl = $"{ApiBaseUrl}{DeviceId}";
            var responseContent = await CallApiWithConnectionInfo(apiUrl, connectionInfo);

            Console.WriteLine(responseContent);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ett fel inträffade: {ex.Message}");
            return false;
        }
    }

    public static async Task<bool> IsDeviceRegisteredAsync(string deviceId)
    {
        
        using (var httpClient = new HttpClient())
        {
            var response = await httpClient.GetAsync($"{ApiBaseUrl}{deviceId}");

            return response.IsSuccessStatusCode;
        }
    }

    public static async Task<ConnectionInfo> RegisterDeviceAsync(string deviceId)
    {
        
        using (var httpClient = new HttpClient())
        {
            var response = await httpClient.PostAsync($"{ApiBaseUrl}{deviceId}", null);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var registrationInfo = JsonConvert.DeserializeObject<ConnectionInfo>(responseContent);
                return registrationInfo;
            }

            return null;
        }
    }

    public static async Task<string> CallApiWithConnectionInfo(string apiUrl, ConnectionInfo connectionInfo)
    {
        
        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {connectionInfo.DeviceConnectionString}");
            var response = await httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsStringAsync();
            return "Anropet misslyckades.";
        }
    }

    

    public static async Task ShowWeatherInformationMethod(TextBlock cityTextBlock, TextBlock temperatureTextBlock, 
        TextBlock weatherDescriptionTextBlock, Image weatherIconImage)
    {
        WeatherApiClient weatherApiClient = new WeatherApiClient("6ba62436dd00318990437058362d6a82");
        string city = "Ciudad Quesada";

        string weatherData = await weatherApiClient.GetWeatherData(city);

        if (!string.IsNullOrEmpty(weatherData))
        {
            
            var weatherInfo = JsonConvert.DeserializeObject<WeatherInfo>(weatherData);

            
            cityTextBlock.Text = $"Stad: {weatherInfo.Name}";
            temperatureTextBlock.Text = $"Temperatur: {weatherInfo.Main.Temp:F1}°C";
            weatherDescriptionTextBlock.Text = $"Väder: {weatherInfo.Weather[0].Description.ToUpper()}";

            
            string iconUrl = $"http://openweathermap.org/img/w/{weatherInfo.Weather[0].Icon}.png";
            var iconBitmap = new BitmapImage(new Uri(iconUrl));
            weatherIconImage.Source = iconBitmap;
        }
        else
        {
            cityTextBlock.Text = "Fel vid hämtning av väderdata.";
            temperatureTextBlock.Text = "";
            weatherDescriptionTextBlock.Text = "";
            weatherIconImage.Source = null;
        }
    }
}