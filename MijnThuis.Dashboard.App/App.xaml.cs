using MijnThuis.Dashboard.App.Configuration;
using MijnThuis.Dashboard.App.Factories;

namespace MijnThuis.Dashboard.App;

public partial class App : Application
{
    private readonly PageFactory _pageFactory;

    public App(
        IClientConfiguration clientConfiguration,
        PageFactory pageFactory)
    {
        _pageFactory = pageFactory;

        InitializeComponent();

        if (string.IsNullOrEmpty(clientConfiguration.AccessKey))
        {
            MainPage = NavigationPage<SettingsPage>();
        }
        else
        {
            MainPage = NavigationPage<MainPage>();
        }
    }

    private Page NavigationPage<TPage>() where TPage : ContentPage
    {
        return _pageFactory.CreatePage<TPage>();
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