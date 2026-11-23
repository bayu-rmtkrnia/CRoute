# Function C# yang Perlu Disiapkan

Dokumen ini melengkapi `schema.sql` dan `functions.sql`. Fokusnya: apa saja
class/method di `CRoute.Data` yang perlu dibuat agar UI yang sudah ada memakai
PostgreSQL, tanpa mengubah View/ViewModel lebih dari perlu.

> Untuk memakai Supabase sebagai host PostgreSQL, lihat `supabase.md`
> (connection string, role, SSL, dan key yang diperlukan).
>
> **Status:** implementasi saat ini memakai Npgsql murni (ADO.NET) di
> `CRoute.Data` — lihat `PostgresAuthService`, `PostgresTransportModeProvider`,
> dan `PostgresTripHistoryRepository`. Dokumen ini tetap menjadi panduan kalau
> nanti ingin pindah ke EF Core.
>
> Nama kolom aktual mengikuti tabel target: `trip_id`, `date`,
> `transport_mode_id`, `icon`, `emission_factor`, `emission_id`, `route_id`;
> tabel `settings` menggantikan `user_settings`.

## 1. Package dan konfigurasi

```powershell
dotnet add CRoute.Data package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.3
dotnet add CRoute.Data package Microsoft.EntityFrameworkCore.Design
```

Connection string disarankan memakai format Npgsql:

```json
{
  "Database": {
    "ConnectionString": "Host=localhost;Port=5432;Database=croute;Username=croute_app;Password=RAHASIA;Include Error Detail=true"
  }
}
```

Simpan di `CRoute.Services/appsettings.json` (file ini sudah di-copy ke output
oleh `CRoute.Services.csproj`), lalu tambahkan property di `ApiSettings`:

```csharp
public sealed class DatabaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class ApiSettings
{
    public GoogleMapsSettings GoogleMaps { get; set; } = new();
    public ClimatiqSettings Climatiq { get; set; } = new();
    public DatabaseSettings Database { get; set; } = new(); // baru
}
```

`CRoute.Data` tidak perlu membaca JSON sendiri; `App.xaml.cs` yang membaca
konfigurasi lalu memberi connection string ke factory `DbContext`.

## 2. Perubahan kecil di CRoute.Core

`IAuthService` sekarang belum menyimpan ID pengguna, padahal repository butuh
`user_id`. Tambahkan satu property:

```csharp
public interface IAuthService
{
    Guid? CurrentUserId { get; }
    string? CurrentUserFullName { get; }

    bool Login(string username, string password);
    bool Register(string fullName, string username, string password);
    void Logout();
}
```

`FakeAuthService` juga perlu menyesuaikan (misalnya memakai Guid tetap untuk
akun demo). Ini satu-satunya perubahan kontrak yang diperlukan.

Alternatif yang lebih rapi: buat `ICurrentUser` terpisah di `CRoute.Core`, lalu
`IAuthService` dan repository sama-sama bergantung padanya. Untuk proyek ini,
menambah `CurrentUserId` di `IAuthService` sudah cukup.

## 3. Entity persistence di CRoute.Data

Model `CRoute.Core` punya private setter, validasi di setter, dan koleksi
read-only (`Trip.Results`, `TripHistory.Trips`). Jangan dipetakan langsung ke
EF Core. Buat entity terpisah, lalu konversi di repository.

```text
CRoute.Data/
├── CrouteDbContext.cs
├── CrouteDbContextFactory.cs        // design-time untuk dotnet ef
├── Entities/
│   ├── UserEntity.cs
│   ├── SettingEntity.cs
│   ├── TransportModeEntity.cs
│   ├── TripEntity.cs
│   ├── RouteEntity.cs
│   └── EmissionResultEntity.cs
├── Repositories/
│   ├── PostgresAuthService.cs
│   ├── PostgresTransportModeProvider.cs
│   └── PostgresTripHistoryRepository.cs
└── Stats/
    ├── IStatsService.cs              // opsional, lihat bagian 8
    └── PostgresStatsService.cs
```

### Contoh entity

