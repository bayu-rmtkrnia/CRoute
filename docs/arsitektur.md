# Arsitektur CRoute

Dokumen ini menjelaskan struktur solusi, pembagian layer, alur data, dan
keadaan implementasi saat ini.

> **Legenda status**
> ✅ Sudah berjalan · ⚠️ Sebagian (ada, tapi belum lengkap/belum real) · ❌ Belum

## 1. Ringkasan

CRoute adalah aplikasi desktop **WPF** (.NET 10) untuk membandingkan emisi CO₂
dari beberapa moda transportasi untuk satu rute. Data pengguna, riwayat, target,
dan master moda disimpan di **PostgreSQL** (lokal atau Supabase).

| Aspek | Pilihan |
|---|---|
| Platform | Windows desktop |
| Framework UI | WPF, pola MVVM |
| Bahasa / runtime | C#, .NET 10 (`net10.0-windows` untuk App, `net10.0` untuk layer lain) |
| MVVM | CommunityToolkit.Mvvm 8.4.2 |
| Database | PostgreSQL (Npgsql 10.0.3, ADO.NET murni, bukan EF Core) |
| Integrasi eksternal | Google Maps (Routes API, Embed), Climatiq (faktor emisi; ditunda) |

## 2. Struktur solusi

Solusi `croute.slnx` berisi lima proyek:

```
croute.slnx
├── CRoute.Core/        Model domain + interface (tanpa dependensi)
├── CRoute.Services/    Integrasi API eksternal + pembacaan konfigurasi
├── CRoute.Data/        Implementasi akses PostgreSQL (Npgsql)
├── CRoute.App/         WPF: View, ViewModel, navigasi, fake, converter
└── CRoute.Tests/       Unit test xUnit (Core + Services + validasi Data)
```

Arah dependensi:

```
CRoute.App ──► CRoute.Services ──► CRoute.Core
     │                                 ▲
     └──────► CRoute.Data ─────────────┘

CRoute.Tests ──► CRoute.Core
      ├────────► CRoute.Services
      └────────► CRoute.Data
```

- `CRoute.Core` tidak bergantung ke proyek lain. Di sinilah model
  (`Route`, `TransportMode`, `Trip`, `EmissionResult`, `TripHistory`) dan
  kontrak (`IRouteService`, `ITransitRouteService`, `IEmissionService`,
  `ITripHistoryRepository`, `ITransportModeProvider`, `IAuthService`) berada.
- `CRoute.Services` berisi `RouteService`, `TransitRouteService`, dan
  `EmissionService` (sisi API) serta `ApiSettingsLoader` untuk membaca
  konfigurasi.
- `CRoute.Data` berisi implementasi PostgreSQL: `PostgresAuthService`,
  `PostgresTransportModeProvider`, `PostgresTripHistoryRepository`.
- `CRoute.App` berisi tampilan dan logika presentasi. Ia tidak tahu SQL; semua
  akses lewat interface.
- `CRoute.Tests` berisi unit test xUnit untuk model Core, konfigurasi Services,
  dan validasi Data yang tidak membuka koneksi database. Alur database tetap
  diuji lewat black box.

## 3. Komponen per layer

### 3.1 CRoute.Core

| Tipe | Peran | Status |
|---|---|---|
| `TransportMode` | Moda + faktor emisi (kg CO₂/km). `CalculateEmission(km)` = `km × EmissionFactor` | ✅ |
| `Route` | Origin, destination, jarak (km), durasi (menit) | ✅ |
| `EmissionResult` | Total emisi (kg) + moda yang dipakai | ✅ |
| `Trip` | Satu perjalanan: tanggal, rute, daftar hasil emisi, `ChosenModeId`. `GetBestOption()` = emisi terkecil; `GetChosenOption()` = pilihan pengguna dengan fallback opsi terbaik | ✅ |
| `TripHistory` | Daftar trip + target bulanan. `GetMonthlyTotal()`, `GetProgress()` | ✅ |
| `AppClock` | Waktu acuan aplikasi di zona Asia/Jakarta untuk agregasi bulanan/harian; melempar error jika zona tidak tersedia | ✅ unit test |
| `IRouteService` | `Task<Route> GetRouteAsync(origin, destination)` | ✅ kontrak |
| `ITransitRouteService` | `Task<TransitRouteResult> GetRoutesAsync(origin, destination)` | ✅ kontrak |
| `IEmissionService` | `EmissionResult Calculate(route, mode)` | ✅ kontrak |
| `ITripHistoryRepository` | `GetHistory`, `AddTrip`, `SetMonthlyTarget`, `ClearTrips` | ✅ kontrak |
| `ITransportModeProvider` | `GetAll()`, `GetBaseline()` | ✅ kontrak |
| `IAuthService` | Login, Register, Logout, `CurrentUserId`, `CurrentUserFullName` | ✅ kontrak |

