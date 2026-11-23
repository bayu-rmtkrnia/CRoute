# Perencanaan Backend dan Pengujian

Dokumen ini adalah rencana kerja Ahmad Maulana Ibrahim (Back End) setelah
pembagian tugas disepakati:

| Anggota | Peran | Pekerjaan utama |
|---|---|---|
| Bayu Rahmat Kurnia | Software Architect | Integrasi Google Maps dan Climatiq, web API tambahan (opsional), kemasan `.exe` |
| Ahmad Maulana Ibrahim | Back End | Database, konfigurasi, verifikasi repository, unit test, black box test |
| Sukmawati | Front End | Dashboard, visualisasi, screenshot panduan pengguna |

Google Maps dan Climatiq dikerjakan Bayu, jadi rencana ini **tidak** mencakup
`RouteService.ParseRoute`, body request Climatiq, dan penggantian fake di
`App.xaml.cs`. Ahmad menyiapkan database, konfigurasi, dan pengujian yang
menopang pekerjaan itu.

> **Legenda status**
> ✅ Sudah selesai · ⚠️ Sebagian · ❌ Belum

## Status repo saat rencana disusun

Diperiksa pada 22 November 2026:

- Branch `dev` berada di commit `7947766` ("Keluarkan bin/ dan obj/ dari
  tracking Git"). Tidak ada branch lain yang lebih baru; `main` masih di
  `c4b79d0`. Semua pekerjaan di bawah memakai versi `dev` ini.
- Perubahan dokumentasi di working tree belum di-commit.
- Environment variable `CROUTE_Database__ConnectionString` **tidak ditemukan**
  di scope User, Machine, maupun Process.
- `CRoute.Services/appsettings.local.json` **tidak ada**.
- `CRoute.Services/appsettings.json` masih berisi placeholder API dan
  connection string lokal `postgres`/`postgres`.

Artinya, langkah pertama bukan membuat database dari nol, tetapi memastikan
connection string Supabase benar-benar tersimpan permanen dan bisa dibaca dari
terminal baru.

## 1. Tujuan

1. Database Supabase siap dipakai dan sudah divalidasi terhadap `schema.sql`
   dan `functions.sql`.
2. Kredensial tidak lagi tersimpan di berkas yang di-commit.
3. Alur PostgreSQL teruji end-to-end: login, register, simpan riwayat, lihat
   riwayat, atur target, dan hapus riwayat.
4. Perhitungan bulanan konsisten antara aplikasi dan SQL.
5. Ada bukti pengujian: unit test untuk logika backend dan black box test untuk
   alur aplikasi (format Lab 10.2).

## 2. Ruang lingkup

| Termasuk (Ahmad) | Di luar ruang lingkup (pemegang lain) |
|---|---|
| Setup dan validasi database Supabase | `RouteService.ParseRoute` dan integrasi Google Maps (Bayu) |
| Pemisahan kredensial dan placeholder | Integrasi Climatiq dan `EmissionService` (Bayu) |
| Verifikasi repository PostgreSQL | Mengganti fake di `App.xaml.cs` (Bayu) |
| Konsistensi zona waktu | Kartu Dashboard dan visualisasi (Sukmawati) |
| Proyek `CRoute.Tests` dan unit test | Paket `.exe`, panduan instalasi, panduan konfigurasi (Bayu) |
| Dokumen dan pelaksanaan black box test | Screenshot panduan pengguna (Sukmawati) |
| Update status di `docs/database` dan `docs/perhitungan-emisi.md` | `docs/integrasi-api.md` (Bayu) |

## 3. Kondisi dan progres

Rujukan lengkap ada di [`integrasi-api.md`](integrasi-api.md),
[`perhitungan-emisi.md`](perhitungan-emisi.md), dan
[`database/supabase.md`](database/supabase.md).

| Item | Status | Catatan |
|---|---|---|
| `PostgresAuthService`, `PostgresTransportModeProvider`, `PostgresTripHistoryRepository` | ✅ | Diuji end-to-end: register, login, simpan/baca/hapus trip, target desimal. |
| `schema.sql` dan `functions.sql` | ✅ | 6 tabel, 19 function, dan 6 moda terverifikasi di Supabase. |
| Connection string Supabase di env lokal | ✅ | Tersimpan di scope User, terbaca dari proses baru; memakai session pooler `aws-0`. |
| Pemisahan kredensial | ✅ | `appsettings.json` hanya placeholder; rahasia lewat env `CROUTE_*`. |
| Placeholder `ISI_...` vs `GANTI_DENGAN...` | ✅ | Semua memakai `GANTI_DENGAN_...`; `ApiSettingsLoader` mengosongkan placeholder. |
| Zona waktu | ✅ | `AppClock` (Asia/Jakarta) dipakai `TripHistory`, `StatsViewModel`, dan `DashboardViewModel`. |
| `PostgresTripHistoryRepository.GetMonthlyTotal()` | ✅ | Dihapus karena tidak dipakai; agregasi memakai `TripHistory.GetMonthlyTotal()`. |
| Proyek test | ✅ | `CRoute.Tests`: 67 lulus, 0 di-skip. |
| Dokumen black box | ✅ | 15 kasus; BB-01–BB-06 sudah dijalankan dan bernilai **B**. |

### Progres 22 November 2026

- [x] Connection string Supabase tersimpan di environment variable scope User
      dan terbaca dari proses baru.
- [x] `schema.sql` dan `functions.sql` dijalankan ke Supabase: 6 tabel,
      19 function, dan 6 moda.
- [x] Role `croute` dibuat dan grant diberikan; koneksi sebagai `croute` berhasil.
- [x] Login `demo` / `demo123` berhasil lewat `PostgresAuthService`.
- [x] Simpan, baca, ubah target, dan hapus semua trip berhasil lewat
      `PostgresTripHistoryRepository`. Total bulanan C# dan SQL sama
      (1,7442 kg untuk rute uji 10,2 km dengan moda Mobil).
- [x] `CRoute.Tests` dibuat; 67 test lulus, 0 di-skip. Termasuk test
      `RouteService` (Routes API), `TransitRouteService` (filter bus/kereta),
      `Trip.GetChosenOption`/fallback, 3 test `AppClock`, dan validasi password
      `PostgresAuthService`.
- [x] `docs/pengujian-black-box.md` dibuat dengan 15 kasus.
- [x] Placeholder `appsettings.json` dan `appsettings.example.json` diseragamkan.
- [x] BB-01–BB-06 dijalankan otomatis lewat Windows UI Automation dan
      menghasilkan **B**. BB-07–BB-10 menunggu API key Google Maps asli;
      BB-11–BB-15 bisa dijalankan dengan data uji.
- [x] Zona waktu aplikasi disamakan ke Asia/Jakarta lewat `AppClock`.
- [x] Integrasi `RouteService` ke Google Maps Routes API (Bayu).
- [ ] Integrasi `EmissionService` ke Climatiq (Bayu).

Catatan koneksi: session pooler `aws-0-ap-northeast-2` berhasil setelah role
`croute` dibuat. Sebelum role itu ada, pooler mengembalikan
`tenant/user ... not found`. Koneksi langsung `db.<project-ref>.supabase.co`
juga berhasil lewat IPv6. Environment variable sekarang memakai session pooler
dengan username `croute.<project-ref>`.

## 4. Keputusan yang harus diambil dulu

| No | Keputusan | Pilihan | Rekomendasi |
|---|---|---|---|
| D1 | Database untuk pengujian | Supabase (sama dengan final) atau PostgreSQL lokal | Supabase, karena sudah dipilih sebagai database final |
| D2 | Lokasi kredensial | `appsettings.local.json` atau env var `CROUTE_*` | Env var `CROUTE_*`, lalu disimpan permanen di scope User |
| D3 | Zona waktu agregasi | Asia/Jakarta atau waktu lokal mesin | Asia/Jakarta, sama dengan `functions.sql` |
| D4 | Framework test | xUnit, NUnit, atau MSTest | xUnit, paling umum untuk proyek .NET baru |
| D5 | Cakupan unit test | Hanya Core, atau Core + Services + Data | Core + Services + validasi Data tanpa koneksi. Alur database tetap diuji lewat black box |

## 5. Tahapan kerja

### Ringkasan jadwal

| Sesi | Fokus | Estimasi | Keluaran |
|---|---|---|---|
| 1 | Verifikasi Supabase dan kredensial | 1–2 jam | Connection string permanen, query verifikasi lulus, `appsettings.json` bersih |
| 2 | Uji alur repository dan zona waktu | 2–3 jam | Daftar bug dan perbaikan; total Dashboard = SQL |
| 3 | Proyek test dan unit test Core | 2–3 jam | `CRoute.Tests` dibuat, `dotnet test` lulus untuk Core |
| 4 | Test kontrak dan black box awal | 2–3 jam | Test `RouteService`/`EmissionService`, BB-01–BB-06 terisi |
| 5 | Black box akhir, dokumentasi, commit | 2–3 jam | BB-07–BB-15 terisi, dokumentasi diperbarui, commit |

Total estimasi 9–14 jam kerja, tidak termasuk waktu menunggu integrasi Bayu.
Sesi 4 dan 5 bisa dipecah kalau integrasi Google Maps/Climatiq belum selesai.

### Tahap 1: Verifikasi database Supabase

Database final sudah dipilih (Supabase), jadi tahap ini adalah verifikasi,
bukan pembuatan dari nol.

**1a. Simpan connection string secara permanen**

Jalankan sekali di PowerShell, lalu tempel connection string session pooler dari
dashboard Supabase (jangan disimpan di dokumen ini):

```powershell
[Environment]::SetEnvironmentVariable(
    'CROUTE_Database__ConnectionString',
    '<connection-string-session-pooler>',
    'User')
```

Tutup dan buka kembali terminal/Visual Studio. Cek tanpa mencetak isinya:

```powershell
[bool][Environment]::GetEnvironmentVariable('CROUTE_Database__ConnectionString','User')
```

Hasilnya harus `True`. Jangan menempelkan connection string ke chat, issue, atau
commit.

**1b. Verifikasi isi database**

`psql` tidak terpasang di mesin ini, jadi verifikasi lewat aplikasi atau potongan
C# di [`database/supabase.md`](database/supabase.md) bagian 7.

- [ ] `SELECT count(*) FROM transport_modes` → 6.
- [ ] `SELECT count(*) FROM users` → minimal 1 (akun demo) atau akun hasil register.
- [ ] `SELECT fn_get_monthly_target('<uuid-user>')` → 15.00 untuk user baru.
- [ ] Login akun demo `demo` / `demo123` (kalau seed ada) atau register akun baru.

**1c. Kalau ada yang belum ada**

- [ ] Jalankan `schema.sql`, lalu `functions.sql` lewat SQL Editor Supabase.
- [ ] Buat role `croute` dan grant sesuai `supabase.md` bagian 4.
- [ ] Pastikan `appsettings.json` tidak berisi connection string Supabase.
- [ ] Catat hasil aktual dan kendala (pooler, SSL, project pause) ke
      `docs/database/supabase.md`.

**Selesai jika:** environment variable terbaca di terminal baru, query verifikasi
menghasilkan nilai benar, dan aplikasi bisa login memakai database Supabase.

### Tahap 2: Kredensial dan konfigurasi

- [ ] Pastikan connection string hanya ada di env User (Tahap 1a), bukan di
      `appsettings.json`.
- [ ] Kembalikan `CRoute.Services/appsettings.json` ke placeholder.
- [ ] Samakan format placeholder menjadi `GANTI_DENGAN_...` di
      `appsettings.json`, `appsettings.example.json`, dan pengecekan kode.
- [ ] Pastikan `appsettings.local.json` tetap di-ignore:

```powershell
git check-ignore -v CRoute.Services/appsettings.local.json
```

- [ ] Cek tidak ada password atau key asli:

```powershell
git grep -n -i "password=" -- "*.json"
git status --short
```

**Selesai jika:** `git grep` tidak menemukan kredensial asli dan aplikasi tetap
bisa terhubung lewat konfigurasi lokal.

### Tahap 3: Verifikasi repository dan zona waktu

Uji manual alur database sebelum menulis test:

- [x] Register akun baru, lalu login (diverifikasi lewat `CRouteDbCheck`).
- [x] Cek target awal 15,00 kg muncul di Statistik (repository + Dashboard).
- [x] Simpan satu perjalanan dari Cek Rute, lalu pastikan muncul di Riwayat
      (repository sudah diuji; alur UI menunggu integrasi Bayu).
- [x] Ubah target, tutup aplikasi, buka lagi, pastikan target tersimpan
      (target tersimpan di database dan terbaca lagi lewat `GetHistory`).
- [x] Hapus semua riwayat, pastikan konfirmasi dan hasilnya benar (repository;
      dialog konfirmasi sudah ada di `HistoryViewModel`).
- [x] Catat bahwa hapus satu perjalanan belum tersedia di `HistoryViewModel`
      (hanya ada tombol **Hapus semua**).
- [x] Cek nilai desimal jarak dan emisi tidak terpotong saat dibaca kembali
      (10,2 km dan target 20,5 kg).
- [x] Cek perjalanan tanpa hasil emisi (data lama) tidak membuat aplikasi error
      (trip legacy dibaca dengan 0 hasil; `AddTrip` menolak trip baru tanpa hasil).

Zona waktu (D3):

- [x] Tentukan sumber kebenaran: Asia/Jakarta.
- [x] Ubah `TripHistory.GetMonthlyTotal()` agar memakai Asia/Jakarta lewat
      `AppClock`, bukan `DateTime.Now`.
- [x] Samakan `StatsViewModel.BuildDailyBars` (`DateTime.Today`) dengan zona
      waktu yang dipilih.
- [x] Putuskan `PostgresTripHistoryRepository.GetMonthlyTotal()`: dihapus karena
      tidak dipakai.
- [x] Uji kasus batas bulan: unit test memakai 30 November 2026 pukul 17.00 UTC
      (1 Desember 2026 pukul 00.00 WIB); trip masuk bulan Desember.

**Selesai jika:** total di Dashboard sama dengan hasil query SQL
`fn_current_month_total`, termasuk saat tanggal berganti bulan.

### Tahap 4: Unit test

Buat proyek `CRoute.Tests` (xUnit) yang mereferensikan `CRoute.Core` dan
`CRoute.Services`. Data hanya diuji untuk validasi yang tidak membuka koneksi
database; alur database diuji lewat black box.

- [x] `TransportMode.CalculateEmission`: contoh dari `perhitungan-emisi.md`,
      termasuk faktor 0 untuk sepeda dan jalan kaki.
- [x] `Trip.GetBestOption`: memilih emisi terkecil, dan `null` jika belum ada hasil.
- [x] `TripHistory`: total bulanan, `GetProgress`, target 0, target negatif.
- [ ] `EmissionSavings.ForTrip`: belum ada unit test karena kelasnya ada di
      `CRoute.App` (di luar cakupan Core + Services). Perilakunya dicek lewat UI.
- [x] `ApiSettingsLoader`: mengosongkan placeholder `GANTI_DENGAN_...`.
- [x] **Kontrak `RouteService`**: request Routes API, parsing jarak/durasi,
      error API, timeout, dan placeholder key sudah diuji.
- [x] `PostgresAuthService.Register`: password kurang dari 6 karakter ditolak
      sebelum koneksi database.
- [ ] **Kontrak `EmissionService`**: `Calculate` sudah diuji; parsing `co2e` dan
      `co2e_unit` serta fallback API belum ada.

Jalankan:

```powershell
dotnet test croute.slnx
```

**Selesai jika:** semua test lulus dan `dotnet build` tetap 0 warning.

### Tahap 5: Black box test

Buat `docs/pengujian-black-box.md` dengan format Lab 10.2. Setiap kasus memuat
ID, fitur, prasyarat, langkah, input, hasil yang diharapkan, hasil aktual, dan
kolom Benar/Salah.

| ID | Fitur | Kasus | Hasil yang diharapkan |
|---|---|---|---|
| BB-01 | Login | Username dan password benar | Masuk ke Dashboard |
| BB-02 | Login | Password salah | "Username atau password salah." |
| BB-03 | Register | Username sudah dipakai | "Username sudah dipakai, coba yang lain." |
| BB-04 | Register | Password kurang dari batas minimal | "Password minimal {n} karakter." |
| BB-05 | Register | Konfirmasi tidak sama | "Konfirmasi password tidak sama dengan password." |
| BB-06 | Cek Rute | Asal dan tujuan sama | "Lokasi asal dan tujuan tidak boleh sama." |
| BB-07 | Cek Rute | Alamat valid | Jarak dan durasi sesuai Google Maps |
| BB-08 | Cek Rute | Key API salah | Pesan error jelas, bukan jarak 0 |
| BB-09 | Cek Rute | Alamat tidak ditemukan | Pesan "rute tidak ditemukan" |
| BB-10 | Cek Rute | Hitung semua moda | Emisi tiap moda tampil dan rekomendasi muncul |
| BB-11 | Riwayat | Simpan perjalanan | Muncul di daftar Riwayat |
| BB-12 | Riwayat | Hapus satu data | Data hilang dari daftar (fitur belum ada; lihat catatan) |
| BB-13 | Riwayat | Hapus semua | Muncul konfirmasi "Hapus semua riwayat perjalanan? ..." |
| BB-14 | Statistik | Target 70% dan 100% | Pesan status sesuai ambang |
| BB-15 | Dashboard | Data bulan ini | Total cocok dengan query SQL |

- [x] Jalankan BB-01 sampai BB-06 lebih dulu (tidak bergantung Bayu);
      hasilnya 6 **B**, 0 **S**.
- [ ] Jalankan BB-07 sampai BB-10 setelah integrasi Bayu selesai.
- [x] Isi kolom Benar/Salah dan catatan hasil untuk BB-01–BB-06.
- [x] Lampirkan screenshot untuk kasus yang gagal (tidak ada kasus **S**;
      bukti diambil dari teks UI Automation).

**Selesai jika:** semua kasus sudah dijalankan dan kolom hasil terisi.

### Tahap 6: Serah terima

- [x] Update status di `docs/database/supabase.md` dan
      `docs/perhitungan-emisi.md`.
- [x] Tambahkan `docs/perencanaan-backend.md`, `docs/pengujian-black-box.md`,
      dan `docs/koreksi-w1.md` ke tabel dokumentasi di `README.md`.
- [ ] Commit per tahap, misalnya `test: tambah unit test TransportMode`.
- [ ] Serahkan hasil test kontrak ke Bayu sebelum integrasi digabung.

## 6. Koordinasi dengan Bayu

Pekerjaan Bayu dan Ahmad bersinggungan di tiga tempat:

| Area | Pemilik | Kesepakatan |
|---|---|---|
| `RouteService` dan `EmissionService` | Bayu | Ahmad menulis test kontrak; Bayu menyesuaikan implementasi agar lulus |
| `App.xaml.cs` | Bayu | Ahmad tidak mengubah file ini |
| `appsettings.json` dan `appsettings.example.json` | Ahmad lebih dulu | Bayu menambahkan key lewat `appsettings.local.json` atau env var |
| `docs/integrasi-api.md` | Bayu | Ahmad tidak mengubah status integrasi |

Yang perlu disepakati dengan Bayu sebelum Tahap 4:

- Format contoh response yang akan dipakai test (`OK`, `REQUEST_DENIED`,
  `ZERO_RESULTS`).
- Perilaku yang diharapkan saat API gagal: exception dengan pesan apa.
- Kapan integrasi diperkirakan selesai, supaya BB-07 sampai BB-10 bisa dijadwalkan.

## 7. Risiko

| Risiko | Dampak | Mitigasi |
|---|---|---|
| Supabase free tier pause | Koneksi pertama timeout | Buka dashboard untuk membangunkan project sebelum pengujian |
| Connection string hanya ada di sesi terminal | Env hilang saat terminal ditutup | Simpan di scope User dan cek dari terminal baru |
| Jaringan tidak mendukung IPv6 | Direct connection gagal | Pakai session pooler |
| Akun demo ikut ter-seed di database bersama | Data uji bercampur | Hapus blok seed atau pakai akun uji terpisah |
| Implementasi Bayu belum selesai | Test kontrak dan BB-07–BB-10 tertunda | Kerjakan Tahap 1–3 dan BB-01–BB-06 lebih dulu |
| Zona waktu mesin berbeda | Total Dashboard beda dengan SQL | Terapkan D3 dan uji kasus batas bulan |
| Test bergantung pada database | Test lambat dan rapuh | Unit test hanya untuk Core dan Services |

## 8. Definition of Done

- [ ] Connection string Supabase tersimpan permanen di env User dan terbaca dari
      terminal baru.
- [ ] Database Supabase berisi skema, function, role, dan data awal yang benar.
- [ ] Tidak ada kredensial asli di berkas yang di-track git.
- [ ] Alur login, register, riwayat, target, dan hapus berjalan end-to-end.
- [ ] Total bulanan konsisten dengan SQL pada zona waktu yang disepakati.
- [ ] `dotnet test` lulus.
- [ ] `docs/pengujian-black-box.md` berisi hasil aktual BB-01 sampai BB-15.
- [ ] Dokumentasi di `docs/database` dan `docs/perhitungan-emisi.md` sesuai
      kondisi akhir.

## 9. Rujukan

- [`integrasi-api.md`](integrasi-api.md)
- [`perhitungan-emisi.md`](perhitungan-emisi.md)
- [`arsitektur.md`](arsitektur.md)
- [`database/supabase.md`](database/supabase.md)
- [`database/functions.sql`](database/functions.sql)
