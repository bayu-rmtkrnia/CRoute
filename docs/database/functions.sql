-- =============================================================================
-- CRoute — function PostgreSQL untuk Dashboard, Riwayat, dan Statistik
-- Jalankan setelah schema.sql:  psql ... -f functions.sql
-- Semua function bersifat idempotent (CREATE OR REPLACE).
-- Zona waktu agregasi: Asia/Jakarta.
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- AGREGASI BULANAN
-- -----------------------------------------------------------------------------

-- Total emisi (opsi terpilih tiap trip) untuk satu bulan tertentu.
CREATE OR REPLACE FUNCTION fn_monthly_total(
    p_user_id UUID,
    p_year    INTEGER,
    p_month   INTEGER
)
RETURNS NUMERIC(12,4)
LANGUAGE sql
STABLE
AS $$
    SELECT COALESCE(SUM(s.chosen_emission_kg), 0)::NUMERIC(12,4)
    FROM v_trip_summary s
    WHERE s.user_id = p_user_id
      AND s.trip_date_local >= make_date(p_year, p_month, 1)
      AND s.trip_date_local <  (make_date(p_year, p_month, 1) + INTERVAL '1 month')::date;
$$;

-- Total emisi bulan berjalan (berdasarkan waktu Asia/Jakarta).
CREATE OR REPLACE FUNCTION fn_current_month_total(p_user_id UUID)
RETURNS NUMERIC(12,4)
LANGUAGE sql
STABLE
AS $$
    SELECT fn_monthly_total(
        p_user_id,
        EXTRACT(YEAR  FROM (now() AT TIME ZONE 'Asia/Jakarta'))::INTEGER,
        EXTRACT(MONTH FROM (now() AT TIME ZONE 'Asia/Jakarta'))::INTEGER
    );
$$;

-- Rasio pemakaian target bulan berjalan. 1.0 = tepat target.
-- Mengembalikan 0 kalau target belum diatur (0).
CREATE OR REPLACE FUNCTION fn_monthly_progress(p_user_id UUID)
RETURNS NUMERIC
LANGUAGE sql
STABLE
AS $$
    SELECT CASE
        WHEN target.monthly_target_kg = 0 THEN 0::NUMERIC
        ELSE fn_current_month_total(p_user_id) / target.monthly_target_kg
    END
    FROM (
        SELECT COALESCE((
            SELECT s.value::NUMERIC
            FROM settings s
            WHERE s.user_id = p_user_id
              AND s.key = 'monthly_target_kg'
        ), 0) AS monthly_target_kg
    ) AS target;
$$;

-- -----------------------------------------------------------------------------
-- AGREGASI SELURUH RIWAYAT
-- -----------------------------------------------------------------------------

-- Total penghematan dibanding moda baseline (mobil), memakai opsi terpilih,
-- untuk seluruh trip.
CREATE OR REPLACE FUNCTION fn_total_saved(p_user_id UUID)
RETURNS NUMERIC(12,4)
LANGUAGE sql
STABLE
AS $$
    SELECT COALESCE(SUM(s.saved_kg), 0)::NUMERIC(12,4)
    FROM v_trip_summary s
    WHERE s.user_id = p_user_id;
$$;

-- Rata-rata emisi opsi terpilih per trip.
CREATE OR REPLACE FUNCTION fn_average_per_trip(p_user_id UUID)
RETURNS NUMERIC(12,4)
LANGUAGE sql
STABLE
AS $$
    SELECT COALESCE(AVG(s.chosen_emission_kg), 0)::NUMERIC(12,4)
    FROM v_trip_summary s
    WHERE s.user_id = p_user_id;
$$;

-- Jumlah trip yang tersimpan.
CREATE OR REPLACE FUNCTION fn_trip_count(p_user_id UUID)
RETURNS BIGINT
LANGUAGE sql
STABLE
AS $$
    SELECT COUNT(*)::BIGINT
    FROM trips t
    WHERE t.user_id = p_user_id;
$$;

