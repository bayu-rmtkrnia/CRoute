# Panduan Pengguna CRoute

CRoute membantu kamu membandingkan estimasi emisi CO₂ dari beberapa moda
transportasi untuk satu perjalanan: mobil, motor, bus, kereta, sepeda, dan
jalan kaki. Aplikasi juga merekomendasikan moda yang paling hemat, menyimpan
riwayat, dan membantu kamu memantau target emisi bulanan.

> **Legenda status**
> ✅ Sudah berjalan · ⚠️ Sebagian (ada, tapi belum lengkap/belum real) · ❌ Belum

> **Catatan screenshot:** gambar di panduan ini masih placeholder. Screenshot
> asli akan diambil setelah aplikasi selesai.

## 1. Sebelum mulai

- Aplikasi berjalan di Windows sebagai aplikasi desktop.
- Aplikasi memerlukan koneksi ke database. Jika database belum tersambung,
  login akan menampilkan pesan "Gagal terhubung ke database".
- Untuk pengaturan database, lihat [`README.md`](../README.md).

## 2. Masuk dan daftar akun

### 2.1 Halaman login ✅

> 📷 **[Screenshot: Halaman login]** — akan diisi setelah aplikasi selesai.

1. Isi **Username** dan **Password**.
2. Klik **Login**. Tombol ini juga aktif dengan tombol Enter.
3. Jika username atau password salah, muncul pesan *"Username atau password salah."*
4. Klik **Close** untuk menutup aplikasi.

Jika belum punya akun, klik **Daftar di sini**.

### 2.2 Halaman daftar akun ✅

> 📷 **[Screenshot: Halaman daftar akun]** — akan diisi setelah aplikasi selesai.

| Kolom | Aturan |
|---|---|
| Nama lengkap | Dipakai untuk sapaan di Dashboard. Hanya kata pertama yang dipakai, mis. "Halo, Budi!" |
| Username | Harus unik. Jika sudah dipakai: *"Username sudah dipakai, coba yang lain."* |
| Password | Minimal 6 karakter |
| Konfirmasi password | Harus sama dengan password |

Klik **Daftar** untuk membuat akun, atau **Batal** untuk kembali ke login.

> Akun demo `demo` / `demo123` bisa dipakai jika database sudah diisi data
> contoh.

### 2.3 Keluar ✅

Klik **Keluar** di bagian bawah menu samping untuk kembali ke halaman login.

## 3. Navigasi

Menu samping berisi empat halaman:

| Menu | Fungsi |
|---|---|
| 🏠 Dashboard | Ringkasan emisi bulan ini, target, dan perjalanan terakhir |
| 🧭 Cek Rute | Membandingkan emisi untuk satu perjalanan |
| 🕘 Riwayat | Daftar perjalanan yang sudah disimpan |
| 📊 Statistik & Target | Total, rata-rata, grafik 7 hari, dan pengaturan target |

## 4. Dashboard ✅

> 📷 **[Screenshot: Dashboard]** — akan diisi setelah aplikasi selesai.

Dashboard menampilkan:

- **Sapaan** dengan nama depan kamu dan tanggal hari ini. Jika nama tidak tersedia,
  sapaannya "Halo, Tamu!".
- **Kartu ringkasan**:
  - *Emisi bulan ini* (kg CO₂), dari perjalanan bulan berjalan.
  - *Hemat dibanding naik mobil* (kg CO₂), dihitung dari selisih setiap perjalanan
    dengan moda mobil.
  - *Perjalananmu*, jumlah perjalanan yang tersimpan.
- **Target emisi bulanan**: progres bulan ini dan status singkat. Klik **Atur
  target** untuk membuka Statistik & Target.
- **Perjalanan terakhir**: tiga perjalanan terbaru. Klik **Lihat semua** untuk
  membuka Riwayat.
- **Kartu "Tahukah kamu?"**: informasi edukasi. Teks ini statis dan tidak dihitung
  dari data kamu.

Klik **Yuk, cek sekarang →** untuk langsung membuka Cek Rute.

## 5. Cek Rute ⚠️

> 📷 **[Screenshot: Halaman Cek Rute, sebelum dan sesudah hasil]** — akan diisi setelah aplikasi selesai.

### 5.1 Membandingkan emisi

1. Isi **Lokasi asal**, mis. "Jl. Kaliurang, Sleman".
2. Isi **Lokasi tujuan**, mis. "UGM, Bulaksumur".
3. Pilih satu atau lebih moda di **Moda yang dibandingkan**. Moda yang dipilih
   berwarna terang.
4. Klik **Bandingkan emisi**.

Aturan validasi:

- Asal dan tujuan wajib diisi.
- Asal dan tujuan tidak boleh sama. Jika sama, muncul pesan *"Lokasi asal dan
  tujuan tidak boleh sama."*
