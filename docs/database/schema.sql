-- =============================================================================
-- CRoute — skema database PostgreSQL
-- Jalankan sekali di database kosong:  psql ... -f schema.sql
-- Setelah ini jalankan functions.sql.
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. TABEL
-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS users (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    full_name           VARCHAR(100) NOT NULL,
    username            VARCHAR(50)  NOT NULL,
    password_hash       BYTEA        NOT NULL,
    password_salt       BYTEA        NOT NULL,
    password_iterations INTEGER      NOT NULL DEFAULT 100000,
    password_algorithm  VARCHAR(30)  NOT NULL DEFAULT 'pbkdf2-sha256',
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ  NOT NULL DEFAULT now(),
    CONSTRAINT users_full_name_not_blank CHECK (length(btrim(full_name)) > 0),
    CONSTRAINT users_username_not_blank  CHECK (length(btrim(username)) > 0),
    CONSTRAINT users_iterations_positive CHECK (password_iterations > 0)
);

-- Username case-insensitive. Aplikasi tetap menormalkan ke huruf kecil.
CREATE UNIQUE INDEX IF NOT EXISTS users_username_unique_ci
    ON users (lower(username));

CREATE TABLE IF NOT EXISTS transport_modes (
    transport_mode_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name              VARCHAR(50)   NOT NULL,
    icon              VARCHAR(255)  NOT NULL,
    emission_factor   NUMERIC(12,6) NOT NULL,
    is_baseline       BOOLEAN       NOT NULL DEFAULT FALSE,
    is_active         BOOLEAN       NOT NULL DEFAULT TRUE,
    sort_order        INTEGER       NOT NULL DEFAULT 0,
    created_at        TIMESTAMPTZ   NOT NULL DEFAULT now(),
    updated_at        TIMESTAMPTZ   NOT NULL DEFAULT now(),
    CONSTRAINT transport_modes_name_unique UNIQUE (name),
    CONSTRAINT transport_modes_factor_non_negative CHECK (emission_factor >= 0),
    CONSTRAINT transport_modes_icon_not_blank CHECK (length(btrim(icon)) > 0)
);

-- Hanya boleh ada satu moda acuan (baseline).
CREATE UNIQUE INDEX IF NOT EXISTS transport_modes_one_baseline
    ON transport_modes (is_baseline)
    WHERE is_baseline;

-- Pengaturan generik per pengguna, mengikuti bentuk SETTING(key, value)
-- pada rancangan target. Primary key komposit dipakai karena multi-user.
CREATE TABLE IF NOT EXISTS settings (
    user_id    UUID         NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    key        VARCHAR(50)  NOT NULL,
    value      VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ  NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, key)
);

