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
| Commit yang diuji | `7947766` untuk BB-01–BB-06. Kode sekarang `89f6ed8` + perbaikan lokal, belum diuji ulang penuh |
| Sistem operasi | Windows 11 25H2 (10.0.26200) |
| Versi .NET | SDK 10.0.402, runtime 10.0.13 |
| Database | Supabase PostgreSQL 17.11 |
| Integrasi Google Maps | Routes API aktif; API key sudah tersedia di env User mesin uji. BB-07–BB-10 belum dijalankan |
| Integrasi Climatiq | Ditunda; perhitungan memakai `LocalEmissionService` dari faktor `transport_modes` |
| Akun yang dipakai | `demo` / `demo123` (akun ini masih bersih, 0 trip) |

## 2. Daftar kasus

| ID | Fitur | Prasyarat | Langkah | Input | Hasil yang diharapkan | Hasil aktual | B/S |
|---|---|---|---|---|---|---|---|
| BB-01 | Login | Akun demo tersedia | Buka aplikasi, isi username dan password, klik **Login** | `demo` / `demo123` | Dashboard tampil dan menyapa nama pengguna | Window **CRoute** tampil; sapaan "Halo, Akun!" dan tanggal "Minggu, 22 November 2026" muncul. | B |
| BB-02 | Login | Akun demo tersedia | Buka aplikasi, isi username dan password salah, klik **Login** | `demo` / `salah123` | Pesan "Username atau password salah." dan tetap di halaman login | Pesan "Username atau password salah." tampil; window login tetap terbuka dan Dashboard tidak muncul. | B |
| BB-03 | Register | Username `demo` sudah ada | Buka **Register**, isi data dengan username `demo` | Nama valid, `demo`, password 6+ karakter, konfirmasi sama | Pesan "Username sudah dipakai, coba yang lain." | Pesan "Username sudah dipakai, coba yang lain." tampil; window Register tetap terbuka. | B |
| BB-04 | Register | - | Buka **Register**, isi password kurang dari 6 karakter | Username baru, password `123`, konfirmasi `123` | Pesan "Password minimal 6 karakter." | Pesan "Password minimal 6 karakter." tampil. | B |
| BB-05 | Register | - | Buka **Register**, isi konfirmasi berbeda | Password `rahasia1`, konfirmasi `rahasia2` | Pesan "Konfirmasi password tidak sama dengan password." | Pesan "Konfirmasi password tidak sama dengan password." tampil. | B |
| BB-06 | Cek Rute | Sudah login | Buka menu **Cek Rute**, isi asal dan tujuan sama, klik **Bandingkan emisi** | `Kampus` dan `Kampus` | Pesan "Lokasi asal dan tujuan tidak boleh sama." dan tidak ada hasil | Pesan "Lokasi asal dan tujuan tidak boleh sama." tampil; panel "Belum ada hasil" tetap dan tabel perbandingan tidak muncul. | B |
| BB-07 | Cek Rute | Sudah login, `GoogleMaps:ApiKey` berisi key **asli** | Buka menu **Cek Rute**, isi dua alamat valid, klik **Bandingkan emisi** | `Universitas Gadjah Mada, Yogyakarta` dan `Stasiun Tugu Yogyakarta` | Jarak dan durasi tampil, bukan 0, dan mendekati hasil Google Maps | | |
| BB-08 | Cek Rute | `GoogleMaps:ApiKey` berisi key salah yang **bukan placeholder** | Ganti API key menjadi nilai salah, jalankan ulang aplikasi, coba **Bandingkan emisi** | Dua alamat valid | Pesan error yang jelas tentang API key, bukan jarak 0 | | |
| BB-09 | Cek Rute | Sudah login, `GoogleMaps:ApiKey` berisi key asli | Buka menu **Cek Rute**, isi alamat yang tidak ada, klik **Bandingkan emisi** | `Alamat Tidak Ada 12345` dan alamat valid | Pesan "rute tidak ditemukan" atau pesan sejenis, bukan jarak 0 | | |
| BB-10 | Cek Rute | Sudah login, Google Maps aktif; faktor emisi dari master database | Centang semua moda, klik **Bandingkan emisi** | Dua alamat valid, 6 moda | Emisi setiap moda yang tersedia tampil dan rekomendasi menunjukkan moda paling hemat; Bus/Kereta hanya tampil jika rutenya ada dan jalan kaki ≤ 2 km | | |
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
| BB-07 | Cek Rute | | Butuh API key Google Maps asli |
| BB-08 | Cek Rute | | Butuh API key salah yang bukan placeholder |
| BB-09 | Cek Rute | | Butuh API key Google Maps asli |
| BB-10 | Cek Rute | | Butuh API key Google Maps asli; Climatiq ditunda |
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
- **Penting untuk BB-07–BB-10:** integrasi Routes API hanya aktif kalau
  `GoogleMaps:ApiKey` berisi key asli. Tanpa key, `App.xaml.cs` memakai
  `FakeRouteService`, sehingga jarak dan durasi tetap tampil tetapi **simulasi**.
  Jangan mencatat **B** untuk BB-07/BB-09 kalau key belum diisi.
- BB-08 harus memakai key salah yang bukan placeholder; kalau key kosong atau
  masih `GANTI_DENGAN...`, aplikasi memakai fake dan tidak memanggil API.
- BB-10 memakai faktor emisi dari tabel `transport_modes`, bukan Climatiq.
  Climatiq ditunda.
- BB-10: Bus dan Kereta hanya muncul jika Routes API mengembalikan rute
  bus + jalan kaki atau kereta + jalan kaki dengan total jalan kaki ≤ 2 km.
  Rute campuran bus + kereta tidak dipakai.
- BB-08 membutuhkan restart aplikasi setelah mengganti key, karena konfigurasi
  dibaca saat aplikasi mulai.
- BB-14 membutuhkan perjalanan bulan berjalan yang **opsi terbaiknya bukan
  Sepeda/Jalan kaki**, karena emisi terbaik kedua moda itu 0 dan target tidak
  akan pernah tercapai. Pilih hanya moda Mobil saat menghitung jika perlu.
- BB-15 membutuhkan akses SQL Editor Supabase atau `psql`.
