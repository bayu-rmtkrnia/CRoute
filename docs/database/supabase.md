# Menggunakan Supabase sebagai Database CRoute

Supabase menyediakan PostgreSQL terkelola. Aplikasi CRoute tetap memakai
`Npgsql`/EF Core untuk terhubung langsung ke database; **tidak wajib** memakai
Supabase Auth, REST, atau Storage.

Ada dua opsi arsitektur:

| Opsi | Auth | Koneksi database | Perubahan kode |
|---|---|---|---|
| **A. Direct PostgreSQL (disarankan untuk CRoute sekarang)** | Tetap `users` + PBKDF2 milik aplikasi | Npgsql / EF Core ke Supabase | Paling sedikit, tinggal ganti connection string |
| B. Supabase Auth + REST | Supabase Auth (email/password) | Supabase client / PostgREST | Login/register harus dirombak |

Dokumen ini fokus pada **Opsi A**, karena UI CRoute sudah punya login/register
sendiri dan skema `users` yang cocok.

---

## 1. Key/kredensial yang perlu disiapkan

Untuk Opsi A (Npgsql/EF Core), **yang benar-benar diperlukan hanya dua**:

1. **Database password** — dibuat saat membuat project Supabase.
2. **Connection string** — diambil dari Dashboard → **Connect**.

Kredensial lain **tidak diperlukan** kecuali kamu memakai fitur Supabase lain:

| Key / kredensial | Letak di Supabase | Perlu untuk Npgsql? | Catatan |
|---|---|---|---|
| **Database password** | Settings → Database (atau saat create project) | ✅ **Ya** | Bisa di-reset kalau lupa |
| **Connection string** | Tombol **Connect** di dashboard | ✅ **Ya** | Pilih mode pooler yang sesuai |
| **Project ref** | URL project: `https://<project-ref>.supabase.co` | ✅ Untuk pooler | Dipakai di username pooler |
| Project URL | Settings → API | ❌ | Hanya untuk REST/Auth/Storage |
| **Publishable key** (`sb_publishable_...`, dulu **anon key**) | Settings → API Keys | ❌ | Hanya untuk Supabase client; aman di client **jika RLS aktif** |
| **Secret key** (`sb_secret_...`, dulu **service_role key**) | Settings → API Keys | ❌ | **Rahasia server**. Jangan pernah dimasukkan ke aplikasi desktop |
| JWT secret | Settings → API | ❌ | Hanya untuk verifikasi token Supabase Auth |
| Personal access token | Account → Access Tokens | ❌ | Hanya untuk Supabase CLI |
| CA certificate | Settings → Database → SSL | ⚠️ Opsional | Untuk `SSL Mode=VerifyFull` |

> Supabase sedang mengganti nama key: `anon` → **publishable**
> (`sb_publishable_...`) dan `service_role` → **secret** (`sb_secret_...`).
> Key lama masih berfungsi sampai akhir 2026, tetapi untuk proyek baru pakai
> nama baru. Untuk CRoute dengan Npgsql, **keduanya tidak diperlukan**.

> **Penting:** aplikasi desktop yang terhubung langsung ke database akan
> menyimpan connection string (termasuk password) di mesin pengguna. Untuk
> proyek kuliah ini wajar, tetapi untuk produksi sebaiknya lewat backend API
> agar password database tidak pernah dikirim ke client.

---

## 2. Membuat project Supabase

1. Buka <https://supabase.com/dashboard>, login.
2. **New project**.
3. Isi:
   - **Name:** `croute`
   - **Database Password:** buat password kuat, **simpan**.
   - **Region:** pilih yang terdekat, misalnya **Southeast Asia (Singapore)**.
4. Tunggu sampai project selesai provisioning (±1–2 menit).
5. Catat **project ref** dari URL dashboard:
   `https://supabase.com/dashboard/project/<project-ref>`

---

## 3. Mengambil connection string

Buka dashboard project → tombol **Connect** (atau Settings → Database →
Connection string). Ada tiga mode:

### a. Direct connection (IPv6)

```text
Host=db.<project-ref>.supabase.co;Port=5432;Database=postgres;Username=croute;Password=<DB_PASSWORD>;SSL Mode=Require
```

- Paling sederhana, tanpa pooler tambahan.
- **Hanya IPv6** (tanpa add-on IPv4 berbayar). Banyak jaringan di Indonesia
  belum mendukung IPv6, jadi sering gagal.
- Cocok untuk `psql`/migration kalau jaringan mendukung IPv6.

