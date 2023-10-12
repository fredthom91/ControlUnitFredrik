using System.Linq;
using System.Threading.Tasks;
using AzureFunctionFredrik.Data;
using ClassLibraryFredrik.DataModels;

namespace ControlUnitFredrik.Services;

public class StoreInDataBase
{
    public async Task UpdateDeviceRegistrationStatusAsync(string deviceName, string status)
    {
        using (var dbContext = new ApplicationDbContext())
        {
            var existingDevice = dbContext.DeviceRegistrations.FirstOrDefault(d => d.DeviceName == deviceName);

            if (existingDevice != null)
            {
                existingDevice.Status = status;
            }
            else
            {
                var newDevice = new DeviceRegistration
                {
                    DeviceName = deviceName,
                    Status = status
                };

                dbContext.DeviceRegistrations.Add(newDevice);
            }

            await dbContext.SaveChangesAsync();
        }
    }
}