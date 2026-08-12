using System.Windows;
using System.Windows.Media;
using MenuBoard.ViewModels;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Brushes = System.Windows.Media.Brushes;

namespace MenuBoard.Views;

public partial class DisplayWindow : Window
{
    public DisplayWindow(DisplayViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(DisplayViewModel.Settings))
                ApplySettings();
        };
        Closed += (_, _) => viewModel.Dispose();

        // When the menus cover every monitor, these are the escape hatches
        // back to the admin editor.
        KeyDown += (_, args) =>
        {
            if (args.Key == System.Windows.Input.Key.Escape)
                App.Current.ActivateAdmin();
        };
        MouseDoubleClick += (_, _) => App.Current.ActivateAdmin();

        ApplySettings();
    }

    private void ApplySettings()
    {
        if (DataContext is DisplayViewModel vm)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(vm.Settings.BackgroundColor);
                RootGrid.Background = new SolidColorBrush(color);
            }
            catch
            {
                RootGrid.Background = Brushes.Black;
            }

            RootGrid.LayoutTransform = new ScaleTransform(vm.Settings.FontScale, vm.Settings.FontScale);
        }
    }
}