### b. Session pooler (disarankan untuk aplikasi desktop)

```text
Host=aws-0-<region>.pooler.supabase.com;Port=5432;Database=postgres;Username=croute.<project-ref>;Password=<DB_PASSWORD>;SSL Mode=Require
```

- Mendukung IPv4.
- Mendukung prepared statement dan session setting.
- **Gunakan mode ini untuk aplikasi CRoute dan untuk `dotnet ef database update`.**

### c. Transaction pooler

```text
Host=aws-0-<region>.pooler.supabase.com;Port=6543;Database=postgres;Username=croute.<project-ref>;Password=<DB_PASSWORD>;SSL Mode=Require
```

- Untuk aplikasi serverless yang banyak koneksi sebentar.
- **Tidak cocok** untuk migration, prepared statement, atau session setting.
- Jangan dipakai untuk CRoute kecuali kamu paham konsekuensinya.

> Ganti `<region>`, `<project-ref>`, dan `<DB_PASSWORD>` sesuai dashboard.
> Dashboard menampilkan host pooler yang tepat (mis. `aws-0-ap-southeast-1`
> atau `aws-1-ap-southeast-1`); selalu salin dari sana.

### SSL

Supabase mewajibkan SSL. Dua pilihan:

**Cepat (development):**

```text
SSL Mode=Require;Trust Server Certificate=true
```

**Aman (disarankan):**

1. Dashboard → Settings → Database → **SSL Configuration** → download
   certificate (mis. `prod-ca-2021.crt`).
2. Simpan di luar repository, misalnya
   `C:\Users\<user>\.croute\prod-ca-2021.crt`.
3. Connection string:

```text
SSL Mode=VerifyFull;Root Certificate=C:\Users\<user>\.croute\prod-ca-2021.crt
```

Jangan commit file certificate atau password ke Git.

---

## 4. Membuat role database khusus aplikasi

Jangan memakai role `postgres` (superuser) dari aplikasi. Buat role terbatas.

> Untuk proyek ini role-nya bernama **`croute`**. Skrip siap pakai ada di
> `supabase-croute-role.sql`; ganti `GANTI_PASSWORD` dengan password role.

Buka **SQL Editor** di dashboard Supabase, lalu jalankan (ganti password):

```sql
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'croute') THEN
        CREATE ROLE croute LOGIN;
    END IF;
END;
$$;

ALTER ROLE croute WITH LOGIN PASSWORD 'PASSWORD_KUAT_DI_SINI';

GRANT CONNECT ON DATABASE postgres TO croute;
GRANT USAGE   ON SCHEMA public       TO croute;

GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES    IN SCHEMA public TO croute;
GRANT USAGE, SELECT                  ON ALL SEQUENCES IN SCHEMA public TO croute;
GRANT EXECUTE                        ON ALL FUNCTIONS IN SCHEMA public TO croute;

-- Untuk objek baru yang dibuat role postgres di masa depan:
ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO croute;
ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO croute;
ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public
    GRANT EXECUTE ON FUNCTIONS TO croute;
```

Kalau nanti kamu memakai Supabase CLI/migration, pastikan migration dijalankan
sebagai role yang punya hak DDL (mis. `postgres`), bukan `croute`.

---

## 5. Menjalankan skema dan function CRoute

Urutannya:

1. `docs/database/schema.sql`
2. `docs/database/functions.sql`

### Cara A — SQL Editor Supabase (paling mudah)

1. Dashboard → **SQL Editor** → **New query**.
2. Buka `schema.sql`, salin seluruh isinya.
3. **Hapus baris `BEGIN;` dan `COMMIT;` paling atas/bawah** (SQL Editor sudah
   menangani transaksi sendiri; `BEGIN/COMMIT` manual bisa memunculkan warning).
4. Klik **Run**.
5. Ulangi untuk `functions.sql`.
6. Cek:

```sql
SELECT name, emission_factor, is_baseline
FROM transport_modes
ORDER BY sort_order;

SELECT username, full_name FROM users; -- akun demo, kalau seed dijalankan
```

### Cara B — `psql`

Kalau `psql` terpasang dan jaringan mendukung IPv6/direct connection:

```powershell
psql "postgresql://postgres:<DB_PASSWORD>@db.<project-ref>.supabase.co:5432/postgres?sslmode=require" `
  -f docs/database/schema.sql

