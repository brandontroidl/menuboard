using System.Windows;
using System.Windows.Controls;
using MenuBoard.Models;
using MenuBoard.ViewModels;
using Microsoft.Win32;

namespace MenuBoard.Views;

public partial class AdminWindow : Window
{
    private AdminViewModel ViewModel => (AdminViewModel)DataContext;

    public AdminWindow(AdminViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
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

    private void CategoryNameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedCategory is not null)
            ViewModel.SaveCategoryName(ViewModel.SelectedCategory);
    }

    private void ItemField_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is MenuItem item)
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
                var relativePath = ViewModel.BrowseAndCopyImage(dialog.FileName);
                if (relativePath is not null)
                {
                    item.ImagePath = relativePath;
                    ViewModel.SaveMenuItem(item);
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