-- Moda yang paling sering dipilih pengguna. Mengembalikan 0 baris kalau kosong.
CREATE OR REPLACE FUNCTION fn_favorite_mode(p_user_id UUID)
RETURNS TABLE (
    mode_id    UUID,
    mode_name  VARCHAR(50),
    icon       VARCHAR(255),
    trip_count BIGINT
)
LANGUAGE sql
STABLE
AS $$
    SELECT
        s.chosen_mode_id,
        s.chosen_mode_name,
        s.chosen_mode_icon,
        COUNT(*)::BIGINT
    FROM v_trip_summary s
    WHERE s.user_id = p_user_id
      AND s.chosen_mode_id IS NOT NULL
    GROUP BY s.chosen_mode_id, s.chosen_mode_name, s.chosen_mode_icon
    ORDER BY COUNT(*) DESC, s.chosen_mode_name ASC
    LIMIT 1;
$$;

-- Emisi per hari untuk grafik 7 hari terakhir.
-- Hari tanpa trip tetap muncul dengan nilai 0.
CREATE OR REPLACE FUNCTION fn_daily_emissions(
    p_user_id UUID,
    p_days    INTEGER DEFAULT 7
)
RETURNS TABLE (
    day      DATE,
    total_kg NUMERIC(12,4)
)
LANGUAGE sql
STABLE
AS $$
    WITH days AS (
        SELECT generate_series(
            (now() AT TIME ZONE 'Asia/Jakarta')::date
                - (GREATEST(COALESCE(p_days, 7), 1) - 1),
            (now() AT TIME ZONE 'Asia/Jakarta')::date,
            INTERVAL '1 day'
        )::date AS day
    )
    SELECT
        d.day,
        COALESCE(SUM(s.chosen_emission_kg), 0)::NUMERIC(12,4) AS total_kg
    FROM days d
    LEFT JOIN v_trip_summary s
           ON s.user_id = p_user_id
          AND s.trip_date_local = d.day
    GROUP BY d.day
    ORDER BY d.day;
$$;

-- Beberapa trip terakhir (default 3, seperti Dashboard).
CREATE OR REPLACE FUNCTION fn_recent_trips(
    p_user_id UUID,
    p_limit   INTEGER DEFAULT 3
)
RETURNS SETOF v_trip_summary
LANGUAGE sql
STABLE
AS $$
    SELECT *
    FROM v_trip_summary
    WHERE user_id = p_user_id
    ORDER BY date DESC
    LIMIT GREATEST(COALESCE(p_limit, 3), 0);
$$;

-- -----------------------------------------------------------------------------
-- PENGATURAN TARGET
-- -----------------------------------------------------------------------------

CREATE OR REPLACE FUNCTION fn_get_monthly_target(p_user_id UUID)
RETURNS NUMERIC(10,2)
LANGUAGE sql
STABLE
AS $$
    SELECT COALESCE(
        (
            SELECT s.value::NUMERIC
            FROM settings s
            WHERE s.user_id = p_user_id
              AND s.key = 'monthly_target_kg'
        ),
        0
    )::NUMERIC(10,2);
$$;

CREATE OR REPLACE FUNCTION fn_set_monthly_target(
    p_user_id UUID,
    p_target  NUMERIC(10,2)
)
RETURNS NUMERIC(10,2)
LANGUAGE plpgsql
AS $$
BEGIN
    IF p_target IS NULL OR p_target < 0 THEN
        RAISE EXCEPTION 'Target bulanan tidak boleh negatif.';
    END IF;

    INSERT INTO settings (user_id, key, value)
    VALUES (p_user_id, 'monthly_target_kg', p_target::TEXT)
    ON CONFLICT (user_id, key) DO UPDATE
       SET value      = EXCLUDED.value,
           updated_at = now();

    RETURN p_target;
END;
$$;

-- -----------------------------------------------------------------------------
-- SIMPAN / HAPUS TRIP
-- -----------------------------------------------------------------------------

-- Hapus semua trip milik pengguna. Route & emission_results ikut terhapus
-- karena ON DELETE CASCADE. Target bulanan tidak berubah.
CREATE OR REPLACE FUNCTION fn_clear_trips(p_user_id UUID)
RETURNS INTEGER
LANGUAGE plpgsql
AS $$
DECLARE
    v_count INTEGER;
BEGIN
    DELETE FROM trips WHERE user_id = p_user_id;
    GET DIAGNOSTICS v_count = ROW_COUNT;
    RETURN v_count;