psql "postgresql://postgres:<DB_PASSWORD>@db.<project-ref>.supabase.co:5432/postgres?sslmode=require" `
  -f docs/database/functions.sql
```

Atau lewat session pooler (IPv4):

```powershell
psql "postgresql://croute.<project-ref>:<DB_PASSWORD>@aws-0-<region>.pooler.supabase.com:5432/postgres?sslmode=require" `
  -f docs/database/functions.sql
```

> `schema.sql` membuat tabel di schema `public` pada database `postgres`
> (database default Supabase). Tidak perlu membuat database baru.

### Akun demo

`schema.sql` menyertakan akun `demo` / `demo123`. Untuk database yang dipakai
bersama, hapus blok `INSERT INTO users` di akhir `schema.sql` sebelum dijalankan.

---

## 6. Memasang connection string di aplikasi

Jangan menaruh password asli di `appsettings.json` yang di-commit. Repository
ini **masih men-track `appsettings.json`** (yang di-ignore baru
`.gitignore.example`), jadi lakukan salah satu:

### Opsi 1 — environment variable (disarankan)

Tambahkan dukungan environment variable di `ApiSettingsLoader`:

```csharp
var configuration = new ConfigurationBuilder()
    .SetBasePath(basePath ?? AppContext.BaseDirectory)
    .AddJsonFile(ApiSettingsLoader.DefaultFileName, optional: true)
    .AddEnvironmentVariables("CROUTE_")
    .Build();
