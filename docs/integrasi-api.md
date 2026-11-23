# Integrasi API Eksternal

Dokumen ini menjelaskan integrasi CRoute dengan layanan pihak ketiga: Google
Maps (jarak dan peta) dan Climatiq (faktor emisi, ditunda). Isinya mencakup
konfigurasi, kondisi kode saat ini, dan langkah mengganti implementasi simulasi
dengan API nyata.

> **Legenda status**
> ✅ Sudah berjalan · ⚠️ Sebagian (ada, tapi belum lengkap/belum real) · ❌ Belum

## 1. Ringkasan status

| Integrasi | Dipakai di aplikasi | Status |
|---|---|---|
| Google Maps Routes API (jarak dan durasi) | Ya, kalau `GoogleMaps:ApiKey` diisi. Tanpa key, otomatis jatuh ke `FakeRouteService` | ✅ |
| Google Maps Embed (peta rute) | Belum ada di View | ❌ |
| Climatiq (faktor emisi) | Belum, **ditunda**. Aplikasi memakai `LocalEmissionService` dengan faktor dari `transport_modes` | ❌ ditunda; body request masih placeholder dan parsing belum ada |
| Perhitungan emisi lokal (`LocalEmissionService` + `TransportMode.CalculateEmission`) | Ya | ✅ |

Jarak dan durasi di Cek Rute berasal dari Google Maps selama API key sudah
diisi di `appsettings.local.json`; tanpa key, aplikasi tetap jalan dengan
simulasi `FakeRouteService`. Faktor emisi yang dipakai
adalah nilai tetap dari master `transport_modes` (lihat
[`perhitungan-emisi.md`](perhitungan-emisi.md)).

## 2. Konfigurasi

Kredensial dan endpoint dibaca dari `CRoute.Services/appsettings.json` lewat
`ApiSettingsLoader`. Urutan pembacaan (nilai terakhir menang):

1. `appsettings.json`
2. `appsettings.local.json` (opsional, sudah masuk `.gitignore`)
3. Environment variable berawalan `CROUTE_` (mis. `CROUTE_GoogleMaps__ApiKey`)

| Kunci | Fungsi |
|---|---|
| `GoogleMaps:ApiKey` | API key Google Maps Platform |
| `GoogleMaps:RoutesBaseUrl` | `https://routes.googleapis.com/directions/v2:computeRoutes` (nilai bawaan kalau tidak diisi) |
| `GoogleMaps:EmbedBaseUrl` | `https://www.google.com/maps/embed/v1/directions` |
| `Climatiq:ApiKey` | API key Climatiq |
| `Climatiq:BaseUrl` | `https://api.climatiq.io/data/v1/estimate` |
| `Database:ConnectionString` | Koneksi PostgreSQL/Supabase |

Catatan yang perlu ditindaklanjuti:

- **Placeholder sudah diseragamkan** (22 November 2026). Semua berkas memakai
  awalan `GANTI_DENGAN_...`, termasuk `appsettings.example.json`. Pengecekan di
  `RouteService` dan `EmissionService` mengenali awalan itu. `ApiSettingsLoader`
  juga mengosongkan connection string yang masih berupa placeholder supaya
  aplikasi menampilkan pesan "belum diatur".
- **`appsettings.json` masih ter-track di git**, tetapi sekarang hanya berisi
  placeholder. Nilai rahasia harus lewat `appsettings.local.json` atau
  environment variable `CROUTE_*`.
- Jangan menulis API key asli di berkas yang di-commit.

## 3. Google Maps Routes API (jarak dan durasi)

### 3.1 Tujuan

Mengisi `Route.DistanceKm` dan `Route.DurationMin` dari dua teks (asal dan tujuan).
Hasilnya dipakai untuk menghitung emisi setiap moda: `E = jarak × faktor`.

### 3.2 Kenapa Routes API, bukan Distance Matrix

Distance Matrix API (Legacy) sejak Oktober 2026 tidak bisa diaktifkan di proyek
Google Cloud baru, dan Google menyarankan migrasi ke Routes API. Tim memilih
opsi pindah ke Routes API (`computeRoutes`), karena satu permintaan memang hanya
butuh satu asal dan satu tujuan. Sudah diuji dengan Maps Demo Key:
"Malioboro, Yogyakarta" → "Universitas Gadjah Mada, Yogyakarta" = 4,7 km, ±16 menit.

### 3.3 Kondisi kode (`CRoute.Services/RouteService.cs`)

- ✅ `EnsureConfigured()` menolak key kosong atau placeholder (`GANTI_DENGAN_...`,
  `ISI_...`) lewat `GoogleMapsSettings.HasApiKey`, tanpa memanggil HTTP.
- ✅ Request: `POST {RoutesBaseUrl}` dengan header `X-Goog-Api-Key` dan
  `X-Goog-FieldMask: routes.distanceMeters,routes.duration` (field mask wajib).
  Key dikirim lewat header, bukan query string, jadi tidak ikut tercatat di log URL.
