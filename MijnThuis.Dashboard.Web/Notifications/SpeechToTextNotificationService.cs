namespace MijnThuis.Dashboard.Web.Notifications;

public class SpeechToTextNotificationService
{
    public event EventHandler? EventClick;

    public void NotifyEventClick(object sender)
    {
        if (EventClick != null)
        {
            EventClick(sender, EventArgs.Empty);
        }
    }
}