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
    private readonly ITransitRouteService _transitService;
    private readonly IEmissionService _emissionService;
    private readonly ITripHistoryRepository _historyRepository;
    private readonly INavigationService _navigation;
    private readonly TransportMode _baseline;

    /// <summary>Hasil perhitungan terakhir yang belum disimpan ke riwayat.</summary>
    private Trip? _pendingTrip;

    /// <summary>Baris moda yang dipilih pengguna untuk disimpan.</summary>
    private ModeComparisonRowViewModel? _selectedRow;

    public CariRuteViewModel(
        IRouteService routeService,
        ITransitRouteService transitService,
        IEmissionService emissionService,
        ITransportModeProvider modes,
        ITripHistoryRepository historyRepository,
        INavigationService navigation)
    {
        _routeService = routeService;
        _transitService = transitService;
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
    [NotifyPropertyChangedFor(nameof(HasTransitStatus))]
    private string _transitStatusText = string.Empty;

    /// <summary>Catatan kenapa Bus/Kereta tidak muncul (mis. tidak ada rute).</summary>
    public bool HasTransitStatus => TransitStatusText.Length > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSelectionHint))]
    private bool _hasResult;

    /// <summary>
    /// Rute yang digambar di peta (lewat <c>RouteMapAssistant</c>). Peta hanya
    /// menggambar garis kalau <see cref="Route.EncodedPolyline"/> terisi.
    /// </summary>
    [ObservableProperty]
    private Route? _mapRoute;

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
    [NotifyPropertyChangedFor(nameof(ShowSelectionHint))]
    private bool _hasSelection;

    [ObservableProperty]
    private string _selectedModeText = string.Empty;

    /// <summary>
    /// Petunjuk muncul setelah hasil ada, tetapi pengguna belum memilih moda
    /// yang benar-benar dipakai. Tombol simpan tetap nonaktif sampai memilih.
    /// </summary>
    public bool ShowSelectionHint => HasResult && !HasSelection && !IsSaved;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SimpanCommand))]
    [NotifyPropertyChangedFor(nameof(ShowSelectionHint))]
    private bool _isSaved;

    private bool CanHitung() =>
        Origin.Trim().Length > 0 &&
        Destination.Trim().Length > 0 &&
        ModeOptions.Any(o => o.IsSelected);

    [RelayCommand(CanExecute = nameof(CanHitung))]
    private async Task HitungAsync()
    {
        ErrorMessage = string.Empty;
        ResetResult();

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

        var selected = ModeOptions.Where(o => o.IsSelected).ToList();
        bool wantsBus = selected.Any(o => IsBus(o.Mode));
        bool wantsTrain = selected.Any(o => IsTrain(o.Mode));

        TransitRouteResult transit = TransitRouteResult.Empty;
        string? transitError = null;

        if (wantsBus || wantsTrain)
        {
            try
            {
                transit = await _transitService.GetRoutesAsync(origin, destination);
            }
            catch (Exception ex)
            {
                transitError = "Gagal mengambil rute transit: " + ex.Message;
            }
        }

        var trip = new Trip { TripDate = DateTime.Now, Route = route };
        var transitDetails = new Dictionary<TransportMode, string>();
        var transitNotes = new List<string>();

        foreach (ModeOptionViewModel option in selected)
        {
            if (IsBus(option.Mode))
            {
                if (transit.Bus is { } bus)
                {
                    trip.AddResult(CreateTransitResult(bus, option.Mode));
                    transitDetails[option.Mode] = FormatTransitDetail("bus", bus);
                }
                else if (transitError is null)
                {
                    transitNotes.Add("Bus tidak tersedia untuk rute ini.");
                }

                continue;
            }

            if (IsTrain(option.Mode))
            {
                if (transit.Train is { } train)
                {
                    trip.AddResult(CreateTransitResult(train, option.Mode));
                    transitDetails[option.Mode] = FormatTransitDetail("kereta", train);
                }
                else if (transitError is null)
                {
                    transitNotes.Add("Kereta tidak tersedia untuk rute ini.");
                }

                continue;
            }

            trip.AddResult(_emissionService.Calculate(route, option.Mode));
        }

        if (transitError is not null)
            transitNotes.Insert(0, transitError);

        TransitStatusText = string.Join(" ", transitNotes);

        if (trip.Results.Count == 0)
        {
            ErrorMessage = "Tidak ada moda yang tersedia untuk rute ini.";
            HasResult = false;
            return;
        }

        EmissionResult best = trip.GetBestOption()!;
        double maxKg = trip.Results.Max(r => r.TotalEmissionKg);
        double saved = EmissionSavings.Calculate(route, best, _baseline);

        MapRoute = route;
        RouteSummaryText = RouteFormatter.ToSummaryString(route);
        RouteDetailText = RouteFormatter.ToDetailString(route);

        RecommendationTitle = "Rekomendasi: " + TransportModeIconConverter.ToGlyph(best.Mode.IconName) +
                              " " + best.Mode.Name;
        RecommendationDetail = saved > 0
            ? "Emisi " + EmissionResultFormatter.ToDisplayString(best) + " CO₂, hemat " +
              EmissionResultFormatter.FormatKg(saved) + " dibanding " + _baseline.Name.ToLowerInvariant() + "."
            : "Emisi " + EmissionResultFormatter.ToDisplayString(best) + " CO₂, terendah dari moda yang kamu pilih.";

        SetComparisonRows(
            trip.Results
                .OrderByDescending(r => r.TotalEmissionKg)
                .Select(r => new ModeComparisonRowViewModel(
                    r,
                    isBest: ReferenceEquals(r, best),
                    ratio: maxKg > 0 ? r.TotalEmissionKg / maxKg : 0,
                    detail: transitDetails.GetValueOrDefault(r.Mode)))
                .ToList());

        _pendingTrip = trip;
        IsSaved = false;
        HasResult = true;
        SimpanCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Menghapus hasil lama sebelum percobaan baru. Tanpa ini, hasil rute yang
    /// sukses sebelumnya tetap tampil dan tombol simpan masih aktif walaupun
    /// pencarian berikutnya gagal.
    /// </summary>
    private void ResetResult()
    {
        _pendingTrip = null;
        IsSaved = false;
        HasResult = false;
        MapRoute = null;
        RouteSummaryText = string.Empty;
        RouteDetailText = string.Empty;
        RecommendationTitle = string.Empty;
        RecommendationDetail = string.Empty;
        SetComparisonRows([]);
        TransitStatusText = string.Empty;
        SimpanCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Mengganti daftar baris sekaligus memasang/mencabut listener pilihan.
    /// Pilihan selalu dikosongkan agar pengguna memilih ulang untuk setiap
    /// perhitungan baru.
    /// </summary>
    private void SetComparisonRows(IReadOnlyList<ModeComparisonRowViewModel> rows)
    {
        foreach (ModeComparisonRowViewModel row in ComparisonRows)
            row.PropertyChanged -= OnComparisonRowChanged;

        ComparisonRows = rows;

        foreach (ModeComparisonRowViewModel row in rows)
            row.PropertyChanged += OnComparisonRowChanged;

        _selectedRow = null;
        UpdateSelectionState();
    }

    private void OnComparisonRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ModeComparisonRowViewModel.IsSelected) ||
            sender is not ModeComparisonRowViewModel row)
        {
            return;
        }

        if (row.IsSelected)
        {
            // Pastikan hanya satu baris terpilih walaupun RadioButton di XAML
            // berada dalam ItemsControl.
            foreach (ModeComparisonRowViewModel other in ComparisonRows)
            {
                if (!ReferenceEquals(other, row))
                    other.IsSelected = false;
            }

            _selectedRow = row;
        }
        else if (ReferenceEquals(_selectedRow, row))
        {
            _selectedRow = null;
        }

        UpdateSelectionState();
    }

    private void UpdateSelectionState()
    {
        HasSelection = _selectedRow is not null;
        SelectedModeText = _selectedRow is null
            ? string.Empty
            : "Dipilih: " + _selectedRow.Mode.Name;

        SimpanCommand.NotifyCanExecuteChanged();
    }

    private bool CanSimpan() => _pendingTrip is not null && _selectedRow is not null && !IsSaved;

    [RelayCommand(CanExecute = nameof(CanSimpan))]
    private void Simpan()
    {
        if (_pendingTrip is null || _selectedRow is null)
            return;

        // Moda yang dipilih pengguna adalah yang disimpan sebagai riwayat,
        // bukan otomatis moda dengan emisi terendah.
        _pendingTrip.ChosenModeId = _selectedRow.Result.Mode.Id;

        try
        {
            _historyRepository.AddTrip(_pendingTrip);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Gagal menyimpan perjalanan: " + ex.Message;
            return;
        }

        ErrorMessage = string.Empty;
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

    private static bool IsBus(TransportMode mode) =>
        mode.Name.Equals("Bus", StringComparison.OrdinalIgnoreCase);

    private static bool IsTrain(TransportMode mode) =>
        mode.Name.Equals("Kereta", StringComparison.OrdinalIgnoreCase);

    private static EmissionResult CreateTransitResult(TransitRouteOption option, TransportMode mode) =>
        new()
        {
            Mode = mode,
            TotalEmissionKg = mode.CalculateEmission(option.TransitDistanceKm),
        };

    private static string FormatTransitDetail(string label, TransitRouteOption option)
    {
        string transport = option.Lines.Count > 0
            ? string.Join(" → ", option.Lines)
            : char.ToUpperInvariant(label[0]) + label[1..];

        return transport +
               " · " + RouteFormatter.FormatDistance(option.TransitDistanceKm) + " " + label +
               " + " + RouteFormatter.FormatDistance(option.WalkDistanceKm) + " jalan kaki";
    }
}
