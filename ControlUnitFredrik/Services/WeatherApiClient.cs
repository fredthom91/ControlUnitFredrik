using System.Net.Http;
using System.Threading.Tasks;

namespace ControlUnitFredrik.Services;

public class WeatherApiClient
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;

    public WeatherApiClient(string apiKey)
    {
        _httpClient = new HttpClient();
        _apiKey = apiKey;
    }

    public async Task<string> GetWeatherData(string city)
    {
        var apiUrl = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={_apiKey}&units=metric";
        var response = await _httpClient.GetAsync(apiUrl);

        if (response.IsSuccessStatusCode) return await response.Content.ReadAsStringAsync();

        return null;
    }
}