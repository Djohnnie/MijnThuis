using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MijnThuis.Dashboard.Web.Model;

public class NotifyingDarkMode : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private bool _isDarkMode;

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                OnPropertyChanged();
            }
        }
    }

    protected virtual void OnPropertyChanged(
        [CallerMemberName] string? propertyName = default)
            => PropertyChanged?.Invoke(this, new(propertyName));

    public void ToggleDarkMode()
    {
        IsDarkMode = !IsDarkMode;
    }
}