- ✅ Body: `origin.address`, `destination.address`, `travelMode: "DRIVE"`,
  `languageCode: "id"`, `regionCode: "id"`. Tanpa `routingPreference`, sehingga
  default `TRAFFIC_UNAWARE` dipakai dan permintaan ditagih sebagai SKU
  **Compute Routes Essentials** (kuota gratis paling besar).
- ✅ `ParseRoute()` membaca `routes[0].distanceMeters` (meter → km) dan
  `routes[0].duration` (string detik seperti `"1500s"`, boleh pecahan → menit,
  dibulatkan).
- ✅ Diuji di `CRoute.Tests/Services/RouteServiceTests.cs`.

### 3.4 Format request dan response

Request:

```json
{
  "origin": { "address": "Malioboro, Yogyakarta" },
  "destination": { "address": "Universitas Gadjah Mada, Yogyakarta" },
  "travelMode": "DRIVE",
  "languageCode": "id",
  "regionCode": "id"
}
```

Response sukses:

```json
{ "routes": [ { "distanceMeters": 4700, "duration": "929s" } ] }
```

Catatan dari dokumentasi resmi:

- Kalau rute tidak ditemukan, array `routes` kosong (atau tidak dikirim).
- Field bernilai default (0) boleh tidak dikirim, jadi field yang hilang dianggap 0.
- Alamat yang ambigu bisa salah ditebak Google. Tulis alamat lengkap dengan nama
  kota (mis. "Malioboro, Yogyakarta", bukan "Malioboro").

### 3.5 Biaya dan API key

- SKU **Compute Routes Essentials**: gratis 10.000 permintaan per bulan, lalu
  ± USD 5 per 1.000. Permintaan naik ke Pro/Enterprise hanya kalau memakai
  fitur lalu lintas, >10 waypoint, motor, tol, dsb. (tidak dipakai CRoute).
- Proyek Google Cloud biasa wajib mengaktifkan billing. Untuk pengembangan, bisa
  memakai **Maps Demo Key** (tanpa data pembayaran, ada batas harian, berhenti
  tanpa tagihan kalau batas habis). Tiap anggota sebaiknya membuat key sendiri.
- Aktifkan **Routes API** (bukan Roads API) dan batasi key hanya ke API itu.
- Simpan key di `appsettings.local.json`, jangan di `appsettings.json`.

### 3.6 Mode perjalanan (diperbarui 23 November 2026)

- Rute utama tetap memakai `travelMode: "DRIVE"` untuk Jalan kaki, Mobil,
  Motor, dan Sepeda. Ini masih menjadi batasan yang diketahui.
- **Bus dan Kereta** sekarang dicek lewat satu request `travelMode: "TRANSIT"`:
  - Bus hanya diterima jika seluruh langkah transitnya bus dan total jalan kaki
    akses/egress/transfer **≤ 2 km**.
  - Kereta hanya diterima jika seluruh langkah transitnya rail dan total jalan
    kaki **≤ 2 km**.
  - Rute campuran bus + kereta ditolak untuk kedua mode.
  - `allowedTravelModes` hanya preferensi, bukan filter keras; penyaringan
    dilakukan di aplikasi berdasarkan `transitLine.vehicle.type`.
- Jika tidak ada rute yang memenuhi syarat, Bus/Kereta tidak ditampilkan dan
  tidak dihitung sebagai emisi 0.
- Emisi Bus/Kereta memakai jarak langkah transit masing-masing, bukan jarak
  rute DRIVE. Durasi juga diambil dari rute transit.

## 4. Google Maps Embed (peta)

- Endpoint: `GoogleMaps:EmbedBaseUrl`.
- ❌ Belum dipakai. Paket `Microsoft.Web.WebView2` (1.0.4191.47) sudah terpasang di
  `CRoute.App`, tetapi belum ada kontrol WebView2 di `CariRuteView`.
- Rencana: menampilkan peta rute di Cek Rute. Parameter URL dan key perlu
  diverifikasi sebelum implementasi, karena Embed API juga membutuhkan API key.
- Embed dan Routes API adalah dua layanan terpisah. Keduanya memakai
  kunci Google Maps Platform, tetapi konfigurasi dan kodenya independen.

## 5. Climatiq (faktor emisi)

### 5.1 Tujuan dan keputusan

Mengambil faktor emisi resmi untuk tiap moda, sebagai pengganti nilai tetap di
master `transport_modes`. **Integrasi ini ditunda untuk sementara.** Faktor yang
dipakai saat ini berasal dari tabel `transport_modes` (seed Modul 2) dan
dihitung oleh `LocalEmissionService` di `CRoute.App/Services`.

### 5.2 Kondisi kode saat ini

- ✅ `LocalEmissionService.Calculate(route, mode)` menghitung emisi dari
  `TransportMode.EmissionFactor` dan tidak memanggil API. Kelas inilah yang
  dipasang di `App.xaml.cs`.
