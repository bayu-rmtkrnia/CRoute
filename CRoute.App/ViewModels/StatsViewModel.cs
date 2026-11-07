using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CRoute.App.Converters;
using CRoute.Core.Interfaces;
using CRoute.Core.Models;

namespace CRoute.App.ViewModels;

public partial class StatsViewModel : ObservableObject
{
    private const int ChartDays = 7;

    private readonly ITripHistoryRepository _historyRepository;
    private readonly TransportMode _baseline;

    public StatsViewModel(ITripHistoryRepository historyRepository, ITransportModeProvider modes)
    {
        _historyRepository = historyRepository;
        _baseline = modes.GetBaseline();

        TargetInput = historyRepository.GetHistory().MonthlyTargetKg.ToString("F1", DisplayCulture.Indonesian);
        Refresh();
    }

    [ObservableProperty] private string _totalText = string.Empty;
    [ObservableProperty] private string _ofTargetText = string.Empty;
    [ObservableProperty] private double _targetPercent;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private string _savedAllText = string.Empty;
    [ObservableProperty] private string _averageText = string.Empty;
    [ObservableProperty] private string _favoriteModeName = string.Empty;
    [ObservableProperty] private IReadOnlyList<DailyEmissionBarViewModel> _dailyBars = [];

    /// <summary>Isi kotak input target (teks mentah, divalidasi saat disimpan).</summary>
    [ObservableProperty] private string _targetInput = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTargetMessage))]
    private string _targetMessage = string.Empty;

    [ObservableProperty] private bool _isTargetMessageError;

    public bool HasTargetMessage => TargetMessage.Length > 0;

    [RelayCommand]
    private void SaveTarget()
    {
        string text = TargetInput.Trim().Replace(',', '.');
        bool ok = double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value);

        if (!ok || value <= 0)
        {
            ShowTargetMessage("Masukkan angka target yang lebih dari 0.", isError: true);
            return;
        }

        _historyRepository.SetMonthlyTarget(value);
        Refresh();
        ShowTargetMessage("✓ Target disimpan.", isError: false);
    }

    private void ShowTargetMessage(string message, bool isError)
    {
        IsTargetMessageError = isError;
        TargetMessage = message;
    }

    private void Refresh()
    {
        TripHistory history = _historyRepository.GetHistory();
        IReadOnlyList<Trip> trips = history.Trips;

        double total = history.GetMonthlyTotal();
        double percent = history.GetProgress() * 100;

        TotalText = EmissionResultFormatter.FormatKg(total, decimals: 1);
        OfTargetText = "dari target " + EmissionResultFormatter.FormatKg(history.MonthlyTargetKg, 1) +
                       " (" + percent.ToString("0", DisplayCulture.Indonesian) + "%)";
        TargetPercent = Math.Min(percent, 100);
        StatusText = percent < 70 ? "Masih aman, pertahankan! 🎉"
                   : percent < 100 ? "Hampir mencapai target, pilih moda yang lebih hemat."
                   : "Target bulan ini sudah terlampaui.";

        SavedAllText = EmissionResultFormatter.FormatKg(trips.Sum(t => EmissionSavings.ForTrip(t, _baseline)), 1);
        AverageText = trips.Count > 0
            ? EmissionResultFormatter.FormatKg(trips.Average(BestEmissionKg))
            : "-";

        var chosenModes = trips
            .Select(t => t.GetBestOption()?.Mode.Name)
            .OfType<string>()
            .ToList();
        FavoriteModeName = chosenModes.Count > 0
            ? chosenModes.GroupBy(name => name).OrderByDescending(g => g.Count()).First().Key
            : "-";

        DailyBars = BuildDailyBars(trips);
    }

    private static List<DailyEmissionBarViewModel> BuildDailyBars(IReadOnlyList<Trip> trips)
    {
        var days = Enumerable.Range(0, ChartDays)
            .Select(i => DateTime.Today.AddDays(i - (ChartDays - 1)))
            .Select(day => (Day: day, Kg: trips.Where(t => t.TripDate.Date == day).Sum(BestEmissionKg)))
            .ToList();

        double max = days.Max(d => d.Kg);

        return days
            .Select(d => new DailyEmissionBarViewModel
            {
                Label = d.Day.ToString("ddd", DisplayCulture.Indonesian),
                ValueText = d.Kg > 0 ? EmissionResultFormatter.FormatKg(d.Kg, 1, withUnit: false) : string.Empty,
                Ratio = max > 0 ? d.Kg / max : 0,
            })
            .ToList();
    }

    private static double BestEmissionKg(Trip trip) => trip.GetBestOption()?.TotalEmissionKg ?? 0;
}