```csharp
public sealed class UserEntity
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty; // selalu lowercase
    public byte[] PasswordHash { get; set; } = [];
    public byte[] PasswordSalt { get; set; } = [];
    public int PasswordIterations { get; set; } = 100_000;
    public string PasswordAlgorithm { get; set; } = "pbkdf2-sha256";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public SettingEntity? Settings { get; set; }
    public List<TripEntity> Trips { get; set; } = [];
}

public sealed class TripEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? ChosenModeId { get; set; }
    public DateTime TripDate { get; set; } // UTC
    public DateTime CreatedAt { get; set; }

    public RouteEntity Route { get; set; } = null!;
    public List<EmissionResultEntity> Results { get; set; } = [];
}
```

### DbContext

```csharp
public sealed class CrouteDbContext : DbContext
{
    public CrouteDbContext(DbContextOptions<CrouteDbContext> options) : base(options) { }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<SettingEntity> Settings => Set<SettingEntity>();
    public DbSet<TransportModeEntity> TransportModes => Set<TransportModeEntity>();
    public DbSet<TripEntity> Trips => Set<TripEntity>();
    public DbSet<RouteEntity> Routes => Set<RouteEntity>();
    public DbSet<EmissionResultEntity> EmissionResults => Set<EmissionResultEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(100);
            e.Property(x => x.Username).HasColumnName("username").HasMaxLength(50);
            e.Property(x => x.PasswordHash).HasColumnName("password_hash");
            e.Property(x => x.PasswordSalt).HasColumnName("password_salt");
            e.Property(x => x.PasswordIterations).HasColumnName("password_iterations");
            e.Property(x => x.PasswordAlgorithm).HasColumnName("password_algorithm").HasMaxLength(30);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(x => x.Username).IsUnique(); // username dinormalisasi lowercase
        });

        modelBuilder.Entity<TripEntity>(e =>
        {
            e.ToTable("trips");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("trip_id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.TripDate).HasColumnName("date");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Route)
             .WithOne()
             .HasForeignKey<RouteEntity>(x => x.TripId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ... konfigurasi RouteEntity, EmissionResultEntity,
        //     TransportModeEntity, SettingEntity mengikuti schema.sql.
    }
}
```

Catatan penting:

- Npgsql memetakan `DateTime` ke `timestamptz` dan **mewajibkan Kind = Utc**.
  Simpan `trip.TripDate.ToUniversalTime()`; saat membaca, ubah lagi ke lokal
  dengan `.ToLocalTime()`.
- Username dinormalisasi `trim().ToLowerInvariant()` sebelum simpan/cari.
- ID dibuat di C# (`Guid.NewGuid()`) atau di database; pilih salah satu. Entity
  di atas mengasumsikan C# mengisi ID. Kalau memakai `fn_save_trip`, ID trip
  dibuat database (lihat catatan di bagian 6).

## 4. PostgresAuthService : IAuthService

Method yang wajib ada:

| Method | Query/SQL | Catatan |
|---|---|---|
| `Login(username, password)` | `fn_get_user_for_login` atau query `users` | verifikasi PBKDF2, isi `CurrentUserId` + `CurrentUserFullName` |
| `Register(fullName, username, password)` | `INSERT INTO users` / `fn_register_user` | hash + salt baru, tangani username duplikat |
| `Logout()` | — | kosongkan `CurrentUserId` dan `CurrentUserFullName` |

Contoh inti login (sama persis dengan algoritma `FakeAuthService`):

```csharp
public bool Login(string username, string password)
{
    using var db = _factory.CreateDbContext();

    var user = db.Users
        .AsNoTracking()
        .SingleOrDefault(u => u.Username == Normalize(username));

    if (user is null)
        return false;

    byte[] attempt = Rfc2898DeriveBytes.Pbkdf2(
        password, user.PasswordSalt, user.PasswordIterations,
        HashAlgorithmName.SHA256, 32);

    if (!CryptographicOperations.FixedTimeEquals(attempt, user.PasswordHash))
        return false;

    CurrentUserId = user.Id;
    CurrentUserFullName = user.FullName;
    return true;
}
```

Contoh register:

