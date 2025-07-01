using Microsoft.AspNetCore.Components;

namespace MijnThuis.Dashboard.Web.Pages;

public partial class Chart
{
    [Inject]
    protected NavigationManager NavigationManager { get; set; }

    [Parameter]
    public string ChartType { get; set; }

    public void BackCommand()
    {
        NavigationManager.NavigateTo($"/{new Uri(NavigationManager.Uri).Query}");
    }
}