- Minimal satu moda harus dipilih. Jika tidak, tombol **Bandingkan emisi** tidak aktif.

### 5.2 Membaca hasil

Setelah perhitungan selesai, panel kanan menampilkan:

- **Ringkasan rute**: asal, tujuan, jarak, dan durasi.
- **Rekomendasi**: moda dengan emisi terkecil, beserta selisih terhadap mobil.
- **Perbandingan emisi per moda (kg CO₂)**: batang horizontal untuk setiap
  moda. Moda dengan emisi terkecil diberi tanda **★ terbaik**.

### 5.3 Menyimpan ke riwayat

- Klik **Simpan ke riwayat** untuk menyimpan perjalanan. Setelah tersimpan,
  tombol berubah menjadi **✓ Tersimpan di riwayat**, dan tombol **Lihat riwayat**
  muncul.
- Hasil yang **belum disimpan akan hilang** jika kamu pindah ke halaman lain.

### 5.4 Batasan saat ini ⚠️

- **Jarak dan durasi** berasal dari Google Maps Routes API jika
  `GoogleMaps:ApiKey` sudah diisi. Tanpa key, aplikasi memakai simulasi
  `FakeRouteService`, sehingga jarak **tidak sesuai kondisi sebenarnya**. Lihat
  [`integrasi-api.md`](integrasi-api.md).
- **Peta belum tersedia.** Rute belum ditampilkan di peta.
- **Jarak berbeda per moda.** Bus dan Kereta memakai jarak rute transit
  masing-masing. Jalan kaki, Mobil, Motor, dan Sepeda masih memakai rute
  referensi `DRIVE`.
- **Bus/Kereta tidak dipaksakan muncul.** Moda ini hanya tampil jika ada rute
  bus + jalan kaki atau kereta + jalan kaki dengan total jalan kaki ≤ 2 km.
  Jika tidak ada, moda tersebut tidak masuk perbandingan.

## 6. Riwayat ✅

> 📷 **[Screenshot: Halaman Riwayat]** — akan diisi setelah aplikasi selesai.

- Halaman **Riwayat Perjalanan** menampilkan semua perjalanan yang sudah
  disimpan, beserta jumlahnya.
- Jika belum ada perjalanan, muncul pesan *"Riwayat masih kosong"* dan tombol
  **Cari rute**.
- Klik **Hapus semua** untuk menghapus seluruh riwayat. Aplikasi meminta
  konfirmasi terlebih dahulu. Tindakan ini tidak bisa dibatalkan.

## 7. Statistik & Target ✅

> 📷 **[Screenshot: Halaman Statistik & Target]** — akan diisi setelah aplikasi selesai.

### 7.1 Target bulan ini

- Kartu target menampilkan total emisi bulan ini dan target yang ditetapkan,
  mis. "12,3 / 15,0 kg".
- Bar progres dan status:

| Progres | Status |
|---|---|
| Kurang dari 70% | Masih aman, pertahankan! |
| 70% sampai kurang dari 100% | Hampir mencapai target, pilih moda yang lebih hemat. |
| 100% atau lebih | Target bulan ini sudah terlampaui. |

- Target default adalah **15,0 kg CO₂ per bulan**.

### 7.2 Mengubah target

1. Isi **Ubah target (kg CO₂ per bulan)**. Angka boleh memakai koma atau titik,
   mis. "12,5" atau "12.5".
2. Klik **Simpan target**.
3. Pesan konfirmasi muncul di bawah tombol. Jika angka tidak valid (negatif,
   nol, atau bukan angka), pesan kesalahan akan muncul.

### 7.3 Ringkasan dan grafik

- **Total hemat vs mobil**: total selisih emisi dibanding naik mobil.
- **Rata-rata per perjalanan**: rata-rata emisi (kg CO₂) per perjalanan.
- **Moda favorit**: moda yang paling sering menjadi rekomendasi.
- **Emisi 7 hari terakhir**: grafik batang per hari, untuk tujuh hari terakhir.

## 8. Fitur yang belum tersedia

| Fitur | Status | Keterangan |
|---|---|---|
| Jarak dan durasi nyata dari Google Maps | ✅/⚠️ | Routes API dipakai jika API key diisi; tanpa key memakai simulasi `FakeRouteService` |
| Peta rute | ❌ | Belum ada di Cek Rute |
| Faktor emisi terbaru dari Climatiq (ditunda) | ❌ | Memakai nilai tetap dari master moda |
| Jumlah penumpang | ❌ | Belum ada. Emisi dihitung per kendaraan |
| Perhitungan bulanan langsung dari database | ⚠️ | Fungsi SQL sudah tersedia, belum dipakai aplikasi |

Fitur yang sudah berjalan ✅: login dan daftar, Dashboard, membandingkan moda,
rekomendasi, menyimpan riwayat, menghapus riwayat, target bulanan, dan grafik
7 hari.