```csharp
public bool Register(string fullName, string username, string password)
{
    byte[] salt = RandomNumberGenerator.GetBytes(16);
    byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
        password, salt, 100_000, HashAlgorithmName.SHA256, 32);

    using var db = _factory.CreateDbContext();
    db.Users.Add(new UserEntity
    {
        Id = Guid.NewGuid(),
        FullName = fullName.Trim(),
        Username = Normalize(username),
        PasswordSalt = salt,
        PasswordHash = hash,
        PasswordIterations = 100_000,
        PasswordAlgorithm = "pbkdf2-sha256",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    });

    try
    {
        db.SaveChanges();
        return true;
    }
    catch (DbUpdateException) when (IsUniqueViolation(...))
    {
        return false; // username sudah dipakai
    }
}
```

Trigger `trg_users_default_settings` akan otomatis membuat baris
`settings` dengan `monthly_target_kg = '15.00'`, jadi tidak perlu insert manual.

## 5. PostgresTransportModeProvider : ITransportModeProvider

| Method | Query |
|---|---|
| `GetAll()` | `SELECT * FROM transport_modes WHERE is_active ORDER BY sort_order` |
| `GetBaseline()` | `SELECT * FROM transport_modes WHERE is_baseline AND is_active LIMIT 1` |

Konversi `TransportModeEntity` → `TransportMode` (nama, ikon, faktor emisi).
Boleh di-cache di memori karena master data jarang berubah.

## 6. PostgresTripHistoryRepository : ITripHistoryRepository

| Method | Rencana implementasi |
|---|---|
| `GetHistory()` | ambil `settings` + semua trip milik `CurrentUserId`, lalu map ke `TripHistory` |
| `AddTrip(Trip trip)` | panggil `fn_save_trip(..., p_chosen_mode_id)` **atau** insert EF Core dalam satu transaksi |
| `SetMonthlyTarget(double kg)` | `SELECT fn_set_monthly_target(...)` |
| `ClearTrips()` | `SELECT fn_clear_trips(...)` |

### GetHistory — mapping ke model Core

```csharp
public TripHistory GetHistory()
{
    Guid userId = RequireCurrentUserId();

    using var db = _factory.CreateDbContext();

    var settings = db.Settings.AsNoTracking()
        .SingleOrDefault(s => s.UserId == userId && s.Key == "monthly_target_kg");

    var history = new TripHistory
    {
        // settings.Value bertipe teks; parse ke angka.
        MonthlyTargetKg = settings is null
            ? 0
            : double.Parse(settings.Value, CultureInfo.InvariantCulture)
    };

    var trips = db.Trips
        .AsNoTracking()
        .Include(t => t.Route)
        .Include(t => t.Results).ThenInclude(r => r.TransportMode)
        .Where(t => t.UserId == userId)
        .OrderBy(t => t.TripDate)
        .ToList();

    foreach (var entity in trips)
    {
        var trip = new Trip
        {
            Id = entity.Id,
            TripDate = entity.TripDate.ToLocalTime(),
            ChosenModeId = entity.ChosenModeId,
            Route = new Route
            {
                Id = entity.Route.Id,
                Origin = entity.Route.Origin,
                Destination = entity.Route.Destination,
                DistanceKm = (double)entity.Route.DistanceKm,
                DurationMin = entity.Route.DurationMin,
            },
        };

        foreach (var result in entity.Results)
        {
            trip.AddResult(new EmissionResult
            {
                Id = result.Id,
                Mode = MapMode(result.TransportMode),
                TotalEmissionKg = (double)result.TotalEmissionKg,
            });
        }

        history.AddTrip(trip);
    }

    return history;
}
```

Catatan: `Trip.Results` read-only sehingga harus lewat `AddResult`. `Id` dan
`Route` bisa di-set lewat object initializer karena `init`/`set`.

### AddTrip — dua pilihan

**Pilihan A (disarankan untuk konsistensi ID): EF Core transaction.**

```csharp
public void AddTrip(Trip trip)
{
    using var db = _factory.CreateDbContext();
    using var tx = db.Database.BeginTransaction();

    var tripEntity = new TripEntity { /* Id = trip.Id, ... */ };
    // simpan route + hasil emisi, lalu db.SaveChanges();
    // tx.Commit();
}
```

