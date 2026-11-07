using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CRoute.App.Converters;
using CRoute.App.Services;
using CRoute.Core.Interfaces;
using CRoute.Core.Models;

namespace CRoute.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private const int RecentTripCount = 3;

    private readonly INavigationService _navigation;

    public DashboardViewModel(
        IAuthService auth,
        ITripHistoryRepository historyRepository,
        ITransportModeProvider modes,
        INavigationService navigation)
    {
        _navigation = navigation;

        string fullName = auth.CurrentUserFullName ?? "Tamu";
        string firstName = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Tamu";
        Greeting = "Halo, " + firstName + "!";
        DateText = DateTime.Now.ToString("dddd, d MMMM yyyy", DisplayCulture.Indonesian);

        TripHistory history = historyRepository.GetHistory();
        TransportMode baseline = modes.GetBaseline();

        double total = history.GetMonthlyTotal();
        double percent = history.GetProgress() * 100;

        MonthTotalText = EmissionResultFormatter.FormatKg(total, decimals: 1, withUnit: false);
        SavedTotalText = EmissionResultFormatter.FormatKg(
            history.Trips.Sum(t => EmissionSavings.ForTrip(t, baseline)), decimals: 1, withUnit: false);
        TripCount = history.Trips.Count;

        TargetPercent = Math.Min(percent, 100);
        TargetDetail = EmissionResultFormatter.FormatKg(total, 1, withUnit: false) + " dari " +
                       EmissionResultFormatter.FormatKg(history.MonthlyTargetKg, 1) + " terpakai (" +
                       percent.ToString("0", DisplayCulture.Indonesian) + "%)";
        TargetStatus = percent < 70 ? "Masih aman, pertahankan!"
                     : percent < 100 ? "Hampir mencapai target bulan ini"
                     : "Target bulan ini sudah terlampaui";

        RecentTrips = history.Trips
            .OrderByDescending(t => t.TripDate)
            .Take(RecentTripCount)
            .Select(t => new TripRowViewModel(t, baseline))
            .ToList();
    }

    public string Greeting { get; }

    public string DateText { get; }

    public string MonthTotalText { get; }

    public string SavedTotalText { get; }

    public int TripCount { get; }

    public double TargetPercent { get; }

    public string TargetDetail { get; }

    public string TargetStatus { get; }

    public IReadOnlyList<TripRowViewModel> RecentTrips { get; }

    public bool HasTrips => RecentTrips.Count > 0;

    [RelayCommand]
    private void GoToCariRute() => _navigation.NavigateTo(AppPage.CariRute);

    [RelayCommand]
    private void GoToStats() => _navigation.NavigateTo(AppPage.Stats);

    [RelayCommand]
    private void GoToHistory() => _navigation.NavigateTo(AppPage.History);
}
