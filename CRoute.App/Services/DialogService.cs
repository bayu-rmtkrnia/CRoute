using System.Windows;

namespace CRoute.App.Services;

public class DialogService : IDialogService
{
    private const string Title = "CRoute";

    public void ShowInfo(string message)
    {
        MessageBox.Show(message, Title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public bool Confirm(string message)
    {
        return MessageBox.Show(message, Title, MessageBoxButton.YesNo, MessageBoxImage.Warning)
               == MessageBoxResult.Yes;
    }
}