END;
$$;

-- Simpan satu trip beserta rute dan semua hasil emisi secara atomik.
-- p_chosen_mode_id adalah moda yang benar-benar dipakai pengguna. Kalau null
-- (mis. klien lama), function memakai hasil dengan emisi terendah.
-- Contoh p_results:
-- [
--   {"transport_mode_id":"...", "total_emission_kg":2.1204,
--    "emission_factor":0.171}
-- ]
DROP FUNCTION IF EXISTS fn_save_trip(UUID, TEXT, TEXT, NUMERIC, INTEGER, JSONB, TIMESTAMPTZ);
DROP FUNCTION IF EXISTS fn_save_trip(UUID, TEXT, TEXT, NUMERIC, INTEGER, JSONB, TIMESTAMPTZ, UUID);

CREATE OR REPLACE FUNCTION fn_save_trip(
    p_user_id        UUID,
    p_origin         TEXT,
    p_destination    TEXT,
    p_distance_km    NUMERIC,
    p_duration       INTEGER,
    p_results        JSONB,
    p_date           TIMESTAMPTZ DEFAULT now(),
    p_chosen_mode_id UUID DEFAULT NULL
)
RETURNS UUID
LANGUAGE plpgsql
AS $$
DECLARE
    v_trip_id        UUID;
    v_factor         NUMERIC(12,6);
    v_item           RECORD;
    v_chosen_mode_id UUID;
BEGIN
    IF p_user_id IS NULL THEN
        RAISE EXCEPTION 'User wajib diisi.';
    END IF;
    IF p_origin IS NULL OR btrim(p_origin) = '' THEN
        RAISE EXCEPTION 'Lokasi asal tidak boleh kosong.';
    END IF;
    IF p_destination IS NULL OR btrim(p_destination) = '' THEN
        RAISE EXCEPTION 'Lokasi tujuan tidak boleh kosong.';
    END IF;
    IF p_distance_km IS NULL OR p_distance_km < 0 THEN
        RAISE EXCEPTION 'Jarak tidak boleh negatif.';
    END IF;
    IF p_duration IS NULL OR p_duration < 0 THEN
        RAISE EXCEPTION 'Durasi tidak boleh negatif.';
    END IF;
    IF p_results IS NULL
       OR jsonb_typeof(p_results) <> 'array'
       OR jsonb_array_length(p_results) = 0 THEN
        RAISE EXCEPTION 'Minimal satu hasil emisi wajib diisi.';
    END IF;

    INSERT INTO trips (user_id, date)
    VALUES (p_user_id, COALESCE(p_date, now()))
    RETURNING trip_id INTO v_trip_id;

    INSERT INTO routes (trip_id, origin, destination, distance_km, duration)
    VALUES (v_trip_id, btrim(p_origin), btrim(p_destination), p_distance_km, p_duration);

    FOR v_item IN
        SELECT x.transport_mode_id,
               x.total_emission_kg,
               x.emission_factor
        FROM jsonb_to_recordset(p_results) AS x(
            transport_mode_id UUID,
            total_emission_kg NUMERIC,
            emission_factor   NUMERIC
        )
    LOOP
        IF v_item.transport_mode_id IS NULL
           OR v_item.total_emission_kg IS NULL
           OR v_item.total_emission_kg < 0 THEN
            RAISE EXCEPTION 'Hasil emisi tidak valid: %', row_to_json(v_item);
        END IF;

        SELECT tm.emission_factor
        INTO v_factor
        FROM transport_modes tm
        WHERE tm.transport_mode_id = v_item.transport_mode_id;

        IF NOT FOUND THEN
            RAISE EXCEPTION 'Moda transportasi % tidak ditemukan.',
                v_item.transport_mode_id;
        END IF;

        INSERT INTO emission_results
            (trip_id, transport_mode_id, distance_km, duration,
             total_emission_kg, emission_factor)
        VALUES
            (v_trip_id,
             v_item.transport_mode_id,
             p_distance_km,
             p_duration,
             v_item.total_emission_kg,
             COALESCE(v_item.emission_factor, v_factor));
    END LOOP;

    -- Simpan mode terpilih. Kalau pengguna tidak memilih (data lama/klien
    -- lama), fallback ke hasil dengan emisi terendah.
    IF p_chosen_mode_id IS NOT NULL THEN
        IF NOT EXISTS (
            SELECT 1
            FROM emission_results er
            WHERE er.trip_id = v_trip_id
              AND er.transport_mode_id = p_chosen_mode_id
        ) THEN
            RAISE EXCEPTION 'Moda terpilih tidak ada dalam hasil emisi.';
        END IF;

        v_chosen_mode_id := p_chosen_mode_id;
    ELSE
        SELECT er.transport_mode_id
        INTO v_chosen_mode_id
        FROM emission_results er
        WHERE er.trip_id = v_trip_id
        ORDER BY er.total_emission_kg ASC, er.created_at ASC, er.emission_id ASC
        LIMIT 1;
    END IF;

    UPDATE trips
    SET chosen_mode_id = v_chosen_mode_id
    WHERE trip_id = v_trip_id;

    RETURN v_trip_id;
