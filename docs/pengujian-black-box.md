# Pengujian Black Box CRoute

Dokumen ini mengikuti format Lab 10.2: setiap kasus berisi langkah, input, hasil
yang diharapkan, hasil aktual, dan kolom Benar/Salah. Pengujian dilakukan setelah
aplikasi bisa dijalankan dengan database dan integrasi API yang berlaku.

> **Cara mengisi**
> 1. Jalankan kasus sesuai urutan. Jangan mengubah kolom "Hasil yang diharapkan"
>    agar cocok dengan hasil aktual.
> 2. Isi "Hasil aktual" dengan apa yang benar-benar terlihat.
> 3. Isi "B/S" dengan **B** jika sesuai dan **S** jika tidak sesuai.
> 4. Untuk kasus **S**, lampirkan screenshot dan catat pesan error di kolom
>    catatan.
> 5. Kolom Benar/Salah diisi oleh penguji, bukan oleh penulis kasus.

## 1. Informasi pengujian

| Item | Isi |
|---|---|
| Tanggal pengujian | 22 November 2026 |
| Penguji | Ahmad Maulana Ibrahim (otomatis lewat Windows UI Automation) |
| Commit yang diuji | `7947766` (branch `dev`) + perubahan lokal 22 November 2026 yang belum di-commit |
| Sistem operasi | Windows 11 25H2 (10.0.26200) |
| Versi .NET | SDK 10.0.402, runtime 10.0.13 |
| Database | Supabase PostgreSQL 17.11 |
| Integrasi Google Maps | Belum (`FakeRouteService`) |
| Integrasi Climatiq | Belum (`FakeEmissionService`) |
| Akun yang dipakai | `demo` / `demo123` |

## 2. Daftar kasus

| ID | Fitur | Prasyarat | Langkah | Input | Hasil yang diharapkan | Hasil aktual | B/S |
|---|---|---|---|---|---|---|---|
| BB-01 | Login | Akun demo tersedia | Buka aplikasi, isi username dan password, klik **Login** | `demo` / `demo123` | Dashboard tampil dan menyapa nama pengguna | Window **CRoute** tampil; sapaan "Halo, Akun!" dan tanggal "Minggu, 22 November 2026" muncul. | B |
| BB-02 | Login | Akun demo tersedia | Buka aplikasi, isi username dan password salah, klik **Login** | `demo` / `salah123` | Pesan "Username atau password salah." dan tetap di halaman login | Pesan "Username atau password salah." tampil; window login tetap terbuka dan Dashboard tidak muncul. | B |
| BB-03 | Register | Username `demo` sudah ada | Buka **Register**, isi data dengan username `demo` | Nama valid, `demo`, password 6+ karakter, konfirmasi sama | Pesan "Username sudah dipakai, coba yang lain." | Pesan "Username sudah dipakai, coba yang lain." tampil; window Register tetap terbuka. | B |
| BB-04 | Register | - | Buka **Register**, isi password kurang dari 6 karakter | Username baru, password `123`, konfirmasi `123` | Pesan "Password minimal 6 karakter." | Pesan "Password minimal 6 karakter." tampil. | B |
| BB-05 | Register | - | Buka **Register**, isi konfirmasi berbeda | Password `rahasia1`, konfirmasi `rahasia2` | Pesan "Konfirmasi password tidak sama dengan password." | Pesan "Konfirmasi password tidak sama dengan password." tampil. | B |
| BB-06 | Cek Rute | Sudah login | Buka menu **Cek Rute**, isi asal dan tujuan sama, klik **Bandingkan emisi** | `Kampus` dan `Kampus` | Pesan "Lokasi asal dan tujuan tidak boleh sama." dan tidak ada hasil | Pesan "Lokasi asal dan tujuan tidak boleh sama." tampil; panel "Belum ada hasil" tetap dan tabel perbandingan tidak muncul. | B |
| BB-07 | Cek Rute | Sudah login, Google Maps aktif | Buka menu **Cek Rute**, isi dua alamat valid, klik **Bandingkan emisi** | `Universitas Gadjah Mada, Yogyakarta` dan `Stasiun Tugu Yogyakarta` | Jarak dan durasi tampil, bukan 0, dan mendekati hasil Google Maps | | |
| BB-08 | Cek Rute | Google Maps aktif dengan key salah | Ganti API key menjadi nilai salah, jalankan ulang aplikasi, coba **Bandingkan emisi** | Dua alamat valid | Pesan error yang jelas tentang API key, bukan jarak 0 | | |
| BB-09 | Cek Rute | Sudah login, Google Maps aktif | Buka menu **Cek Rute**, isi alamat yang tidak ada, klik **Bandingkan emisi** | `Alamat Tidak Ada 12345` dan alamat valid | Pesan "rute tidak ditemukan" atau pesan sejenis, bukan jarak 0 | | |
| BB-10 | Cek Rute | Sudah login, Google Maps dan Climatiq aktif | Centang semua moda, klik **Bandingkan emisi** | Dua alamat valid, 6 moda | Emisi setiap moda tampil dan rekomendasi menunjukkan moda paling hemat | | |
| BB-11 | Riwayat | BB-10 berhasil | Setelah menghitung, klik **Simpan ke riwayat**, lalu buka **Riwayat** | - | Perjalanan muncul di daftar Riwayat dengan jarak dan emisi yang sama | | |
| BB-12 | Riwayat | Ada minimal satu riwayat | Buka **Riwayat**, pilih satu data, klik **Hapus** | - | Data hilang dari daftar dan tidak muncul setelah aplikasi dibuka ulang | | |
| BB-13 | Riwayat | Ada minimal satu riwayat | Buka **Riwayat**, klik **Hapus semua** | - | Muncul konfirmasi "Hapus semua riwayat perjalanan? Tindakan ini tidak bisa dibatalkan."; Batal mempertahankan data, OK menghapus semua | | |
| BB-14 | Statistik | Ada perjalanan bulan ini | Buka **Statistik & Target**, atur target sehingga pemakaian sekitar 70%, lalu sekitar 100%, klik **Simpan target** | Target = total bulanan / 0,7 lalu target = total bulanan | 70% sampai <100% menampilkan "Hampir mencapai target, pilih moda yang lebih hemat."; ≥100% menampilkan "Target bulan ini sudah terlampaui." | | |
| BB-15 | Dashboard | Ada perjalanan bulan ini | Catat angka emisi bulan ini di Dashboard, lalu jalankan query SQL | `SELECT fn_current_month_total('<uuid-user>');` | Angka Dashboard sama dengan hasil SQL (perbedaan hanya pembulatan tampilan) | | |

