# CRoute

CRoute adalah sebuah aplikasi desktop yang berfungsi membandingkan emisi yang dihasilkan dari perjalanan menggunakan berbagai kendaraan.

## Database

Perencanaan database PostgreSQL beserta skrip siap pakai ada di [`docs/database`](docs/database/README.md):

- [`schema.sql`](docs/database/schema.sql) — tabel, constraint, index, seed, trigger, view.
- [`functions.sql`](docs/database/functions.sql) — function agregasi Dashboard/Statistik.
- [`csharp-functions.md`](docs/database/csharp-functions.md) — class/method C# yang perlu dibuat di `CRoute.Data`.
- [`supabase.md`](docs/database/supabase.md) — langkah memakai Supabase sebagai database.
- [`target-alignment-plan.md`](docs/database/target-alignment-plan.md) — rencana agar skema ≥70% mirip dengan target, tetap multi-user.
- [`migration-minimal-target.sql`](docs/database/migration-minimal-target.sql) — migrasi database lama ke struktur minimal target.

## Menjalankan aplikasi

1. Siapkan PostgreSQL (lokal atau Supabase).
2. Jalankan `docs/database/schema.sql`, lalu `docs/database/functions.sql`.
3. Atur connection string di `CRoute.Services/appsettings.json` atau environment variable `CROUTE_Database__ConnectionString`.
4. Buka `croute.slnx`, jadikan `CRoute.App` sebagai startup project, lalu tekan **F5**.
5. Login dengan akun demo `demo` / `demo123` (kalau seed dijalankan) atau buat akun baru lewat Register.

> Saat ini auth, riwayat perjalanan, target, dan master moda memakai PostgreSQL.
> Route dan perhitungan emisi masih memakai implementasi fake sampai integrasi
> Google Maps/Climatiq dikerjakan.

Nama Anggota Kelompok:

1. Ketua Kelompok : BAYU RAHMAT KURNIA - 24/533736/TK/59139
2. Anggota 1 : Ahmad Maulana Ibrahim - 24/539655/TK/59853
3. Anggota 2 : Sukmawati - 24/545512/TK/60686

Class Diagram CRoute
<img width="850" height="1100" alt="Class Diagram CRoute png" src="https://github.com/user-attachments/assets/6c4687c5-7e94-4453-bac3-c4bba0cd274e" />
