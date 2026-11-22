# Integrasi API Eksternal

Dokumen ini menjelaskan integrasi CRoute dengan layanan pihak ketiga: Google
Maps (jarak dan peta) dan Climatiq (faktor emisi). Isinya mencakup konfigurasi,
kondisi kode saat ini, dan langkah mengganti implementasi fake dengan API nyata.

> **Legenda status**
> ✅ Sudah berjalan · ⚠️ Sebagian (ada, tapi belum lengkap/belum real) · ❌ Belum

## 1. Ringkasan status

| Integrasi | Dipakai di aplikasi | Status |
|---|---|---|
| Google Maps Distance Matrix (jarak dan durasi) | Belum. Aplikasi memakai `FakeRouteService` | ⚠️ request sudah dibangun, parsing response masih TODO (jarak 0) |
| Google Maps Embed (peta rute) | Belum ada di View | ❌ |
| Climatiq (faktor emisi) | Belum. Aplikasi memakai `FakeEmissionService` | ❌ body request masih placeholder, parsing TODO, belum dipanggil |
| Perhitungan emisi lokal (`TransportMode.CalculateEmission`) | Ya | ✅ |

Akibatnya, jarak yang tampil di Cek Rute berasal dari simulasi
(`FakeRouteService`), bukan dari Google Maps. Faktor emisi yang dipakai
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
| `GoogleMaps:DistanceMatrixBaseUrl` | `https://maps.googleapis.com/maps/api/distancematrix/json` |
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

## 3. Google Maps Distance Matrix

### 3.1 Tujuan

Mengisi `Route.DistanceKm` dan `Route.DurationMin` dari dua teks (asal dan tujuan).
Hasilnya dipakai untuk menghitung emisi setiap moda: `E = jarak × faktor`.

### 3.2 Kondisi kode saat ini (`CRoute.Services/RouteService.cs`)

- ✅ `EnsureConfigured()` menolak key kosong/placeholder `GANTI_DENGAN...` dan
  `DistanceMatrixBaseUrl` kosong, dengan pesan yang menunjuk ke `appsettings.json`.
- ✅ `BuildRequestUri()` membentuk URL dengan parameter `origins`,
  `destinations`, dan `key`, masing-masing di-encode dengan
  `Uri.EscapeDataString`.
- ✅ Request dikirim dengan `HttpClient.GetAsync`, lalu `EnsureSuccessStatusCode()`.
- ❌ `ParseRoute()` **belum diimplementasikan**. Method ini selalu mengembalikan
  `DistanceKm = 0` dan `DurationMin = 0`. Kalau `RouteService` dipasang sekarang,
  semua emisi akan 0 dan rekomendasi tidak berarti.
- ❌ Status di dalam JSON belum dicek. Distance Matrix dapat mengembalikan HTTP 200
  dengan `status` seperti `REQUEST_DENIED` atau `OVER_QUERY_LIMIT`. Kondisi ini
  harus diubah menjadi error, bukan jarak 0.

### 3.3 Format response yang diharapkan

Berdasarkan dokumentasi resmi Distance Matrix (diakses saat penulisan):

```json
{
  "destination_addresses": ["..."],
  "origin_addresses": ["..."],
  "rows": [
    {
      "elements": [
        {
          "status": "OK",
          "duration": { "text": "25 mins", "value": 1500 },
          "distance": { "text": "10.2 km", "value": 10200 }
        }
      ]
    }
  ],
  "status": "OK"
}
```

Konversi yang perlu dilakukan:

- `rows[0].elements[0].distance.value` (meter) → `DistanceKm = value / 1000.0`
- `rows[0].elements[0].duration.value` (detik) → `DurationMin = value / 60`
- Periksa `status` teratas dan `rows[0].elements[0].status` sebelum membaca nilainya.
  Nilai `OK` menandakan sukses.

### 3.4 Catatan penting: API ini sudah legacy

Halaman resmi Google (diakses saat penulisan) menyatakan:

> Distance Matrix API (Legacy) sejak Oktober 2026 tidak bisa diaktifkan di
> proyek Google Cloud baru. Proyek yang sudah ada tetap didukung. Google
> menyarankan migrasi ke Routes API.

Implikasinya: jika kelompok membuat proyek Google Cloud baru, API ini mungkin
tidak bisa diaktifkan. Pilihan yang perlu diputuskan tim:

1. Tetap memakai Distance Matrix (legacy), dengan proyek Google Cloud yang sudah
   ada. Kode yang ada tidak perlu banyak diubah.
2. Pindah ke Routes API (Compute Route Matrix). Perlu menulis ulang
   `BuildRequestUri`, header, dan parsing. Konfigurasi `DistanceMatrixBaseUrl`
   juga perlu diganti.

Sampai keputusan diambil, status ini tetap ⚠️.

### 3.5 Hal yang belum diputuskan

