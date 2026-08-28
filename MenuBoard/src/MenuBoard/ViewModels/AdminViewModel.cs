using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MenuBoard.Models;
using MenuBoard.Services;

namespace MenuBoard.ViewModels;

public partial class AdminViewModel : ObservableObject
{
    private readonly MenuDataService _dataService;
    private readonly ImageService _imageService;
    private readonly AppSettingsService _appSettings;
    private readonly StartupService _startupService;
    private readonly MonitorService _monitorService;

    /// <summary>Raised when monitor layout or swap settings change so the app can reposition the display windows.</summary>
    public event Action? MonitorSettingsChanged;

    [ObservableProperty]
    private int _selectedScreen = 1;

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private Category? _selectedCategory;

    [ObservableProperty]
    private DisplaySettings _currentDisplaySettings = new();

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _swapDisplays;

    [ObservableProperty]
    private int _monitorLayoutIndex;

    /// <summary>ComboBox items for the per-TV monitor pickers: "Automatic" followed by each detected monitor.</summary>
    public ObservableCollection<string> MonitorOptions { get; } = new();

    /// <summary>0 = Automatic; n = monitor n (left-to-right) for TV 1.</summary>
    [ObservableProperty]
    private int _tv1MonitorOption;

    /// <summary>0 = Automatic; n = monitor n (left-to-right) for TV 2.</summary>
    [ObservableProperty]
    private int _tv2MonitorOption;

    private bool _suppressSettingCallbacks;

    public AdminViewModel(MenuDataService dataService, ImageService imageService,
        AppSettingsService appSettings, StartupService startupService, MonitorService monitorService)
    {
        _dataService = dataService;
        _imageService = imageService;
        _appSettings = appSettings;
        _startupService = startupService;
        _monitorService = monitorService;

        MonitorOptions.Add("Automatic");
        var monitors = _monitorService.GetMonitorsSorted();
        for (var i = 0; i < monitors.Count; i++)
        {
            var label = $"Monitor {i + 1} of {monitors.Count} (left to right)";
            if (monitors[i].IsPrimary)
                label += " - primary";
            MonitorOptions.Add(label);
        }

        _suppressSettingCallbacks = true;
        StartWithWindows = _startupService.IsEnabled();
        SwapDisplays = _appSettings.Settings.SwapDisplays;
        MonitorLayoutIndex = (int)_appSettings.Settings.MonitorLayout;
        Tv1MonitorOption = ToOptionIndex(_appSettings.Settings.Tv1Monitor, monitors.Count);
        Tv2MonitorOption = ToOptionIndex(_appSettings.Settings.Tv2Monitor, monitors.Count);
        _suppressSettingCallbacks = false;

        LoadCategories();
        LoadDisplaySettings();
    }

    private static int ToOptionIndex(int monitorIndex, int monitorCount)
    {
        return monitorIndex >= 0 && monitorIndex < monitorCount ? monitorIndex + 1 : 0;
    }

    partial void OnTv1MonitorOptionChanged(int value)
    {
        if (_suppressSettingCallbacks || value < 0) return;
        _appSettings.Settings.Tv1Monitor = value - 1;
        _appSettings.Save();
        MonitorSettingsChanged?.Invoke();
    }

    partial void OnTv2MonitorOptionChanged(int value)
    {
        if (_suppressSettingCallbacks || value < 0) return;
        _appSettings.Settings.Tv2Monitor = value - 1;
        _appSettings.Save();
        MonitorSettingsChanged?.Invoke();
    }

    partial void OnStartWithWindowsChanged(bool value)
    {
        if (_suppressSettingCallbacks) return;
        if (!_startupService.SetEnabled(value))
        {
            // Registry write failed - revert the checkbox to reality.
            _suppressSettingCallbacks = true;
            StartWithWindows = _startupService.IsEnabled();
            _suppressSettingCallbacks = false;
        }
    }

    partial void OnSwapDisplaysChanged(bool value)
    {
        if (_suppressSettingCallbacks) return;
        _appSettings.Settings.SwapDisplays = value;
        _appSettings.Save();
        MonitorSettingsChanged?.Invoke();
    }

    partial void OnMonitorLayoutIndexChanged(int value)
    {
        if (_suppressSettingCallbacks) return;
        if (value < 0 || value > (int)MonitorLayout.AllMonitors) return;
        _appSettings.Settings.MonitorLayout = (MonitorLayout)value;
        _appSettings.Save();
        MonitorSettingsChanged?.Invoke();
    }