Validasi ada di setter model (mis. nama moda tidak boleh kosong, emisi tidak
boleh negatif, jarak harus angka valid).

Status ✅ di tabel ini berarti perilakunya sudah dicek lewat unit test otomatis
atau black box. Integrasi eksternal yang belum selesai tetap ditandai ⚠️/❌.

### 3.2 CRoute.Services

| Tipe | Peran | Status |
|---|---|---|
| `RouteService` | Memanggil Google Maps Routes API (`computeRoutes`) | ✅ dipakai kalau API key diisi |
| `TransitRouteService` | Satu request `TRANSIT`, filter Bus/Kereta dan jalan kaki ≤ 2 km | ✅ dipakai kalau API key diisi |
| `EmissionService` | Kerangka integrasi Climatiq (`Calculate` placeholder + `RefreshEmissionFactorAsync`) | ❌ ditunda; belum dipasang |
| `ApiSettingsLoader` | Membaca `appsettings.json`, `appsettings.local.json`, dan env `CROUTE_*` | ✅ |
| `ApiSettings`, `GoogleMapsSettings`, `ClimatiqSettings`, `DatabaseSettings` | Model konfigurasi | ✅ |

> Catatan: `RouteService` sudah dipasang dan dipakai kalau
> `GoogleMaps:ApiKey` diisi. `EmissionService` (Climatiq) belum dipasang dan
> integrasinya ditunda; aplikasi memakai `LocalEmissionService` yang menghitung
> dari faktor tabel `transport_modes` (lihat bagian 5).

### 3.3 CRoute.Data

| Tipe | Peran | Status |
|---|---|---|
| `PostgresAuthService` | Login/register. Password di-hash PBKDF2-HMAC-SHA256 (100.000 iterasi, salt 16 byte, hash 32 byte) di aplikasi | ✅ |
| `PostgresTransportModeProvider` | Membaca `transport_modes` (`is_active`), di-cache di memori | ✅ |
| `PostgresTripHistoryRepository` | Riwayat, simpan trip lewat `fn_save_trip`, target lewat `fn_set_monthly_target`, hapus lewat `fn_clear_trips` | ✅ |

Skema dan function SQL ada di `docs/database/` dan menjadi sumber kebenaran.
Lihat [`docs/database/README.md`](database/README.md).

### 3.4 CRoute.App

| Kelompok | Isi | Status |
|---|---|---|
| Composition root | `App.xaml.cs` merakit semua dependensi dan menampilkan jendela login | ✅ |
| Navigasi | `MainViewModel` (menu samping), `AppPage` (Dashboard, CariRute, History, Stats), `INavigationService` | ✅ |
| Window | `WindowService`, `DialogService`, `IWindowService`, `IDialogService` | ✅ |
| ViewModel | `LoginViewModel`, `RegisterViewModel`, `MainViewModel`, `DashboardViewModel`, `CariRuteViewModel`, `HistoryViewModel`, `StatsViewModel`, plus row/item VM (`TripRowViewModel`, `DailyEmissionBarViewModel`, `ModeComparisonRowViewModel`) | ✅ |
| Helper presentasi | `EmissionSavings` (hitung hemat vs baseline Mobil, dipakai Cek Rute, Dashboard, Statistik) | ✅ |
| View | `LoginWindow`, `RegisterWindow`, `MainWindow`, `DashboardView`, `CariRuteView`, `HistoryView`, `StatsView` | ✅ |
| Converter | Formatter emisi (`EmissionResultFormatter`, `RouteFormatter`), konversi ikon, level warna, dll. | ✅ |
| Fakes | `FakeRouteService`, `FakeTransitRouteService`, `FakeAuthService`, `FakeTransportModeProvider`, `FakeTripHistoryRepository` | ✅ sebagai simulasi/uji |
| Peta (WebView2 / Embed) | Belum ada di View. Paket `Microsoft.Web.WebView2` sudah terpasang | ❌ |

