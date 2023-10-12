using System.Text;
using Azure.Messaging.EventHubs;
using AzureFunctionFredrik.Data;
using ClassLibraryFredrik.DataModels;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureFunctionFredrik;

public class DataProcessorFunction
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DataProcessorFunction> _logger;

    public DataProcessorFunction(ILogger<DataProcessorFunction> logger, ApplicationDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    [Function(nameof(DataProcessorFunction))]
    public void Run(
        [EventHubTrigger("iothub-ehub-iothubfred-25250098-44d758f04e", Connection = "IotHubEndpoint")]
        EventData[] events)
    {
        foreach (var @event in events)
            try
            {
                var data = Encoding.UTF8.GetString(@event.Body.ToArray());


                var parsedData = JsonConvert.DeserializeObject<LampData>(data);

                if (parsedData != null)
                {
                    _logger.LogInformation("Event Body: {body}", data);

                    SendToDBMethod(parsedData);
                }
                else
                {
                    _logger.LogError("Felaktig JSON-data: {data}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fel i Run: {ex.Message}");
            }
    }


    public void SendToDBMethod(LampData lampData)
    {
        _db.LampData.Add(lampData);
        _db.SaveChanges();
    }
}