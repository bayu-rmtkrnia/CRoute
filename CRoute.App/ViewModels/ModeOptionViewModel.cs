using CommunityToolkit.Mvvm.ComponentModel;
using CRoute.Core.Models;

namespace CRoute.App.ViewModels;

/// <summary>Chip moda di halaman Cari Rute yang bisa dicentang/tidak.</summary>
public partial class ModeOptionViewModel : ObservableObject
{
    public ModeOptionViewModel(TransportMode mode)
    {
        Mode = mode;
    }

    public TransportMode Mode { get; }

    [ObservableProperty]
    private bool _isSelected = true;
}
