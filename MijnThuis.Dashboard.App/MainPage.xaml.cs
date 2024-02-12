
namespace MijnThuis.Dashboard.App;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    public void Reload()
    {
        mainWebView.Source = "http://192.168.10.2:8809/?accessKey=cddf6b39-ebc4-4744-ad09-6c486c28120d";
        mainWebView.Reload();
    }
}