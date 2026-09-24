# PreToolUse hook: migration komutlarını yalnızca db-agent'a izin verir.
# stdin'den gelen JSON'da agent_type "db-agent" değilse exit 2 ile engeller.
# Korkuluktur; script içinden dolaylı çağrıları yakalamaz (bkz. docs/database.md).

$ErrorActionPreference = 'Stop'

try {
    $raw = [Console]::In.ReadToEnd()
    $payload = $raw | ConvertFrom-Json
} catch {
    exit 0
}

$command = [string]$payload.tool_input.command
if ([string]::IsNullOrWhiteSpace($command)) { exit 0 }

$pattern = '(?i)dotnet(\s+|-)ef\s+(migrations\s+(add|remove)|database\s+update)'
if ($command -notmatch $pattern) { exit 0 }

if ([string]$payload.agent_type -eq 'db-agent') { exit 0 }

$stderr = New-Object System.IO.StreamWriter([Console]::OpenStandardError(), (New-Object System.Text.UTF8Encoding($false)))
$stderr.Write("Migration üretme yetkisi sadece db-agent'a aittir. Bkz: docs/database.md. Takım liderine bildir.")
$stderr.Flush()
exit 2
