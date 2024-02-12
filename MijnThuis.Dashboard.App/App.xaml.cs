
namespace MijnThuis.Dashboard.App;

public partial class App : Application
{
    public App(MainPage mainPage)
    {
        InitializeComponent();

        MainPage = mainPage;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);

        window.Activated += (sender, args) =>
        {
            if (MainPage is MainPage mainPage)
            {
                mainPage.Reload();
            }
        };

        return window;
    }
}