
using MijnThuis.Dashboard.App.Configuration;

namespace MijnThuis.Dashboard.App;

public partial class MainPage : ContentPage
{
    private readonly IClientConfiguration _clientConfiguration;

    public MainPage(IClientConfiguration clientConfiguration)
    {
        InitializeComponent();
        _clientConfiguration = clientConfiguration;
    }

    public void Reload()
    {
        mainWebView.Source = $"https://thuis.djohnnie.be:8809/?accessKey={_clientConfiguration.AccessKey}";
        mainWebView.Reload();
    }
}