## 3. Rekap hasil

| ID | Fitur | B/S | Catatan |
|---|---|---|---|
| BB-01 | Login | B | Diuji otomatis 22 Nov 2026 |
| BB-02 | Login | B | Diuji otomatis 22 Nov 2026 |
| BB-03 | Register | B | Diuji otomatis 22 Nov 2026 |
| BB-04 | Register | B | Diuji otomatis 22 Nov 2026 |
| BB-05 | Register | B | Diuji otomatis 22 Nov 2026 |
| BB-06 | Cek Rute | B | Diuji otomatis 22 Nov 2026 |
| BB-07 | Cek Rute | | Menunggu integrasi Google Maps |
| BB-08 | Cek Rute | | Menunggu integrasi Google Maps |
| BB-09 | Cek Rute | | Menunggu integrasi Google Maps |
| BB-10 | Cek Rute | | Menunggu integrasi Google Maps + Climatiq |
| BB-11 | Riwayat | | Menunggu BB-10 |
| BB-12 | Riwayat | | Fitur hapus satu data belum tersedia |
| BB-13 | Riwayat | | Bisa dijalankan setelah ada riwayat |
| BB-14 | Statistik | | Bisa dijalankan setelah ada riwayat |
| BB-15 | Dashboard | | Bisa dijalankan setelah ada riwayat |

Jumlah: **B** = 6, **S** = 0, **belum dijalankan** = 9.

## 4. Catatan

- BB-12: `HistoryViewModel` saat ini hanya punya tombol **Hapus semua**. Tombol
  hapus satu perjalanan belum tersedia. Jika fitur tidak ditambahkan, catat
  **S** dengan keterangan "fitur belum tersedia", bukan bug aplikasi.
- BB-01 sampai BB-06 **sudah dijalankan** pada 22 November 2026 memakai Windows
  UI Automation terhadap build Debug. Skrip membuka window, mengisi field,
  mengirim `Space` ke menu, dan membaca teks hasil langsung dari UI.
- Label tombol yang benar: **Bandingkan emisi** (bukan "Cek Rute"),
  **Simpan ke riwayat**, **Hapus semua**, dan **Simpan target**.
- BB-07 sampai BB-10 bergantung pada pekerjaan Bayu. Jalankan setelah
  `FakeRouteService` dan `FakeEmissionService` diganti di `App.xaml.cs`.
- BB-08 membutuhkan restart aplikasi setelah mengganti key, karena konfigurasi
  dibaca saat aplikasi mulai.
- BB-14 membutuhkan data perjalanan bulan berjalan. Jika belum ada, simpan satu
  perjalanan terlebih dahulu.
- BB-15 membutuhkan akses SQL Editor Supabase atau `psql`.