```

Lalu set di PowerShell:

```powershell
$env:CROUTE_Database__ConnectionString = "Host=aws-0-<region>.pooler.supabase.com;Port=5432;Database=postgres;Username=croute.<project-ref>;Password=<DB_PASSWORD>;SSL Mode=Require;Trust Server Certificate=true"
```

Di Visual Studio: **Project → CRoute.App Properties → Debug → Environment
variables**, tambahkan `CROUTE_Database__ConnectionString`.

### Opsi 2 — file lokal yang di-ignore

Buat `appsettings.local.json` (tambahkan ke `.gitignore`), lalu muat sebagai
file opsional:

```csharp
.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false)
```

Isi:

```json
{
  "Database": {
    "ConnectionString": "Host=...;Port=5432;Database=postgres;Username=croute.<project-ref>;Password=...;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

### Jangan lakukan ini

- Jangan commit password/connection string ke Git.
- Jangan memakai **secret key** (`sb_secret_...`) atau `service_role` lama di
  aplikasi desktop.
- Jangan membagikan connection string di grup chat/tugas.

Kalau `appsettings.json` sudah terlanjur berisi password, hapus dari tracking:

```powershell
git rm --cached CRoute.Services/appsettings.json
```

lalu pastikan `appsettings.json` masuk `.gitignore`.

---

## 7. Uji koneksi

Sebelum mengubah seluruh aplikasi, uji dengan potongan C#:

```csharp
using Npgsql;

string cs = Environment.GetEnvironmentVariable("CROUTE_Database__ConnectionString")!;

await using var conn = new NpgsqlConnection(cs);
await conn.OpenAsync();

await using var cmd = new NpgsqlCommand("SELECT count(*) FROM transport_modes", conn);
Console.WriteLine(await cmd.ExecuteScalarAsync()); // harus 6
```

Kalau berhasil, lanjutkan implementasi repository sesuai
`docs/database/csharp-functions.md`, lalu ganti fake di `App.xaml.cs`.

---

## 8. Catatan EF Core + Supabase

- Gunakan `Npgsql.EntityFrameworkCore.PostgreSQL` (bukan hanya `Npgsql`).
- Untuk `dotnet ef database update`, pakai **session pooler (port 5432)** atau
  direct connection. **Jangan** transaction pooler (port 6543).
- Supabase juga punya connection pooler; jangan set `Maximum Pool Size` Npgsql
  terlalu besar. Untuk aplikasi desktop, 5–10 sudah cukup:
  `Maximum Pool Size=10;Timeout=15`.
- Jika memakai `appsettings.json` untuk migration, set environment variable
  `CROUTE_DB_CONNECTION` seperti di `csharp-functions.md`.
- Supabase free tier **pause** setelah periode tidak aktif. Koneksi pertama
  setelah pause bisa lambat/timeout; buka dashboard untuk membangunkan project.

---

## 9. RLS (Row Level Security) — baca sebelum mengaktifkan

Supabase identik dengan RLS, tetapi RLS untuk koneksi Npgsql langsung **tidak
otomatis** memakai `auth.uid()`; `auth.uid()` hanya tersedia kalau request
melewati Supabase Auth/PostgREST dengan JWT.

Untuk CRoute Opsi A:

- **Disarankan:** RLS **tidak diaktifkan** untuk tabel aplikasi; isolasi data
  dilakukan lewat `user_id` di query/function dan role `croute` yang
  terbatas. Ini paling sederhana dan sesuai arsitektur desktop saat ini.
- **Kalau tetap ingin RLS:** aplikasi harus menjalankan
  `SET app.user_id = '<uuid>'` di setiap koneksi, lalu policy memakai
  `current_setting('app.user_id')::uuid`. Ini hanya aman dengan **session
  pooler** (bukan transaction pooler).
- **Kalau nanti memakai Supabase Auth/REST:** gunakan `auth.uid()` dan policy
  standar Supabase; jangan pakai tabel `users` dengan password sendiri lagi.

Ingat: selama password database ada di aplikasi desktop, pengguna teknis tetap
bisa mengekstraknya. Untuk keamanan produksi, pindahkan akses database ke
backend API.

---

## 10. Troubleshooting umum

| Gejala | Penyebab | Solusi |
|---|---|---|
| `No such host is known` / timeout | Jaringan tidak mendukung IPv6, memakai direct connection | Pakai **session pooler** (IPv4) |
| `password authentication failed` | Password salah, atau username pooler salah format | Pooler: `croute.<project-ref>`; direct: `croute` |
| `SSL required` | Connection string tanpa SSL | Tambahkan `SSL Mode=Require` |
| `prepared statement already exists` | Memakai transaction pooler | Pakai session pooler port 5432 |
| Migration gagal | Transaction pooler | Pakai session pooler/direct untuk migration |
| Koneksi pertama lambat | Project free tier paused | Buka dashboard Supabase untuk wake up |
| `permission denied for table ...` | Role belum diberi grant | Jalankan blok `GRANT` di bagian 4 |
| `function ... does not exist` | `functions.sql` belum dijalankan | Jalankan `functions.sql` |
| `relation "transport_modes" does not exist` | `schema.sql` belum dijalankan / schema salah | Jalankan `schema.sql` di database `postgres`, schema `public` |

---

## 11. Mendukung PostgreSQL lokal dan Supabase sekaligus

Karena keduanya sama-sama PostgreSQL dan aplikasi memakai Npgsql/EF Core,
**tidak ada perubahan kode** saat berpindah provider. Cukup ganti connection
string.

Contoh strategi:

| Environment | Connection string |
|---|---|
| Lokal (Docker/PostgreSQL) | `Host=localhost;Port=5432;Database=croute;Username=postgres;Password=postgres` |
| Supabase (session pooler) | `Host=aws-0-<region>.pooler.supabase.com;Port=5432;Database=postgres;Username=croute.<project-ref>;Password=<DB_PASSWORD>;SSL Mode=Require` |

Cara memilih saat runtime:

1. Simpan connection string lokal di `appsettings.json` (tanpa password rahasia
   produksi), lalu override dengan environment variable
   `CROUTE_Database__ConnectionString` saat memakai Supabase.
2. Atau buat dua file: `appsettings.Local.json` dan `appsettings.Supabase.json`,
   keduanya di-ignore Git, lalu pilih lewat environment variable
   `CROUTE_ENVIRONMENT` / `DOTNET_ENVIRONMENT`.
3. Jangan membuat percabangan kode `if (supabase) ...`; perbedaan hanya di
   konfigurasi koneksi dan SSL.

Kalau nanti butuh migration otomatis saat start, pastikan mode koneksi yang
dipakai mendukung DDL (session pooler/direct), bukan transaction pooler.

## 12. Ringkasan checklist

- [ ] Buat project Supabase, simpan **database password**.
- [ ] Catat **project ref**.
- [ ] Ambil **session pooler connection string** dari tombol Connect.
- [ ] Buat role `croute` + grant (bagian 4).
- [ ] Jalankan `schema.sql` lalu `functions.sql` (hapus `BEGIN/COMMIT` jika lewat SQL Editor).
- [ ] Simpan connection string di environment variable / file yang di-ignore.
- [ ] Uji `SELECT count(*) FROM transport_modes` → hasil 6.
- [ ] Implementasikan repository PostgreSQL, ganti fake di `App.xaml.cs`.
- [ ] Jangan pernah memakai secret key (`sb_secret_...`) / `service_role` di
      aplikasi desktop.
