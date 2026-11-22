# Catatan Koreksi terhadap W1

Dokumen ini mencatat perbedaan antara proposal W1 dan implementasi CRoute yang
sedang dikerjakan. W1 tidak diubah; koreksi ini dipakai saat menyusun laporan
akhir dan presentasi.

| No | Isi W1 | Implementasi final | Alasan | Status |
|---|---|---|---|---|
| 1 | Database **SQLite** | **PostgreSQL** (Supabase) | Aplikasi multi-user, butuh function agregasi, dan data harus bisa diakses beberapa anggota tim. SQLite tidak cocok untuk itu. | ✅ Dipakai |
| 2 | **Google Maps Embed** untuk peta | **Google Maps Routes API** (`computeRoutes`) untuk jarak dan durasi | Embed hanya menampilkan peta; angka jarak/durasi tidak bisa dipakai untuk menghitung emisi. Distance Matrix sudah legacy. | ✅ Dipakai; fallback simulasi tanpa API key |
| 3 | "Pencarian rute memakai API peta" (umum) | **Google Maps Routes API** untuk rute + **Climatiq** untuk faktor emisi | Dua kebutuhan berbeda: jarak rute dan faktor emisi per moda. | ⚠️ Google Maps selesai; Climatiq belum |

## Catatan

- Distance Matrix legacy tidak bisa diaktifkan di proyek Google Cloud baru sejak
  Oktober 2026. Karena itu implementasi memakai **Routes API**, bukan Distance
  Matrix. Format respons dan `RouteService` sudah disesuaikan.
- Faktor emisi saat ini memakai seed database. Climatiq belum dipanggil untuk
  memperbarui faktor.
- Koreksi ini tidak mengubah ruang lingkup fitur W1: cari rute, hitung emisi,
  bandingkan moda, rekomendasi, riwayat, statistik, dan target tetap sama.