    partial void OnSelectedScreenChanged(int value)
    {
        // Persist any pending edits to the previous screen's settings before
        // they're discarded by the reload below (fields save on LostFocus/
        // ValueChanged, but a change committed by clicking straight to the
        // other screen's radio button could otherwise race the reload).
        _dataService.UpdateDisplaySettings(CurrentDisplaySettings);
        LoadCategories();
        LoadDisplaySettings();
    }

    public void LoadCategories()
    {
        SelectedCategory = null;
        var cats = _dataService.GetAllCategoriesForScreen(SelectedScreen);
        Categories = new ObservableCollection<Category>(cats);
        SelectedCategory = Categories.FirstOrDefault();
    }

    private void LoadDisplaySettings()
    {
        CurrentDisplaySettings = _dataService.GetDisplaySettings(SelectedScreen);
    }

    [RelayCommand]
    private void AddCategory()
    {
        var category = _dataService.AddCategory("New Category", SelectedScreen);
        LoadCategories();
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == category.Id);
    }

    [RelayCommand]
    private void DeleteCategory()
    {
        if (SelectedCategory is null) return;
        _dataService.DeleteCategory(SelectedCategory.Id);
        LoadCategories();
    }

    [RelayCommand]
    private void MoveCategoryUp()
    {
        if (SelectedCategory is null) return;
        var index = Categories.IndexOf(SelectedCategory);
        if (index <= 0) return;

        var prev = Categories[index - 1];
        var currentOrder = SelectedCategory.DisplayOrder;
        _dataService.ReorderCategory(SelectedCategory.Id, prev.DisplayOrder);
        _dataService.ReorderCategory(prev.Id, currentOrder);
        var selectedId = SelectedCategory.Id;
        LoadCategories();
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == selectedId);
    }

    [RelayCommand]
    private void MoveCategoryDown()
    {
        if (SelectedCategory is null) return;
        var index = Categories.IndexOf(SelectedCategory);
        if (index >= Categories.Count - 1) return;

        var next = Categories[index + 1];
        var currentOrder = SelectedCategory.DisplayOrder;
        _dataService.ReorderCategory(SelectedCategory.Id, next.DisplayOrder);
        _dataService.ReorderCategory(next.Id, currentOrder);
        var selectedId = SelectedCategory.Id;
        LoadCategories();
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == selectedId);
    }

    [RelayCommand]
    private void AddMenuItem()
    {
        if (SelectedCategory is null) return;
        var catId = SelectedCategory.Id;
        _dataService.AddMenuItem(catId, "New Item", 0);
        LoadCategories();
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == catId);
    }

    public void SaveMenuItem(MenuItem item)
    {
        _dataService.UpdateMenuItem(item);
    }

    public void DeleteMenuItem(int menuItemId)
    {
        _dataService.DeleteMenuItem(menuItemId);
        if (SelectedCategory is not null)
        {
            var catId = SelectedCategory.Id;
            LoadCategories();
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == catId);
        }
    }

    public void MoveMenuItemUp(MenuItem item)
    {
        if (SelectedCategory is null) return;
        var items = SelectedCategory.Items.OrderBy(i => i.DisplayOrder).ToList();
        var index = items.IndexOf(item);
        if (index <= 0) return;

        var prev = items[index - 1];
        var currentOrder = item.DisplayOrder;
        _dataService.ReorderMenuItem(item.Id, prev.DisplayOrder);
        _dataService.ReorderMenuItem(prev.Id, currentOrder);
        var catId = SelectedCategory.Id;
        LoadCategories();
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == catId);
    }

    public void MoveMenuItemDown(MenuItem item)
    {
        if (SelectedCategory is null) return;
        var items = SelectedCategory.Items.OrderBy(i => i.DisplayOrder).ToList();
        var index = items.IndexOf(item);
        if (index >= items.Count - 1) return;

        var next = items[index + 1];
        var currentOrder = item.DisplayOrder;
        _dataService.ReorderMenuItem(item.Id, next.DisplayOrder);
        _dataService.ReorderMenuItem(next.Id, currentOrder);
        var catId = SelectedCategory.Id;
        LoadCategories();
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == catId);
    }

    public string? BrowseAndCopyImage(string sourcePath)
    {
        return _imageService.CopyImageToStore(sourcePath);
    }

    public void SaveCategoryName(Category category)
    {
        _dataService.UpdateCategory(category);
    }

    public void SaveDisplaySettings()
    {
        _dataService.UpdateDisplaySettings(CurrentDisplaySettings);
    }
}
