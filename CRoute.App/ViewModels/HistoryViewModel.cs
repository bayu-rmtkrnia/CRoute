using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CRoute.App.Services;
using CRoute.Core.Interfaces;
using CRoute.Core.Models;

namespace CRoute.App.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly ITripHistoryRepository _historyRepository;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _navigation;
    private readonly TransportMode _baseline;

    public HistoryViewModel(
        ITripHistoryRepository historyRepository,
        ITransportModeProvider modes,
        IDialogService dialogs,
        INavigationService navigation)
    {
        _historyRepository = historyRepository;
        _dialogs = dialogs;
        _navigation = navigation;
        _baseline = modes.GetBaseline();

        LoadTrips();
    }

    [ObservableProperty]
    private IReadOnlyList<TripRowViewModel> _trips = [];

    [ObservableProperty]
    private string _countText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ClearCommand))]
    private bool _isEmpty;

    private void LoadTrips()
    {
        Trips = _historyRepository.GetHistory().Trips
            .OrderByDescending(t => t.TripDate)
            .Select(t => new TripRowViewModel(t, _baseline))
            .ToList();

        CountText = Trips.Count + " perjalanan tersimpan";
        IsEmpty = Trips.Count == 0;
    }

    private bool CanClear() => !IsEmpty;

    [RelayCommand(CanExecute = nameof(CanClear))]
    private void Clear()
    {
        if (!_dialogs.Confirm("Hapus semua riwayat perjalanan? Tindakan ini tidak bisa dibatalkan."))
            return;

        _historyRepository.ClearTrips();
        LoadTrips();
    }

    [RelayCommand]
    private void GoToCariRute() => _navigation.NavigateTo(AppPage.CariRute);
}
