using Microsoft.AspNetCore.Components;
using System.ComponentModel;

namespace MijnThuis.Dashboard.Web.DependencyInjection;

public static class CascadingStateServiceCollectionExtensions
{
    public static IServiceCollection AddNotifyingCascadingValue<T>(
        this IServiceCollection services, T state, bool isFixed = false)
        where T : INotifyPropertyChanged
    {
        return services.AddCascadingValue<T>(sp =>
        {
            return new CascadingStateValueSource<T>(state, isFixed);
        });
    }

    private sealed class CascadingStateValueSource<T>
        : CascadingValueSource<T>, IDisposable where T : INotifyPropertyChanged
    {
        private readonly T state;
        private readonly CascadingValueSource<T> source;

        public CascadingStateValueSource(T state, bool isFixed = false)
            : base(state, isFixed = false)
        {
            this.state = state;
            source = new CascadingValueSource<T>(state, isFixed);
            this.state.PropertyChanged += HandlePropertyChanged;
        }

        private void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            _ = NotifyChangedAsync();
        }

        public void Dispose()
        {
            state.PropertyChanged -= HandlePropertyChanged;
        }
    }
}