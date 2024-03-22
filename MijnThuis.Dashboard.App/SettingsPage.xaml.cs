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

    private void Button_Clicked(object sender, EventArgs e)
    {
        _clientConfiguration.AccessKey = accessKeyEditor.Text;
    }
}