END;
$$;

-- -----------------------------------------------------------------------------
-- RUTE FAVORIT (persiapan fitur berikutnya)
-- -----------------------------------------------------------------------------

CREATE OR REPLACE FUNCTION fn_set_route_favorite(
    p_user_id     UUID,
    p_route_id    UUID,
    p_is_favorite BOOLEAN
)
RETURNS BOOLEAN
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE routes r
    SET is_favorite = p_is_favorite
    FROM trips t
    WHERE r.trip_id = t.trip_id
      AND r.route_id = p_route_id
      AND t.user_id = p_user_id;

    RETURN FOUND;
END;
$$;

-- -----------------------------------------------------------------------------
-- AUTENTIKASI
-- -----------------------------------------------------------------------------

-- Dipanggil saat register. Password sudah di-hash di C#.
-- Mengembalikan UUID user baru, atau NULL kalau username sudah dipakai.
CREATE OR REPLACE FUNCTION fn_register_user(
    p_full_name    TEXT,
    p_username     TEXT,
    p_password_hash BYTEA,
    p_password_salt BYTEA,
    p_iterations   INTEGER DEFAULT 100000,
    p_algorithm    TEXT    DEFAULT 'pbkdf2-sha256'
)
RETURNS UUID
LANGUAGE plpgsql
AS $$
DECLARE
    v_id UUID;
BEGIN
    IF p_full_name IS NULL OR btrim(p_full_name) = '' THEN
        RAISE EXCEPTION 'Nama lengkap tidak boleh kosong.';
    END IF;
    IF p_username IS NULL OR btrim(p_username) = '' THEN
        RAISE EXCEPTION 'Username tidak boleh kosong.';
    END IF;
    IF p_password_hash IS NULL OR p_password_salt IS NULL THEN
        RAISE EXCEPTION 'Password hash dan salt wajib diisi.';
    END IF;

    INSERT INTO users
        (full_name, username, password_hash, password_salt,
         password_iterations, password_algorithm)
    VALUES
        (btrim(p_full_name),
         lower(btrim(p_username)),
         p_password_hash,
         p_password_salt,
         p_iterations,
         p_algorithm)
    RETURNING id INTO v_id;

    RETURN v_id;
EXCEPTION
    WHEN unique_violation THEN
        RETURN NULL;
END;
$$;

-- Data yang dibutuhkan untuk login. Verifikasi PBKDF2 tetap di C#.
CREATE OR REPLACE FUNCTION fn_get_user_for_login(p_username TEXT)
RETURNS TABLE (
    id                  UUID,
    full_name           VARCHAR(100),
    password_hash       BYTEA,
    password_salt       BYTEA,
    password_iterations INTEGER,
    password_algorithm  VARCHAR(30)
)
LANGUAGE sql
STABLE
AS $$
    SELECT u.id,
           u.full_name,
           u.password_hash,
           u.password_salt,
           u.password_iterations,
           u.password_algorithm
    FROM users u
    WHERE lower(u.username) = lower(btrim(p_username))
    LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION fn_username_available(p_username TEXT)
RETURNS BOOLEAN
LANGUAGE sql
STABLE
AS $$
    SELECT NOT EXISTS (
        SELECT 1
        FROM users u
        WHERE lower(u.username) = lower(btrim(p_username))
    );
$$;

COMMIT;
