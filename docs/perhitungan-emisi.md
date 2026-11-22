# Perhitungan Emisi CRoute

Dokumen ini menjelaskan rumus, faktor emisi, dan cara setiap angka di aplikasi
dihitung. Semua logika ada di `CRoute.Core` (model) dan `CRoute.App/ViewModels`
(hitungan tampilan).

> **Legenda status**
> ✅ Sudah berjalan · ⚠️ Sebagian · ❌ Belum

## 1. Rumus dasar

$$
E = d \times f
$$

| Simbol | Arti | Satuan |
|---|---|---|
| `E` | Emisi CO₂ untuk satu moda | kg CO₂ |
| `d` | Jarak rute (`Route.DistanceKm`) | km |
| `f` | Faktor emisi moda (`TransportMode.EmissionFactor`) | kg CO₂/km |

Implementasi: `TransportMode.CalculateEmission(double km) => km × EmissionFactor` ✅

`EmissionService.Calculate(route, mode)` memanggil rumus ini dan mengembalikan
`EmissionResult` ✅. Perhitungannya lokal, tidak memanggil API.

## 2. Faktor emisi

Faktor berikut disimpan di tabel `transport_modes` (seed di
[`schema.sql`](database/schema.sql)) dan dibaca lewat
`PostgresTransportModeProvider` ✅.

| Moda | Ikon | Faktor (kg CO₂/km) | Baseline | Urutan |
|---|---|---:|:---:|:---:|
| Mobil | `car` | 0,171 | ✅ | 1 |
| Motor | `motorcycle` | 0,103 | | 2 |
| Bus | `bus` | 0,089 | | 3 |
| Kereta | `train` | 0,041 | | 4 |
| Sepeda | `bicycle` | 0,000 | | 5 |
| Jalan kaki | `walk` | 0,000 | | 6 |

- **Baseline** = Mobil. Dipakai sebagai pembanding untuk penghematan.
- Faktor ini berasal dari spesifikasi Modul 2 dan dianggap nilai awal.
- Faktor disimpan di database, jadi bisa diubah tanpa mengubah kode.
- Setiap hasil emisi juga menyimpan `emission_factor` sebagai *snapshot* pada
  saat perjalanan disimpan. Perubahan faktor di masa depan tidak mengubah
  riwayat lama.

### Pembaruan faktor dari Climatiq (ditunda)

`EmissionService.RefreshEmissionFactorAsync` ❌ baru berupa kerangka, belum
dipanggil, dan parsing response-nya masih TODO. Integrasi Climatiq **ditunda
untuk sementara**; faktor yang dipakai aplikasi adalah nilai seed di atas,
dibaca lewat `LocalEmissionService`. Detailnya ada di
[`integrasi-api.md`](integrasi-api.md).

## 3. Contoh perhitungan

Contoh untuk jarak **10 km**:

| Moda | Perhitungan | Emisi |
|---|---|---:|
| Mobil | 10 × 0,171 | 1,71 kg |
| Motor | 10 × 0,103 | 1,03 kg |
| Bus | 10 × 0,089 | 0,89 kg |
| Kereta | 10 × 0,041 | 0,41 kg |
| Sepeda | 10 × 0,000 | 0,00 kg |
| Jalan kaki | 10 × 0,000 | 0,00 kg |

Penghematan dibanding mobil (rumus di bagian 4):

- Bus: 1,71 − 0,89 = **0,82 kg**. Kartu "Tahukah kamu?" di Dashboard memakai
  angka yang sama (≈ 0,8 kg), tetapi **ditulis sebagai teks statis** di
  `DashboardView.xaml`, bukan dihitung ulang dari data. ⚠️
- Kereta: 1,71 − 0,41 = **1,30 kg**.

## 4. Rekomendasi dan penghematan

### 4.1 Opsi terbaik (rekomendasi)

`Trip.GetBestOption()` ✅ mengembalikan `EmissionResult` dengan
`TotalEmissionKg` paling kecil di antara moda yang dipilih pengguna.
Jika belum ada hasil, hasilnya `null`.

Rekomendasi di Cek Rute ✅ selalu berasal dari opsi ini, hanya dari moda yang
dicentang.

### 4.2 Penghematan vs baseline

```
hemat = max(0, E_mobil − E_opsi_terbaik)
```

- Implementasi: `EmissionSavings.Calculate(route, chosen, baseline)` ✅
- `E_mobil` dihitung ulang dari jarak rute dan faktor baseline. Mobil tidak
  harus dicentang oleh pengguna.
- Nilai negatif dipotong menjadi 0, misalnya jika opsi terbaik ternyata Mobil.
- Teks rekomendasi memakai "hemat X kg dibanding mobil" jika `hemat > 0`, dan
  "terendah dari moda yang kamu pilih" jika tidak ✅.

## 5. Agregasi Dashboard dan Statistik

Semua agregasi memakai **opsi terbaik per perjalanan**, bukan jumlah semua moda.
Data diambil dari `ITripHistoryRepository.GetHistory()` lalu dihitung di memori.
Bulan dan hari mengikuti zona **Asia/Jakarta** lewat `AppClock`, bukan zona waktu
komputer pengguna.

