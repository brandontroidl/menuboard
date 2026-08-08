using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MenuBoard.Models;
using MenuBoard.Services;

namespace MenuBoard.ViewModels;

public partial class DisplayViewModel : ObservableObject
{
    private readonly MenuDataService _dataService;
    private readonly int _screenNumber;

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private DisplaySettings _settings = new();

    public int ScreenNumber => _screenNumber;

    public DisplayViewModel(MenuDataService dataService, int screenNumber)
    {
        _dataService = dataService;
        _screenNumber = screenNumber;
        _dataService.DataChanged += Refresh;
        Refresh();
    }

    public void Refresh()
    {
        var cats = _dataService.GetCategoriesForScreen(_screenNumber);
        Categories = new ObservableCollection<Category>(cats);
        Settings = _dataService.GetDisplaySettings(_screenNumber);
    }
}