**Pilihan B: panggil function SQL `fn_save_trip`.**

```csharp
var resultsJson = JsonSerializer.Serialize(trip.Results.Select(r => new
{
    transport_mode_id = r.Mode.Id,
    total_emission_kg = r.TotalEmissionKg,
    emission_factor = r.Mode.EmissionFactor,
}));

db.Database.ExecuteSqlInterpolated($"""
    SELECT fn_save_trip(
        {userId}, {trip.Route.Origin}, {trip.Route.Destination},
        {(decimal)trip.Route.DistanceKm}, {trip.Route.DurationMin},
        {resultsJson}::jsonb, {trip.TripDate.ToUniversalTime()},
        {trip.ChosenModeId});
    """);
```

Pilihan B lebih ringkas dan atomik, tetapi ID trip/rute/hasil dibuat oleh
database. Kalau ID dari objek `Trip` harus dipertahankan, pilih A atau tambahkan
parameter ID opsional ke `fn_save_trip`.

### SetMonthlyTarget / ClearTrips

```csharp
public void SetMonthlyTarget(double targetKg)
{
    Guid userId = RequireCurrentUserId();
    using var db = _factory.CreateDbContext();
    db.Database.ExecuteSqlInterpolated(
        $"SELECT fn_set_monthly_target({userId}, {(decimal)targetKg})");
}

public void ClearTrips()
{
    Guid userId = RequireCurrentUserId();
    using var db = _factory.CreateDbContext();
    db.Database.ExecuteSqlInterpolated($"SELECT fn_clear_trips({userId})");
}
```

## 7. Factory DbContext dan wiring di App

Supaya `DbContext` tidak hidup terlalu lama (data basi / change tracker besar),
gunakan factory:

```csharp
// App.xaml.cs, OnStartup
var settings = ApiSettingsLoader.Load();
var options = new DbContextOptionsBuilder<CrouteDbContext>()
    .UseNpgsql(settings.Database.ConnectionString)
    .Options;

var factory = new PooledDbContextFactory<CrouteDbContext>(options);

_auth = new PostgresAuthService(factory);
_modes = new PostgresTransportModeProvider(factory);
_history = new PostgresTripHistoryRepository(factory, _auth);
_dialogs = new DialogService();
_windows = new WindowService(CreateLoginViewModel, CreateRegisterViewModel, CreateMainViewModel);

// urutan penting: auth & modes sudah siap sebelum history dipakai
_windows.ShowLogin();
```

`PooledDbContextFactory<T>` ada di package `Microsoft.EntityFrameworkCore`
(sudah menjadi dependency EF Core). Kalau tidak ingin pooling, buat
`new CrouteDbContext(options)` di setiap method repository.

## 8. Function statistik (opsional, tahap 2)

Saat ini `DashboardViewModel` dan `StatsViewModel` memuat seluruh riwayat lalu
menghitung di C#. Itu tetap benar dan paling sedikit perubahan. Kalau ingin
memakai function SQL yang sudah disiapkan, buat interface baru:

```csharp
public interface IStatsService
{
    double GetCurrentMonthTotal();
    double GetMonthlyProgress();
    double GetTotalSaved();
    double GetAveragePerTrip();
    long GetTripCount();
    FavoriteMode? GetFavoriteMode();
    IReadOnlyList<DailyEmission> GetDailyEmissions(int days = 7);
    IReadOnlyList<TripSummary> GetRecentTrips(int limit = 3);
    double GetMonthlyTarget();
    void SetMonthlyTarget(double kg);
}
```

Implementasi memakai `Database.SqlQuery<T>` (EF Core 8+) untuk function yang
mengembalikan tabel:

```csharp
var bars = db.Database
    .SqlQuery<DailyEmission>($"SELECT * FROM fn_daily_emissions({userId}, {days})")
    .ToList();
```

Pemetaan:

