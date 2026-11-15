-- =============================================================================
-- CRoute — role aplikasi untuk Supabase
--
-- Jalankan di Supabase SQL Editor SETELAH schema.sql dan functions.sql.
-- Ganti GANTI_PASSWORD dengan password role croute.
--
-- Catatan Supabase:
--   - Connection pooler memakai username: croute.<project-ref>
--   - Direct connection memakai username: croute
-- =============================================================================

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'croute') THEN
        CREATE ROLE croute LOGIN;
    END IF;
END;
$$;

ALTER ROLE croute WITH LOGIN PASSWORD 'GANTI_PASSWORD';

GRANT CONNECT ON DATABASE postgres TO croute;
GRANT USAGE   ON SCHEMA public       TO croute;

GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES    IN SCHEMA public TO croute;
GRANT USAGE, SELECT                  ON ALL SEQUENCES IN SCHEMA public TO croute;
GRANT EXECUTE                        ON ALL FUNCTIONS IN SCHEMA public TO croute;

-- Objek baru yang dibuat role postgres di masa depan.
ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO croute;
ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO croute;
ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public
    GRANT EXECUTE ON FUNCTIONS TO croute;
