namespace MijnThuis.Dashboard.App.Factories;

public class PageFactory
{
    private readonly IServiceProvider _serviceProvider;

    public PageFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public TPage CreatePage<TPage>() where TPage : ContentPage
    {
        return _serviceProvider.GetService<TPage>();
    }
}