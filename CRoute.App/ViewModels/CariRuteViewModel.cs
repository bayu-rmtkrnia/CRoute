using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CRoute.App.Converters;
using CRoute.App.Services;
using CRoute.Core.Interfaces;
using CRoute.Core.Models;

namespace CRoute.App.ViewModels;

/// <summary>
/// ViewModel halaman Cari Rute (sesuai class diagram): ambil rute dari
/// <see cref="IRouteService"/>, hitung emisi tiap moda lewat
/// <see cref="IEmissionService"/>, lalu bandingkan.
/// </summary>
public partial class CariRuteViewModel : ObservableObject
{
    private readonly IRouteService _routeService;
    private readonly IEmissionService _emissionService;
    private readonly ITripHistoryRepository _historyRepository;
    private readonly INavigationService _navigation;
    private readonly TransportMode _baseline;

    /// <summary>Hasil perhitungan terakhir yang belum disimpan ke riwayat.</summary>
    private Trip? _pendingTrip;

    public CariRuteViewModel(
        IRouteService routeService,
        IEmissionService emissionService,
        ITransportModeProvider modes,
        ITripHistoryRepository historyRepository,
        INavigationService navigation)
    {
        _routeService = routeService;
        _emissionService = emissionService;
        _historyRepository = historyRepository;
        _navigation = navigation;
        _baseline = modes.GetBaseline();

        ModeOptions = modes.GetAll().Select(m => new ModeOptionViewModel(m)).ToList();
        foreach (ModeOptionViewModel option in ModeOptions)
            option.PropertyChanged += OnModeOptionChanged;
    }

    public IReadOnlyList<ModeOptionViewModel> ModeOptions { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(HitungCommand))]
    private string _origin = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(HitungCommand))]
    private string _destination = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    public bool HasError => ErrorMessage.Length > 0;

    [ObservableProperty]
    private bool _hasResult;

    [ObservableProperty]
    private string _routeSummaryText = string.Empty;

    [ObservableProperty]
    private string _routeDetailText = string.Empty;

    [ObservableProperty]
    private string _recommendationTitle = string.Empty;

    [ObservableProperty]
    private string _recommendationDetail = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ModeComparisonRowViewModel> _comparisonRows = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SimpanCommand))]
    private bool _isSaved;

    private bool CanHitung() =>
        Origin.Trim().Length > 0 &&
        Destination.Trim().Length > 0 &&
        ModeOptions.Any(o => o.IsSelected);

    [RelayCommand(CanExecute = nameof(CanHitung))]
    private async Task HitungAsync()
    {
        ErrorMessage = string.Empty;

        string origin = Origin.Trim();
        string destination = Destination.Trim();

        if (string.Equals(origin, destination, StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = "Lokasi asal dan tujuan tidak boleh sama.";
            return;
        }

        Route route;
        try
        {
            route = await _routeService.GetRouteAsync(origin, destination);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Gagal mengambil rute: " + ex.Message;
            return;
        }

        var trip = new Trip { TripDate = DateTime.Now, Route = route };
        foreach (ModeOptionViewModel option in ModeOptions.Where(o => o.IsSelected))
            trip.AddResult(_emissionService.Calculate(route, option.Mode));

        EmissionResult best = trip.GetBestOption()!;
        double maxKg = trip.Results.Max(r => r.TotalEmissionKg);
        double saved = EmissionSavings.Calculate(route, best, _baseline);

        RouteSummaryText = RouteFormatter.ToSummaryString(route);
        RouteDetailText = RouteFormatter.ToDetailString(route);

        RecommendationTitle = "Rekomendasi: " + TransportModeIconConverter.ToGlyph(best.Mode.IconName) +
                              " " + best.Mode.Name;
        RecommendationDetail = saved > 0
            ? "Emisi " + EmissionResultFormatter.ToDisplayString(best) + " CO₂, hemat " +
              EmissionResultFormatter.FormatKg(saved) + " dibanding " + _baseline.Name.ToLowerInvariant() + "."
            : "Emisi " + EmissionResultFormatter.ToDisplayString(best) + " CO₂, terendah dari moda yang kamu pilih.";

        ComparisonRows = trip.Results
            .OrderByDescending(r => r.TotalEmissionKg)
            .Select(r => new ModeComparisonRowViewModel(
                r,
                isBest: ReferenceEquals(r, best),
                ratio: maxKg > 0 ? r.TotalEmissionKg / maxKg : 0))
            .ToList();

        _pendingTrip = trip;
        IsSaved = false;
        HasResult = true;
        SimpanCommand.NotifyCanExecuteChanged();
    }

    private bool CanSimpan() => _pendingTrip is not null && !IsSaved;

    [RelayCommand(CanExecute = nameof(CanSimpan))]
    private void Simpan()
    {
        if (_pendingTrip is null)
            return;

        _historyRepository.AddTrip(_pendingTrip);
        _pendingTrip = null;
        IsSaved = true;
    }

    [RelayCommand]
    private void GoToHistory() => _navigation.NavigateTo(AppPage.History);

    private void OnModeOptionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ModeOptionViewModel.IsSelected))
            HitungCommand.NotifyCanExecuteChanged();
    }
}
