-- Salt okunur kullanıcı: PostgreSQL MCP'si (Claude Code) bununla bağlanır, yazamaz (ADR 0024 §3).
-- Yalnızca boş volume ile ilk başlatmada çalışır; mevcut kurulumda aynı komutlar elle verilir.
CREATE ROLE ajandaai_okuma LOGIN PASSWORD 'ajandaai_okuma';
GRANT CONNECT ON DATABASE ajandaai TO ajandaai_okuma;
\connect ajandaai
GRANT USAGE ON SCHEMA public TO ajandaai_okuma;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO ajandaai_okuma;
ALTER DEFAULT PRIVILEGES FOR ROLE ajandaai IN SCHEMA public GRANT SELECT ON TABLES TO ajandaai_okuma;
-- Gizli veri kapalı: token tabloları ve şifre özeti okunamaz.
REVOKE SELECT ON refresh_tokens, email_verification_tokens, personal_access_tokens FROM ajandaai_okuma;
REVOKE SELECT ON users FROM ajandaai_okuma;
GRANT SELECT (id, email, display_name, time_zone_id, created_at, updated_at, is_active, email_confirmed_at, role) ON users TO ajandaai_okuma;
