-- =============================================================================
-- CRoute — migrasi database lama ke struktur minimal target
--
-- Jalankan SEKALI pada database yang sudah memakai schema lama:
--   psql ... -f docs/database/migration-minimal-target.sql
--
-- Setelah file ini, WAJIB jalankan versi terbaru:
--   psql ... -f docs/database/functions.sql
--
-- Perubahan:
--   - nama kolom disamakan dengan tabel target
--   - routes.is_favorite, routes.label
--   - trips.chosen_mode_id
--   - emission_results.distance_km, emission_results.duration
--   - settings(user_id, key, value) menggantikan user_settings
--   - view disesuaikan dengan nama kolom baru
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Hapus view lama; function yang bergantung akan dibuat ulang oleh
--    functions.sql setelah migrasi ini.
-- -----------------------------------------------------------------------------

DROP VIEW IF EXISTS v_trip_summary CASCADE;
DROP VIEW IF EXISTS v_trip_best_result CASCADE;

-- -----------------------------------------------------------------------------
-- 2. Rename kolom agar sama dengan target
-- -----------------------------------------------------------------------------

ALTER TABLE transport_modes RENAME COLUMN id TO transport_mode_id;
ALTER TABLE transport_modes RENAME COLUMN icon_name TO icon;
ALTER TABLE transport_modes RENAME COLUMN emission_factor_kg_per_km TO emission_factor;

ALTER TABLE trips RENAME COLUMN id TO trip_id;
ALTER TABLE trips RENAME COLUMN trip_date TO date;

ALTER TABLE routes RENAME COLUMN id TO route_id;
ALTER TABLE routes RENAME COLUMN duration_min TO duration;

ALTER TABLE emission_results RENAME COLUMN id TO emission_id;
ALTER TABLE emission_results RENAME COLUMN emission_factor_kg_per_km TO emission_factor;

-- -----------------------------------------------------------------------------
-- 3. Kolom baru
-- -----------------------------------------------------------------------------

ALTER TABLE routes
    ADD COLUMN IF NOT EXISTS is_favorite BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS label       VARCHAR(100);

ALTER TABLE trips
    ADD COLUMN IF NOT EXISTS chosen_mode_id UUID;

ALTER TABLE emission_results
    ADD COLUMN IF NOT EXISTS distance_km NUMERIC(8,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS duration    INTEGER      NOT NULL DEFAULT 0;

-- FK chosen_mode_id
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'trips_chosen_mode_id_fkey'
    ) THEN
        ALTER TABLE trips
            ADD CONSTRAINT trips_chosen_mode_id_fkey
            FOREIGN KEY (chosen_mode_id)
            REFERENCES transport_modes(transport_mode_id)
            ON DELETE SET NULL;
    END IF;
END;
$$;

-- Constraint nilai non-negatif untuk kolom baru.
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'emission_results_distance_non_negative'
    ) THEN
        ALTER TABLE emission_results
            ADD CONSTRAINT emission_results_distance_non_negative
            CHECK (distance_km >= 0);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'emission_results_duration_non_negative'
    ) THEN
        ALTER TABLE emission_results
            ADD CONSTRAINT emission_results_duration_non_negative
            CHECK (duration >= 0);
    END IF;
END;
$$;

-- -----------------------------------------------------------------------------
-- 4. Backfill data lama
-- -----------------------------------------------------------------------------

-- distance/duration per hasil emisi diambil dari rute lama.
UPDATE emission_results er
SET distance_km = r.distance_km,
    duration    = r.duration
FROM trips t
JOIN routes r ON r.trip_id = t.trip_id
WHERE er.trip_id = t.trip_id
  AND (er.distance_km = 0 OR er.duration = 0);

-- chosen_mode_id = hasil emisi terendah.
UPDATE trips t
SET chosen_mode_id = (
    SELECT er.transport_mode_id
    FROM emission_results er
    WHERE er.trip_id = t.trip_id
    ORDER BY er.total_emission_kg ASC, er.created_at ASC, er.emission_id ASC
    LIMIT 1
)
WHERE t.chosen_mode_id IS NULL;

-- -----------------------------------------------------------------------------
-- 5. Index baru
-- -----------------------------------------------------------------------------

CREATE INDEX IF NOT EXISTS trips_chosen_mode_idx
    ON trips (chosen_mode_id);

CREATE INDEX IF NOT EXISTS routes_favorite_idx
    ON routes (is_favorite)
    WHERE is_favorite;

-- -----------------------------------------------------------------------------
-- 6. Settings: key/value per user, migrasi dari user_settings
-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS settings (
    user_id    UUID         NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    key        VARCHAR(50)  NOT NULL,
    value      VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ  NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, key)
);

INSERT INTO settings (user_id, key, value)
SELECT user_id, 'monthly_target_kg', monthly_target_kg::text
FROM user_settings
ON CONFLICT (user_id, key) DO UPDATE
SET value      = EXCLUDED.value,
    updated_at = now();

DROP TRIGGER IF EXISTS trg_settings_updated_at ON settings;
CREATE TRIGGER trg_settings_updated_at
    BEFORE UPDATE ON settings
    FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();

-- Trigger default user baru harus menulis ke settings, bukan user_settings.
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

-- -----------------------------------------------------------------------------
-- 7. Buat ulang view dengan nama kolom baru
-- -----------------------------------------------------------------------------

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
        WHEN b.emission_result_id IS NULL THEN 0
        ELSE GREATEST(
            0::numeric,
            COALESCE(ROUND(r.distance_km * bm.emission_factor, 4), 0)
            - b.total_emission_kg
        )
    END                                            AS saved_kg
FROM trips t
JOIN routes r ON r.trip_id = t.trip_id
LEFT JOIN v_trip_best_result b ON b.trip_id = t.trip_id
LEFT JOIN transport_modes tm ON tm.transport_mode_id = b.transport_mode_id
LEFT JOIN LATERAL (
    SELECT emission_factor
    FROM transport_modes
    WHERE is_baseline AND is_active
    LIMIT 1
) bm ON TRUE;

COMMIT;

-- =============================================================================
-- SETELAH INI:
--   psql ... -f docs/database/functions.sql
--
-- Setelah functions.sql berhasil dan aplikasi diuji, tabel lama boleh dihapus:
--   DROP TABLE IF EXISTS user_settings;
-- =============================================================================
