using MijnThuis.Dashboard.App.Configuration;

namespace MijnThuis.Dashboard.App;

public partial class SettingsPage : ContentPage
{
    private readonly IClientConfiguration _clientConfiguration;

    public SettingsPage(IClientConfiguration clientConfiguration)
    {
        InitializeComponent();

        _clientConfiguration = clientConfiguration;
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        _clientConfiguration.AccessKey = accessKeyEditor.Text;

        PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.Microphone>();

        if (status != PermissionStatus.Granted)
        {
            await Permissions.RequestAsync<Permissions.Microphone>();
        }
    }
}