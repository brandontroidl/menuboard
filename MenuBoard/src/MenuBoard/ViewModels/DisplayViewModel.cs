using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MenuBoard.Models;
using MenuBoard.Services;
using System.Windows.Threading;

namespace MenuBoard.ViewModels;

public partial class DisplayViewModel : ObservableObject, IDisposable
{
    private readonly MenuDataService _dataService;
    private readonly int _screenNumber;
    private readonly Dispatcher _dispatcher;
    private bool _disposed;

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private DisplaySettings _settings = new();

    public int ScreenNumber => _screenNumber;

    public DisplayViewModel(MenuDataService dataService, int screenNumber)
    {
        _dataService = dataService;
        _screenNumber = screenNumber;
        _dispatcher = Dispatcher.CurrentDispatcher;
        _dataService.DataChanged += Refresh;
        Refresh();
    }

    public void Refresh()
    {
        if (!_dispatcher.CheckAccess())
        {
            _dispatcher.Invoke(Refresh);
            return;
        }

        if (_disposed)
            return;

        var cats = _dataService.GetCategoriesForScreen(_screenNumber);
        Categories = new ObservableCollection<Category>(cats);
        Settings = _dataService.GetDisplaySettings(_screenNumber);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _dataService.DataChanged -= Refresh;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
