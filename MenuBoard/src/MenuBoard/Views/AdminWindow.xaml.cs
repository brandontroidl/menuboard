using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using MenuBoard.ViewModels;
using Microsoft.Win32;
using MenuItem = MenuBoard.Models.MenuItem;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using TextBox = System.Windows.Controls.TextBox;

namespace MenuBoard.Views;

public partial class AdminWindow : Window
{
    private AdminViewModel ViewModel => (AdminViewModel)DataContext;

    public AdminWindow(AdminViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Closing the admin window shuts down the whole app, including both
        // TV displays. Confirm so the store operator doesn't blank the TVs
        // by accident - minimizing keeps the menus running.
        var result = MessageBox.Show(this,
            "Exit Menu Board? This will close both TV displays.\n\n" +
            "Yes  -  exit the app (TVs go blank)\n" +
            "No   -  keep the menus running and minimize this window instead",
            "Exit Menu Board",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
        }

        base.OnClosing(e);
    }

    private void Screen1Radio_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is AdminViewModel vm)
            vm.SelectedScreen = 1;
    }

    private void Screen2Radio_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is AdminViewModel vm)
            vm.SelectedScreen = 2;
    }

    private void DeleteCategory_Click(object sender, RoutedEventArgs e)
    {
        var category = ViewModel.SelectedCategory;
        if (category is null)
            return;

        // Category delete cascades to its items and there is no undo, so
        // confirm with the item count at risk before executing.
        var itemCount = category.Items.Count;
        var detail = itemCount switch
        {
            0 => "It has no items.",
            1 => "The 1 item in it will be deleted too.",
            _ => $"All {itemCount} items in it will be deleted too."
        };
        var result = MessageBox.Show(this,
            $"Delete the category \"{category.Name}\"? {detail}",
            "Delete Category",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result == MessageBoxResult.Yes)
            ViewModel.DeleteCategoryCommand.Execute(null);
    }

    private void CategoryNameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedCategory is not null)
            ViewModel.SaveCategoryName(ViewModel.SelectedCategory);
    }

    private void DisplaySetting_LostFocus(object sender, RoutedEventArgs e)
    {
        if (DataContext is AdminViewModel vm)
            vm.SaveDisplaySettings();
    }

    private void FontScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DataContext is AdminViewModel vm)
            vm.SaveDisplaySettings();
    }

    private void ItemField_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb || tb.Tag is not MenuItem item || item.Id <= 0)
            return;

        // The binding also updates its source on LostFocus, but its handler
        // can run AFTER this one - saving would then persist the OLD value
        // and the TVs wouldn't reflect the edit until the next unrelated
        // save. Push the pending text into the entity explicitly first.
        tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        ViewModel.SaveMenuItem(item);
    }

    private void AvailableToggle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is MenuItem item)
            ViewModel.SaveMenuItem(item);
    }

    private void BrowseImage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is MenuItem item)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Title = "Select Item Image"
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var relativePath = ViewModel.BrowseAndCopyImage(dialog.FileName);
                    if (relativePath is not null)
                    {
                        item.ImagePath = relativePath;
                        ViewModel.SaveMenuItem(item);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Failed to import image: {ex.Message}",
                        "Image Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void DeleteItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is MenuItem item)
            ViewModel.DeleteMenuItem(item.Id);
    }

    private void MoveItemUp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is MenuItem item)
            ViewModel.MoveMenuItemUp(item);
    }

    private void MoveItemDown_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is MenuItem item)
            ViewModel.MoveMenuItemDown(item);
    }
}