## 4. Pola MVVM dan navigasi

- Setiap View hanya berisi XAML dan `InitializeComponent()`. Code-behind
  `CariRuteView.xaml.cs` memang kosong; data masuk lewat `{Binding}` dan aksi
  lewat `ICommand`.
- ViewModel memakai `[ObservableProperty]` dan `[RelayCommand]` dari
  CommunityToolkit.Mvvm.
- `MainViewModel` menyimpan `CurrentPage` dan `CurrentPageViewModel`. Halaman
  dibuat ulang setiap kali navigasi, sehingga data selalu segar (mis. riwayat baru
  langsung tampil di Dashboard).
- Akibatnya, hasil perbandingan di Cek Rute yang **belum disimpan** hilang saat
  pengguna pindah halaman.

## 5. Composition root dan wiring saat ini

`App.xaml.cs` menentukan implementasi mana yang dipakai:

| Interface | Implementasi yang dipakai | Status |
|---|---|---|
| `IAuthService` | `PostgresAuthService` | ✅ nyata |
| `ITransportModeProvider` | `PostgresTransportModeProvider` | ✅ nyata |
| `ITripHistoryRepository` | `PostgresTripHistoryRepository` | ✅ nyata |
| `IRouteService` | `RouteService` (kalau `GoogleMaps:ApiKey` diisi), selain itu `FakeRouteService` | ✅ / ⚠️ simulasi tanpa key |
| `ITransitRouteService` | `TransitRouteService` (kalau key diisi), selain itu `FakeTransitRouteService` | ✅ / ⚠️ kosong tanpa key |
| `IEmissionService` | `LocalEmissionService` (faktor dari tabel `transport_modes`) | ✅ perhitungan lokal; Climatiq ditunda |
| `IDialogService` | `DialogService` | ✅ |
| `IWindowService` | `WindowService` | ✅ |

Komposisi yang dipakai sekarang:

```csharp
_routeService = settings.GoogleMaps.HasApiKey
    ? new RouteService(Http, settings)
    : new FakeRouteService();
_transitService = settings.GoogleMaps.HasApiKey
    ? new TransitRouteService(Http, settings)
    : new FakeTransitRouteService();
_emissionService = new LocalEmissionService(); // Climatiq ditunda
```

Lihat [`integrasi-api.md`](integrasi-api.md) untuk status dan batasan integrasi.
`RouteService` dan `TransitRouteService` sudah memakai Routes API; tanpa API key
aplikasi memakai implementasi fake.

## 6. Alur data utama

### 6.1 Cek Rute → Simpan

```
CariRuteView ──(Bandingkan emisi)──► CariRuteViewModel.HitungAsync
    │
    ├─ validasi: asal/tujuan tidak kosong, tidak sama, ≥1 moda dipilih
    ├─ IRouteService.GetRouteAsync(asal, tujuan)           → Route referensi
    ├─ ITransitRouteService.GetRoutesAsync(...)            → Bus/Kereta (opsional)
    ├─ IEmissionService.Calculate(route, moda) per moda    → EmissionResult[]
    │     Bus/Kereta memakai jarak transit hasil filter 2 km
    ├─ Trip { Route, Results } → GetBestOption()           → rekomendasi
    ├─ pengguna memilih satu moda → ChosenModeId
    └─ tampilkan hasil (belum tersimpan, _pendingTrip)
          │
          └─(Simpan, wajib ada pilihan)──► ITripHistoryRepository.AddTrip(trip)
                          └─ SELECT fn_save_trip(user, origin, dest, km, menit, results::jsonb, tanggal, chosen_mode_id)
```

