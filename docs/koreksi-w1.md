# Catatan Koreksi terhadap W1

Dokumen ini mencatat perbedaan antara proposal W1 dan implementasi CRoute yang
sedang dikerjakan. W1 tidak diubah; koreksi ini dipakai saat menyusun laporan
akhir dan presentasi.

| No | Isi W1 | Implementasi final | Alasan | Status |
|---|---|---|---|---|
| 1 | Database **SQLite** | **PostgreSQL** (Supabase) | Aplikasi multi-user, butuh function agregasi, dan data harus bisa diakses beberapa anggota tim. SQLite tidak cocok untuk itu. | ✅ Dipakai |
| 2 | **Google Maps Embed** untuk peta | **Distance Matrix** untuk mengambil jarak dan durasi | Embed hanya menampilkan peta; angka jarak/durasi tidak bisa dipakai untuk menghitung emisi. | ⚠️ Rencana; keputusan Distance Matrix vs Routes API belum final |
| 3 | "Pencarian rute memakai API peta" (umum) | **Google Maps** untuk rute + **Climatiq** untuk faktor emisi | Dua kebutuhan berbeda: jarak rute dan faktor emisi per moda. | ⚠️ Integrasi belum selesai (Bayu) |

## Catatan

- Distance Matrix legacy tidak bisa diaktifkan di proyek Google Cloud baru sejak
  Oktober 2026. Kalau itu terjadi pada proyek tim, alternatifnya **Routes API**.
  Format respons dan kode `RouteService` perlu disesuaikan.
- Faktor emisi saat ini memakai seed database. Climatiq belum dipanggil untuk
  memperbarui faktor.
- Koreksi ini tidak mengubah ruang lingkup fitur W1: cari rute, hitung emisi,
  bandingkan moda, rekomendasi, riwayat, statistik, dan target tetap sama.
