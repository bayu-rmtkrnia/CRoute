# CRoute

CRoute adalah sebuah aplikasi desktop yang berfungsi membandingkan emisi yang dihasilkan dari perjalanan menggunakan berbagai kendaraan.

## Ringkasan MVP

- Cari rute asal–tujuan melalui Google Maps (Routes API).
- Hitung estimasi emisi CO₂ dari faktor emisi master; integrasi Climatiq direncanakan.
- Pilih dan bandingkan moda: mobil, motor, bus, kereta, sepeda, dan jalan kaki.
- Tampilkan rekomendasi moda paling hemat emisi dan visualisasi perbandingannya.
- Simpan riwayat perjalanan dengan moda yang benar-benar dipakai, lihat statistik, dan atur target emisi bulanan.

Status implementasi setiap fitur dijelaskan di [`docs/integrasi-api.md`](docs/integrasi-api.md) dan [`docs/arsitektur.md`](docs/arsitektur.md).

## Dokumentasi

| Dokumen | Isi |
| --- | --- |
| [`docs/index.md`](docs/index.md) | Jawaban Modul 1 (latar belakang, ide solusi, analisis kompetitor) |
| [`docs/arsitektur.md`](docs/arsitektur.md) | Arsitektur lapisan, ViewModel, navigasi, konfigurasi |
| [`docs/perhitungan-emisi.md`](docs/perhitungan-emisi.md) | Rumus emisi, faktor per moda, agregasi, dan batasan |
| [`docs/integrasi-api.md`](docs/integrasi-api.md) | Integrasi Google Maps dan Climatiq, status, dan langkah lanjutan |
| [`docs/panduan-pengguna.md`](docs/panduan-pengguna.md) | Panduan memakai aplikasi dari sisi pengguna |
| [`docs/perencanaan-backend.md`](docs/perencanaan-backend.md) | Rencana pekerjaan backend dan pengujian |
| [`docs/pengujian-black-box.md`](docs/pengujian-black-box.md) | Kasus dan hasil pengujian black box (format Lab 10.2) |
| [`docs/koreksi-w1.md`](docs/koreksi-w1.md) | Perbedaan proposal W1 dengan implementasi final |
| [`docs/database/README.md`](docs/database/README.md) | Skema dan skrip database |

## Prasyarat

- Windows 10/11 dengan [.NET 10 SDK](https://dotnet.microsoft.com/download) dan dukungan Windows Desktop.
- PostgreSQL, lokal atau Supabase.
- Kunci API Google Maps (Routes API) di `CRoute.Services/appsettings.local.json` atau env `CROUTE_GoogleMaps__ApiKey`. Tanpa kunci, aplikasi memakai `FakeRouteService`. Climatiq ditunda; emisi dihitung dari faktor `transport_modes` lewat `LocalEmissionService`. Lihat [`docs/integrasi-api.md`](docs/integrasi-api.md).

## Database

Perencanaan database PostgreSQL beserta skrip siap pakai ada di [`docs/database`](docs/database/README.md):

- [`schema.sql`](docs/database/schema.sql) — tabel, constraint, index, seed, trigger, view.
- [`functions.sql`](docs/database/functions.sql) — function agregasi Dashboard/Statistik.
- [`csharp-functions.md`](docs/database/csharp-functions.md) — class/method C# yang perlu dibuat di `CRoute.Data`.
- [`supabase.md`](docs/database/supabase.md) — langkah memakai Supabase sebagai database.
- [`target-alignment-plan.md`](docs/database/target-alignment-plan.md) — rencana agar skema ≥70% mirip dengan target, tetap multi-user.
- [`migration-minimal-target.sql`](docs/database/migration-minimal-target.sql) — migrasi database lama ke struktur minimal target.
- [`migration-chosen-mode.sql`](docs/database/migration-chosen-mode.sql) — migrasi `fn_save_trip` untuk menyimpan moda yang dipilih pengguna.

## Menjalankan aplikasi

1. Siapkan PostgreSQL (lokal atau Supabase).
2. Jalankan `docs/database/schema.sql`, lalu `docs/database/functions.sql`.
3. Atur connection string dengan salah satu cara berikut (urutan prioritas: env var, lalu file lokal, lalu `appsettings.json`):
   - Environment variable `CROUTE_Database__ConnectionString`, atau
   - `CRoute.Services/appsettings.local.json` (sudah diabaikan oleh git), atau
   - `CRoute.Services/appsettings.json` (hanya untuk nilai default yang aman).

   Jangan menyimpan kredensial asli di `appsettings.json` yang ter-track di git.
4. Buka `croute.slnx`, jadikan `CRoute.App` sebagai startup project, lalu tekan **F5**.
5. Login dengan akun demo `demo` / `demo123` (kalau seed dijalankan) atau buat akun baru lewat Register.

> Saat ini auth, riwayat perjalanan, target, dan master moda memakai PostgreSQL.
> Jarak dan durasi memakai Google Maps Routes API kalau `GoogleMaps:ApiKey`
> diisi; tanpa key, aplikasi memakai `FakeRouteService`. Perhitungan emisi
> memakai faktor lokal dari tabel `transport_modes` (`LocalEmissionService`);
> Climatiq ditunda.

## Pengujian

Jalankan unit test dengan:

```powershell
dotnet test croute.slnx
```

Kasus pengujian black box dan kolom hasilnya ada di
[`docs/pengujian-black-box.md`](docs/pengujian-black-box.md).

Nama Anggota Kelompok:

1. Ketua Kelompok : BAYU RAHMAT KURNIA - 24/533736/TK/59139
2. Anggota 1 : Ahmad Maulana Ibrahim - 24/539655/TK/59853
3. Anggota 2 : Sukmawati - 24/545512/TK/60686

## Pembagian Peran

| Anggota | Peran | Tanggung jawab |
|---|---|---|
| Bayu Rahmat Kurnia | Software Architect | Integrasi Google Maps dan Climatiq, layanan web API tambahan (opsional), kemasan `.exe` dan panduan instalasi |
| Ahmad Maulana Ibrahim | Back End | Database PostgreSQL, konfigurasi dan kredensial, repository, unit test, black box test |
| Sukmawati | Front End | Dashboard, visualisasi perbandingan emisi, screenshot panduan pengguna |

Class Diagram CRoute
<img width="850" height="1100" alt="Class Diagram CRoute png" src="https://github.com/user-attachments/assets/6c4687c5-7e94-4453-bac3-c4bba0cd274e" />