`fn_save_trip` menyimpan trip, rute, semua hasil emisi, dan moda pilihan dalam
satu transaksi. Kalau `chosen_mode_id` null (klien/data lama), function memakai
opsi terbaik sebagai fallback.

### 6.2 Dashboard dan Statistik

```
ITripHistoryRepository.GetHistory()
  └─ SELECT trip + route + emission_results + transport_modes (WHERE user_id)
        └─ TripHistory (di memori)
              ├─ GetMonthlyTotal()   → total opsi terpilih bulan berjalan
              ├─ GetProgress()       → total / target
              └─ EmissionSavings     → hemat vs Mobil dari opsi terpilih
```

Saat ini agregasi dilakukan di memori dari `GetHistory()`. Function SQL
(`fn_current_month_total`, `fn_daily_emissions`, dll.) sudah tersedia di
`functions.sql` untuk dipakai nanti jika riwayat sudah panjang. Lihat
[`perhitungan-emisi.md`](perhitungan-emisi.md).

### 6.3 Login

```
LoginViewModel ──► IAuthService.Login(username, password)
   └─ PostgresAuthService: ambil hash+salt → PBKDF2 → bandingkan (FixedTimeEquals)
   └─ sukses: CurrentUserId & CurrentUserFullName disimpan di memori
```

Session tidak disimpan ke database. Semua repository mengambil `user_id` dari
`CurrentUserId`.

## 7. Konfigurasi

Urutan pembacaan (nilai terakhir menang):

1. `appsettings.json` (di folder output `CRoute.Services`)
2. `appsettings.local.json` (opsional, sudah masuk `.gitignore`)
3. Environment variable berawalan `CROUTE_` (mis. `CROUTE_Database__ConnectionString`)

Bagian konfigurasi:

| Kunci | Dipakai oleh | Status |
|---|---|---|
| `Database:ConnectionString` | Semua repository/auth PostgreSQL | ✅ |
| `GoogleMaps:ApiKey` | `RouteService` | ✅ dipakai kalau diisi; tanpa key fallback simulasi |
| `GoogleMaps:RoutesBaseUrl` | `RouteService` | ✅ (ada nilai bawaan) |
| `GoogleMaps:EmbedBaseUrl` | Peta di View | ❌ belum ada |
| `Climatiq:ApiKey` | `EmissionService.RefreshEmissionFactorAsync` | ❌ belum dipanggil |
| `Climatiq:BaseUrl` | `EmissionService.RefreshEmissionFactorAsync` | ❌ belum dipanggil |

Jangan menulis API key asli di `appsettings.json` yang ikut di-commit. Gunakan
`appsettings.local.json` atau environment variable.

## 8. Teknologi dan dependensi

| Proyek | Paket | Versi |
|---|---|---|
| CRoute.App | CommunityToolkit.Mvvm | 8.4.2 |
| CRoute.App | Microsoft.Web.WebView2 | 1.0.4191.47 (belum dipakai di XAML) |
| CRoute.Data | Npgsql | 10.0.3 |
| CRoute.Services | Microsoft.Extensions.Configuration.Binder / .Json / .EnvironmentVariables | 10.0.12 |

## 9. Berkas di luar solusi

Di root repo ada berkas prototipe WinForms lama (`CRoute.csproj`, `MainForm.cs`,
`LoginForm.cs`, `RegisterForm.cs`, `Program.cs`, `AuthenticationContext.cs`,
`AuthenticationUi.cs`, `Data/`, `Authentication/`, `database/`). Berkas ini
**tidak masuk** `croute.slnx`, memakai SQLite, dan belum berstatus tracked di git.
Berkas ini dianggap tidak dipakai. Penghapusannya menunggu keputusan tim.

## 10. Rencana perubahan arsitektur

- Pasang `EmissionService` (Climatiq) di `App.xaml.cs` setelah parsing selesai.
  `RouteService` sudah terpasang.
- Tambah peta rute (Embed API) di `CariRuteView` memakai WebView2.
- Pertimbangkan agregasi SQL (`functions.sql`) jika riwayat sudah banyak.
- EF Core tetap menjadi opsi, tetapi saat ini tidak dipakai.
