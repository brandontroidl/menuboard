using System.Windows;
using System.Windows.Media;
using MenuBoard.ViewModels;

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
