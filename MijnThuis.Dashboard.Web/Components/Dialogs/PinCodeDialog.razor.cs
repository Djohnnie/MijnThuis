using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MijnThuis.Dashboard.Web.Components.Dialogs;

public partial class PinCodeDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; }

    public string PinCode { get; set; } = "";

    [Parameter]
    public Color Color { get; set; }

    private void Submit() => MudDialog.Close(DialogResult.Ok(PinCode));

    private void Cancel() => MudDialog.Cancel();

    private void PinCodeNumber(string number)
    {
        PinCode += number;
    }
}