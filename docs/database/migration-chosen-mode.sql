-- =============================================================================
-- CRoute — migrasi semantik "moda terpilih" (opsi C)
--
-- Database hosted (Supabase) memakai role aplikasi `croute` yang bukan owner
-- function/view lama. Karena itu script ini sengaja TIDAK melakukan DROP atau
-- CREATE OR REPLACE pada objek milik `postgres`. Yang dibuat:
--
--   1. Overload fn_save_trip(... , p_chosen_mode_id UUID) milik role aplikasi.
--      Overload 7 argumen yang lama tetap ada untuk kompatibilitas.
--   2. View baru v_trip_chosen_result (tidak dipakai aplikasi, disiapkan untuk
--      query SQL di masa depan).
--
-- Perubahan view v_trip_summary dan function agregasi ada di schema.sql /
-- functions.sql untuk instalasi baru atau dijalankan oleh owner database.
-- Aplikasi WPF menghitung Dashboard/Statistik di C# dari chosen_mode_id,
-- dengan fallback ke opsi terbaik untuk data lama.
--
-- Aman dijalankan berulang.
-- =============================================================================

BEGIN;

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

COMMIT;