CREATE TABLE IF NOT EXISTS trips (
    trip_id        UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id        UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    date           TIMESTAMPTZ NOT NULL DEFAULT now(),
    chosen_mode_id UUID REFERENCES transport_modes(transport_mode_id) ON DELETE SET NULL,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS trips_user_date_idx
    ON trips (user_id, date DESC);

CREATE INDEX IF NOT EXISTS trips_chosen_mode_idx
    ON trips (chosen_mode_id);

CREATE TABLE IF NOT EXISTS routes (
    route_id     UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    trip_id      UUID NOT NULL UNIQUE REFERENCES trips(trip_id) ON DELETE CASCADE,
    origin       VARCHAR(255)  NOT NULL,
    destination  VARCHAR(255)  NOT NULL,
    is_favorite  BOOLEAN       NOT NULL DEFAULT FALSE,
    label        VARCHAR(100),
    distance_km  NUMERIC(10,2) NOT NULL,
    duration     INTEGER       NOT NULL,
    created_at   TIMESTAMPTZ   NOT NULL DEFAULT now(),
    CONSTRAINT routes_origin_not_blank      CHECK (length(btrim(origin)) > 0),
    CONSTRAINT routes_destination_not_blank CHECK (length(btrim(destination)) > 0),
    CONSTRAINT routes_distance_non_negative CHECK (distance_km >= 0),
    CONSTRAINT routes_duration_non_negative CHECK (duration >= 0)
);

CREATE INDEX IF NOT EXISTS routes_favorite_idx
    ON routes (is_favorite)
    WHERE is_favorite;

CREATE TABLE IF NOT EXISTS emission_results (
    emission_id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    trip_id                   UUID NOT NULL REFERENCES trips(trip_id) ON DELETE CASCADE,
    transport_mode_id         UUID NOT NULL REFERENCES transport_modes(transport_mode_id) ON DELETE RESTRICT,
    distance_km               NUMERIC(8,2)  NOT NULL DEFAULT 0,
    duration                  INTEGER       NOT NULL DEFAULT 0,
    total_emission_kg         NUMERIC(12,4) NOT NULL,
    emission_factor           NUMERIC(12,6) NOT NULL DEFAULT 0,
    created_at                TIMESTAMPTZ   NOT NULL DEFAULT now(),
    CONSTRAINT emission_results_unique_mode UNIQUE (trip_id, transport_mode_id),
    CONSTRAINT emission_results_distance_non_negative CHECK (distance_km >= 0),
    CONSTRAINT emission_results_duration_non_negative CHECK (duration >= 0),
    CONSTRAINT emission_results_total_non_negative  CHECK (total_emission_kg >= 0),
    CONSTRAINT emission_results_factor_non_negative CHECK (emission_factor >= 0)
);

CREATE INDEX IF NOT EXISTS emission_results_trip_idx
    ON emission_results (trip_id);

CREATE INDEX IF NOT EXISTS emission_results_mode_idx
    ON emission_results (transport_mode_id);

-- -----------------------------------------------------------------------------
-- 2. TRIGGER
-- -----------------------------------------------------------------------------

CREATE OR REPLACE FUNCTION fn_set_updated_at()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_users_updated_at ON users;
CREATE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();

DROP TRIGGER IF EXISTS trg_transport_modes_updated_at ON transport_modes;
CREATE TRIGGER trg_transport_modes_updated_at
    BEFORE UPDATE ON transport_modes
    FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();

DROP TRIGGER IF EXISTS trg_settings_updated_at ON settings;
CREATE TRIGGER trg_settings_updated_at
    BEFORE UPDATE ON settings
    FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();

-- Setiap user baru otomatis mendapat target bulanan awal 15 kg.
CREATE OR REPLACE FUNCTION fn_users_create_default_settings()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO settings (user_id, key, value)
    VALUES (NEW.id, 'monthly_target_kg', '15.00')
    ON CONFLICT (user_id, key) DO NOTHING;

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_users_default_settings ON users;
CREATE TRIGGER trg_users_default_settings
    AFTER INSERT ON users
    FOR EACH ROW EXECUTE FUNCTION fn_users_create_default_settings();

-- -----------------------------------------------------------------------------
-- 3. SEED MASTER MODA TRANSPORTASI
-- -----------------------------------------------------------------------------

INSERT INTO transport_modes
    (transport_mode_id, name, icon, emission_factor, is_baseline, is_active, sort_order)
VALUES
    ('00000000-0000-4000-8000-000000000001', 'Mobil',      'car',        0.171000, TRUE,  TRUE, 1),
    ('00000000-0000-4000-8000-000000000002', 'Motor',      'motorcycle', 0.103000, FALSE, TRUE, 2),
    ('00000000-0000-4000-8000-000000000003', 'Bus',        'bus',        0.089000, FALSE, TRUE, 3),
    ('00000000-0000-4000-8000-000000000004', 'Kereta',     'train',      0.041000, FALSE, TRUE, 4),
    ('00000000-0000-4000-8000-000000000005', 'Sepeda',     'bicycle',    0.000000, FALSE, TRUE, 5),
    ('00000000-0000-4000-8000-000000000006', 'Jalan kaki', 'walk',       0.000000, FALSE, TRUE, 6)
ON CONFLICT (name) DO NOTHING;

-- -----------------------------------------------------------------------------
-- 4. SEED AKUN DEMO (opsional)
--    username: demo / password: demo123
--    PBKDF2-HMAC-SHA256, 100000 iterasi, salt 000102...0f
--    Hapus blok ini kalau database produksi tidak boleh punya akun demo.
-- -----------------------------------------------------------------------------

INSERT INTO users
    (id, full_name, username, password_hash, password_salt,
     password_iterations, password_algorithm)
VALUES
    (
        'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
        'Akun Demo',
        'demo',
        decode('758fd3d16a017f1fafe7a1ad793cf4faeb69ce902b17842308bfda0f1d724188', 'hex'),
        decode('000102030405060708090a0b0c0d0e0f', 'hex'),
        100000,
        'pbkdf2-sha256'
    )
ON CONFLICT DO NOTHING;

-- -----------------------------------------------------------------------------
-- 5. VIEW
-- -----------------------------------------------------------------------------

-- Satu baris per trip: hasil emisi dengan nilai paling kecil.
CREATE OR REPLACE VIEW v_trip_best_result AS
SELECT DISTINCT ON (er.trip_id)
       er.trip_id,
       er.emission_id               AS emission_result_id,
       er.transport_mode_id,
       er.total_emission_kg,
       er.emission_factor
FROM emission_results er
ORDER BY er.trip_id,
         er.total_emission_kg ASC,
         er.created_at ASC,
         er.emission_id ASC;

-- Satu baris per trip: hasil yang dipilih pengguna. Kalau belum ada pilihan
-- (data lama), fallback ke hasil dengan emisi terkecil.
CREATE OR REPLACE VIEW v_trip_chosen_result AS
SELECT t.trip_id,
       er.emission_id               AS emission_result_id,
       er.transport_mode_id,
       er.total_emission_kg,
       er.emission_factor
FROM trips t
JOIN LATERAL (
    SELECT er.emission_id,
           er.transport_mode_id,
           er.total_emission_kg,
           er.emission_factor
    FROM emission_results er
    WHERE er.trip_id = t.trip_id
    ORDER BY (er.transport_mode_id = t.chosen_mode_id) DESC,
             er.total_emission_kg ASC,
             er.created_at ASC,
             er.emission_id ASC
    LIMIT 1
) er ON TRUE;

-- Data siap pakai untuk Dashboard, Riwayat, dan Statistik.
CREATE OR REPLACE VIEW v_trip_summary AS
SELECT
    t.trip_id                                      AS trip_id,
    t.user_id,
    t.date,
    (t.date AT TIME ZONE 'Asia/Jakarta')::date     AS trip_date_local,
    r.route_id                                     AS route_id,
    r.origin,
    r.destination,
    r.distance_km,
    r.duration,
    b.emission_result_id                           AS best_result_id,
    b.transport_mode_id                            AS best_mode_id,
    tm.name                                        AS best_mode_name,
    tm.icon                                        AS best_mode_icon,
    COALESCE(b.total_emission_kg, 0)               AS best_emission_kg,
    COALESCE(ROUND(r.distance_km * bm.emission_factor, 4), 0)
                                                   AS baseline_emission_kg,
    CASE
        WHEN c.emission_result_id IS NULL THEN 0
        ELSE GREATEST(
            0::numeric,
            COALESCE(ROUND(r.distance_km * bm.emission_factor, 4), 0)
            - c.total_emission_kg
        )
    END                                            AS saved_kg,
    c.emission_result_id                           AS chosen_result_id,
    c.transport_mode_id                            AS chosen_mode_id,
    ctm.name                                       AS chosen_mode_name,
    ctm.icon                                       AS chosen_mode_icon,
    COALESCE(c.total_emission_kg, 0)               AS chosen_emission_kg
FROM trips t
JOIN routes r ON r.trip_id = t.trip_id
LEFT JOIN v_trip_best_result b ON b.trip_id = t.trip_id
LEFT JOIN transport_modes tm ON tm.transport_mode_id = b.transport_mode_id
LEFT JOIN v_trip_chosen_result c ON c.trip_id = t.trip_id
LEFT JOIN transport_modes ctm ON ctm.transport_mode_id = c.transport_mode_id
LEFT JOIN LATERAL (
    SELECT emission_factor
    FROM transport_modes
    WHERE is_baseline AND is_active
    LIMIT 1
) bm ON TRUE;

COMMIT;
