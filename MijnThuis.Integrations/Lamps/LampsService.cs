using LifxCloud.NET;
using LifxCloud.NET.Models;
using Microsoft.Extensions.Configuration;

namespace MijnThuis.Integrations.Lamps;

public interface ILampsService
{
    Task<LampsOverview> GetOverview();
}

public class LampsService : LampsBaseService, ILampsService
{
    private readonly string _authToken;

    public LampsService(IConfiguration configuration)
    {
        _authToken = configuration.GetValue<string>("LAMPS_API_AUTH_TOKEN");
    }

    public async Task<LampsOverview> GetOverview()
    {
        var client = await LifxCloudClient.CreateAsync(_authToken);
        var lights = await client.ListLights();

        foreach (var light in lights)
        {
            await client.SetState(light, new SetStateRequest
            {
                Power = PowerState.On,
                Brightness = 1
            });
        }

        return new LampsOverview();
    }
}

public class LampsBaseService
{

}