| Method | Function |
|---|---|
| `GetCurrentMonthTotal` | `fn_current_month_total` |
| `GetMonthlyProgress` | `fn_monthly_progress` |
| `GetTotalSaved` | `fn_total_saved` |
| `GetAveragePerTrip` | `fn_average_per_trip` |
| `GetTripCount` | `fn_trip_count` |
| `GetFavoriteMode` | `fn_favorite_mode` |
| `GetDailyEmissions` | `fn_daily_emissions` |
| `GetRecentTrips` | `fn_recent_trips` |
| `GetMonthlyTarget` | `fn_get_monthly_target` |
| `SetMonthlyTarget` | `fn_set_monthly_target` |

Setelah `IStatsService` ada, `DashboardViewModel` dan `StatsViewModel` bisa
diubah agar memanggil service tersebut, bukan `GetHistory()` berulang kali.

## 9. Migration / inisialisasi database

Karena `schema.sql` dan `functions.sql` sudah menjadi sumber kebenaran, ada dua
pilihan:

### Pilihan A — jalankan skrip SQL (paling sederhana)

```powershell
psql $env:CROUTE_DB_CONNECTION -f docs/database/schema.sql
psql $env:CROUTE_DB_CONNECTION -f docs/database/functions.sql
```

Cocok untuk proyek ini dan memudahkan review skema.

### Pilihan B — EF Core migration

1. Buat entity + `CrouteDbContext`.
2. Tambahkan design-time factory:

```csharp
public sealed class CrouteDbContextFactory : IDesignTimeDbContextFactory<CrouteDbContext>
{
    public CrouteDbContext CreateDbContext(string[] args)
    {
        string cs = Environment.GetEnvironmentVariable("CROUTE_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=croute;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<CrouteDbContext>()
            .UseNpgsql(cs)
            .Options;

        return new CrouteDbContext(options);
    }
}
```

3. Jalankan:

```powershell
dotnet ef migrations add InitialCreate --project CRoute.Data
dotnet ef database update --project CRoute.Data
```

4. Masukkan view/function ke migration berikutnya:

```csharp
migrationBuilder.Sql(File.ReadAllText("../docs/database/functions.sql"));
```

Jangan jalankan `schema.sql` **dan** migration untuk tabel yang sama, nanti
terjadi duplikasi/konflik. Pilih salah satu.

## 10. Urutan implementasi yang disarankan

1. Jalankan `schema.sql` + `functions.sql` di database lokal.
2. Tambahkan package Npgsql + connection string.
3. Buat entity dan `CrouteDbContext` + konfigurasi kolom.
4. Implementasikan `PostgresAuthService`.
5. Implementasikan `PostgresTransportModeProvider`.
6. Implementasikan `PostgresTripHistoryRepository`.
7. Ganti fake di `App.xaml.cs` dengan implementasi PostgreSQL.
8. Uji: register → login → cek rute → simpan → riwayat → target → statistik.
9. (Opsional) Implementasikan `IStatsService` dan refactor Dashboard/Stats.
10. (Opsional) Tambahkan test integrasi memakai database test / Testcontainers.

## 11. Checklist pengujian

- Register username baru berhasil; register username yang sama gagal.
- Login password salah gagal; password benar berhasil.
- Login case-insensitive terhadap username (`Demo` = `demo`).
- Simpan trip: 1 baris `trips`, 1 baris `routes`, N baris `emission_results`,
  dan `trips.chosen_mode_id` = moda yang dipilih pengguna.
- Simpan dengan `chosen_mode_id` yang tidak ada di hasil emisi harus ditolak.
- Simpan tanpa `chosen_mode_id` (klien lama) memakai opsi terbaik sebagai fallback.
- Hapus semua trip: trip/rute/hasil hilang, target bulanan tetap.
- Emisi bulan ini hanya menghitung bulan berjalan (Asia/Jakarta).
- Grafik 7 hari menampilkan hari kosong dengan nilai 0.
- Moda favorit dihitung dari moda terpilih tiap trip; data lama memakai opsi terbaik.
- Mengubah faktor emisi master tidak mengubah riwayat lama (snapshot).
- Dua pengguna berbeda tidak bisa melihat riwayat satu sama lain.
