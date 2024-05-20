using Android.Webkit;

namespace MijnThuis.Dashboard.App.Platforms.Android;

internal class MyWebChromeClient : WebChromeClient
{
    public override void OnPermissionRequest(PermissionRequest request)
    {

        foreach (var resource in request.GetResources())
        {
            if (resource.Equals(PermissionRequest.ResourceAudioCapture, StringComparison.OrdinalIgnoreCase))
            {
                request.Grant(request.GetResources());
                return;
            }
        }

        base.OnPermissionRequest(request);
    }
}