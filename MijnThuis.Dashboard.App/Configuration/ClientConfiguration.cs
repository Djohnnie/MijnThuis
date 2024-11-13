using Microsoft.Maui.Storage;
namespace MijnThuis.Dashboard.App.Configuration;

public interface IClientConfiguration
{
    string AccessKey { get; set; }
}

public class ClientConfiguration : IClientConfiguration
{
    #region <| AccessKey |>

    private const string IdAccessKey = "access_key";
    private static readonly string DefaultAccessKey = "";

    public string AccessKey
    {
        get => Preferences.Get(IdAccessKey, DefaultAccessKey);
        set => Preferences.Set(IdAccessKey, value);
    }

    #endregion
}