| Angka di UI | Rumus | Implementasi | Status |
|---|---|---|---|
| Emisi bulan ini | Jumlah `best(trip)` untuk trip dengan bulan dan tahun berjalan | `TripHistory.GetMonthlyTotal()` | ✅ |
| Persentase target | `GetMonthlyTotal() ÷ MonthlyTargetKg`, dikali 100 | `TripHistory.GetProgress()` | ✅ |
| Bar target (maks 100%) | `min(persen, 100)` | `StatsViewModel.Refresh` | ✅ |
| Hemat vs mobil | Jumlah `EmissionSavings.ForTrip` semua trip | `DashboardViewModel`, `StatsViewModel` | ✅ |
| Rata-rata per perjalanan | Rata-rata `best(trip)` | `StatsViewModel.Refresh` | ✅ |
| Moda favorit | Moda yang paling sering menjadi opsi terbaik | `StatsViewModel.Refresh` | ✅ |
| Grafik 7 hari | Jumlah `best(trip)` per hari, 6 hari lalu sampai hari ini | `StatsViewModel.BuildDailyBars` | ✅ |

### 5.1 Status target

Teks status memakai ambang yang sama di Dashboard dan Statistik:

| Persentase target | Teks |
|---|---|
| < 70% | Masih aman, pertahankan! |
| 70% sampai < 100% | Hampir mencapai target, pilih moda yang lebih hemat. |
| ≥ 100% | Target bulan ini sudah terlampaui. |

Teks tersebut hanya tampilan. Ambangnya ditulis langsung di ViewModel, belum
berasal dari konfigurasi.

### 5.2 Target default

- Target awal pengguna baru adalah **15,00 kg CO₂ per bulan**, diisi otomatis
  oleh trigger `trg_users_default_settings` di database ✅.
- Target bernilai 0 berarti belum diatur. Persentasenya dianggap 0 supaya tidak
  terjadi pembagian dengan nol ✅.
- Target harus lebih dari 0 saat disimpan lewat halaman Statistik ✅.

### 5.3 Grafik 7 hari

- Sumbu label memakai nama hari singkat dalam Bahasa Indonesia (`ddd`).
- Tinggi bar = `hari_ini_kg ÷ kg_hari_tertinggi`. Hari tanpa emisi diberi nilai 0.
- Nilai teks di atas bar muncul hanya jika ada emisi (`> 0`).
- "Hari ini" diambil dari `AppClock.Now.Date`, dan tanggal trip diubah ke zona
  aplikasi dengan `AppClock.ToAppTime()` sebelum dibandingkan. Batas hari jadi
  selalu Asia/Jakarta.

## 6. Versi agregasi SQL

`docs/database/functions.sql` menyediakan versi agregasi di database dengan
hasil yang setara, misalnya `fn_monthly_total`, `fn_current_month_total`,
`fn_monthly_progress`, `fn_total_saved`, `fn_average_per_trip`, dan
`fn_favorite_mode` ✅ (tersedia, belum dipakai aplikasi).

Catatan:

- Versi SQL dan versi C# sama-sama memakai zona **Asia/Jakarta**. C# memakai
  `AppClock.Now` dan `AppClock.ToAppTime()`, bukan `DateTime.Now`, sehingga
  hasilnya tidak bergantung pada zona waktu komputer. Jika zona tidak tersedia,
  `AppClock` melempar error, bukan diam-diam memakai UTC. ✅
- Diverifikasi 22 November 2026: total C# dan SQL sama, yaitu 1,7442 kg untuk
  rute uji 10,2 km dengan moda Mobil. Unit test juga memeriksa trip pada
  30 November 2026 pukul 17.00 UTC (1 Desember 2026 pukul 00.00 WIB) dan
  memastikan trip itu masuk bulan Desember, bukan November. ✅
- `PostgresTripHistoryRepository.GetMonthlyTotal()` dihapus karena tidak ada di
  `ITripHistoryRepository` dan tidak dipakai. Agregasi bulanan tetap memakai
  `TripHistory.GetMonthlyTotal()`. ✅

## 7. Batasan saat ini

- **Jarak tergantung konfigurasi.** Jika `GoogleMaps:ApiKey` diisi, jarak dan
  durasi berasal dari Google Maps Routes API. Tanpa key, aplikasi memakai
  `FakeRouteService` yang menurunkan jarak 3,0 sampai 24,9 km dari teks asal dan
  tujuan. Angka simulasi itu tidak sesuai kondisi sebenarnya. Lihat
  [`integrasi-api.md`](integrasi-api.md). ⚠️
- **Faktor belum diperbarui dari Climatiq (ditunda).** Nilai yang dipakai
  adalah seed `transport_modes`. ❌
- **Jumlah penumpang tidak diperhitungkan.** Emisi dihitung untuk satu
  orang, tanpa `passengerCount`. ❌
- **Moda yang tidak dipilih tidak ikut dibandingkan.** Hasil hanya untuk
  moda yang dicentang saat Cek Rute. ✅
- **Hasil belum disimpan tidak masuk statistik.** Hanya trip yang sudah
  disimpan yang dihitung. ✅