- **Mode perjalanan.** `BuildRequestUri` tidak mengirim parameter `mode`, sehingga
  Google memakai default (`driving`). Desain CRoute memakai **satu jarak untuk
  semua moda** (lihat `perhitungan-emisi.md`), jadi jarak mengemudi dipakai juga
  untuk motor, bus, kereta, sepeda, dan jalan kaki. Ini perlu diakui sebagai
  batasan atau diganti dengan jarak per moda.
- **Durasi per moda.** Durasi juga hanya dari satu mode.

## 4. Google Maps Embed (peta)

- Endpoint: `GoogleMaps:EmbedBaseUrl`.
- ❌ Belum dipakai. Paket `Microsoft.Web.WebView2` (1.0.4191.47) sudah terpasang di
  `CRoute.App`, tetapi belum ada kontrol WebView2 di `CariRuteView`.
- Rencana: menampilkan peta rute di Cek Rute. Parameter URL dan key perlu
  diverifikasi sebelum implementasi, karena Embed API juga membutuhkan API key.
- Embed dan Distance Matrix adalah dua layanan terpisah. Keduanya memakai
  kunci Google Maps Platform, tetapi konfigurasi dan kodenya independen.

## 5. Climatiq (faktor emisi)

### 5.1 Tujuan

Mengambil faktor emisi resmi untuk tiap moda, sebagai pengganti nilai tetap di
master `transport_modes`. Saat ini nilai tetap tersebut berasal dari Modul 2.

### 5.2 Kondisi kode saat ini (`CRoute.Services/EmissionService.cs`)

- ✅ `Calculate(route, mode)` menghitung emisi secara lokal dan tidak memanggil
  API. Method ini dipakai untuk semua perhitungan saat ini.
- ⚠️ `RefreshEmissionFactorAsync(mode)` sudah membentuk request `POST` dengan header
  `Authorization: Bearer <key>`, tetapi:
  - **Body request salah.** Kodenya mengirim `{ "mode": "<nama moda>" }`. Climatiq
    tidak menerima format ini, sehingga request akan gagal.
  - **Parsing response belum ada** (`TODO`). Hasil API dibaca lalu dibuang.
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

Composition root ada di `CRoute.App/App.xaml.cs`. Saat ini `IRouteService` dan
`IEmissionService` memakai implementasi fake. Untuk beralih, ganti dua
inisialisasi tersebut. Contoh (sesuaikan nama variabel dengan `App.xaml.cs`):

```csharp
_routeService = new RouteService(new HttpClient(), settings);
_emissionService = new EmissionService(new HttpClient(), settings);
```

**Jangan lakukan ini sebelum `ParseRoute()` selesai.** Jika tidak, jarak selalu 0
dan semua hasil emisi 0.

Hal yang sebaiknya dilakukan sekaligus:

- Gunakan satu `HttpClient` yang dibagikan (singleton), bukan `new HttpClient()`
  per pemanggilan. Pola ini menghindari kehabisan socket.
- Pastikan `CariRuteViewModel.HitungAsync` menampilkan pesan error yang jelas
  (sudah ada: `"Gagal mengambil rute: ..."`).
- Setelah diganti, uji dengan dua kasus: alamat valid dan key salah.

## 7. Penanganan error dan keamanan

| Situasi | Perilaku saat ini | Yang diharapkan |
|---|---|---|
| Key kosong atau placeholder | `InvalidOperationException` dengan pesan jelas | Sudah sesuai, tetapi placeholder `ISI_...` belum terdeteksi |
| HTTP non-2xx | `EnsureSuccessStatusCode()` melempar exception | Sudah sesuai, pesan perlu ramah pengguna |
| Distance Matrix mengembalikan `status` error dengan HTTP 200 | Belum dicek. Jarak 0 | Dilempar sebagai error |
| Alamat tidak ditemukan (`ZERO_RESULTS`) | Belum dicek | Pesan "rute tidak ditemukan" |
| Climatiq gagal | Method belum dipanggil | Fallback ke nilai lokal dengan pemberitahuan |

Keamanan:

- Key Google dan Climatiq terbaca dari konfigurasi. Jangan commit key asli.
- Key Google yang dipakai di URL query bisa tercatat di log jika logging HTTP
  diaktifkan. Hindari logging URL request.

## 8. Rujukan

- [`perhitungan-emisi.md`](perhitungan-emisi.md): rumus dan faktor emisi lokal.
- [`arsitektur.md`](arsitektur.md): posisi layanan ini dalam solusi.
- [Distance Matrix API (Legacy) — dokumentasi resmi](https://developers.google.com/maps/documentation/distance-matrix/overview)
- [Routes API — migrasi](https://developers.google.com/maps/documentation/routes/migrate-routes)
- [Climatiq — Estimate API](https://www.climatiq.io/docs/api-reference/estimate)