- ❌ `EmissionService` (kerangka Climatiq) **tidak dipasang** di aplikasi.
  `Calculate` di kelas itu hanya placeholder lokal, sedangkan
  `RefreshEmissionFactorAsync` belum selesai:
  - **Body request masih placeholder.** Kodenya mengirim `{ "mode": "<nama moda>" }`;
    format itu belum sesuai Climatiq.
  - **Parsing response belum ada** (`TODO`).
  - **Method belum dipanggil** dari mana pun.

### 5.3 Format request menurut Climatiq

Referensi yang diperiksa saat penulisan (hasil pencarian, halaman resmi belum
bisa diakses dari lingkungan ini) menyebut request `POST` ke
`https://api.climatiq.io/data/v1/estimate` dengan body kira-kira seperti ini:

```json
{
  "emission_factor": {
    "activity_id": "<ID faktor dari Data Explorer>",
    "data_version": "^1"
  },
  "parameters": {
    "distance": 10.2,
    "distance_unit": "km"
  }
}
```

Response memuat total emisi dalam kg CO₂e (`co2e`, `co2e_unit`).

> ⚠️ Format ini **belum diverifikasi terhadap dokumentasi resmi**. Sebelum
> implementasi, cek ulang di `climatiq.io` dan simpan contoh response asli.
> Pilih `activity_id` dari Data Explorer untuk setiap moda. Untuk kebutuhan
> Modul 2, faktor yang dipakai tetap nilai dari master `transport_modes` kecuali
> tim memutuskan lain.

### 5.4 Langkah sebelum diaktifkan

1. Verifikasi format request dan response Climatiq terhadap dokumentasi resmi.
2. Tentukan `activity_id` untuk tiap moda, lalu simpan di konfigurasi atau tabel.
3. Ganti body request di `RefreshEmissionFactorAsync` dan implementasikan parsing.
4. Tentukan apakah hasilnya disimpan ke `transport_modes.emission_factor` (perlu
   keputusan tentang perubahan skema) atau hanya dipakai saat itu.
5. Tangani error (HTTP non-2xx, key tidak valid, kuota habis) dan tampilkan pesan
   ke pengguna.

## 6. Mengganti implementasi fake dengan API nyata

Composition root ada di `CRoute.App/App.xaml.cs`.

- **Rute: sudah diganti.** `RouteService` dipakai kalau
  `settings.GoogleMaps.HasApiKey` bernilai `true`; kalau belum, aplikasi memakai
  `FakeRouteService` supaya anggota tim tanpa key tetap bisa menjalankan aplikasi.
  `HttpClient` dibuat sekali (static, timeout 15 detik) dan dibagikan.
- **Emisi: dihitung lokal.** `LocalEmissionService` memakai faktor dari tabel
  `transport_modes`. Kalau Climatiq diaktifkan nanti, ganti inisialisasi
  `_emissionService` setelah body request dan parsing selesai, dengan memakai
  `HttpClient` yang sama:

```csharp
_emissionService = new EmissionService(Http, settings);
```

`CariRuteViewModel.HitungAsync` menampilkan pesan error dengan awalan
`"Gagal mengambil rute: ..."`.

## 7. Penanganan error dan keamanan

| Situasi | Perilaku saat ini | Yang diharapkan |
|---|---|---|
| Key kosong atau placeholder (`GANTI_DENGAN_...`/`ISI_...`) | `InvalidOperationException` dengan pesan jelas, tanpa memanggil HTTP | ✅ |
| HTTP non-2xx (key tidak valid, API belum diaktifkan, kuota habis, server error) | `HttpRequestException` dengan `StatusCode` dan pesan ramah pengguna, ditambah `status`/`message` dari body error Google | ✅ |
| Tidak ada koneksi atau timeout | `HttpRequestException` "Tidak bisa terhubung ke Google Maps..." / "...timeout" | ✅ |
| Alamat tidak ditemukan (`routes` kosong) | `InvalidOperationException` "Rute ... tidak ditemukan" | ✅ |
| Climatiq gagal | Method belum dipanggil | Fallback ke nilai lokal dengan pemberitahuan |

Keamanan:

- Key Google dan Climatiq terbaca dari konfigurasi. Jangan commit key asli.
- Key Google dikirim lewat header `X-Goog-Api-Key`, bukan URL query, sehingga
  tidak ikut tercatat di log URL.

## 8. Rujukan

- [`perhitungan-emisi.md`](perhitungan-emisi.md): rumus dan faktor emisi lokal.
- [`arsitektur.md`](arsitektur.md): posisi layanan ini dalam solusi.
- [Distance Matrix API (Legacy) — dokumentasi resmi](https://developers.google.com/maps/documentation/distance-matrix/overview)
- [Routes API — migrasi](https://developers.google.com/maps/documentation/routes/migrate-routes)
- [Climatiq — Estimate API](https://www.climatiq.io/docs/api-reference/estimate)
