# PreToolUse hook: migration komutlarını yalnızca db-agent'a izin verir.
# stdin'den gelen JSON'da agent_type "db-agent" değilse exit 2 ile engeller.
# Korkuluktur; script içinden dolaylı çağrıları yakalamaz (bkz. docs/database.md).
#
# Komut metninde kalıbın geçmesi yetmez; kalıp bir komut segmentinin BAŞINDA olmalıdır.
# Böylece dosyaya düz metin yazan komutlar (heredoc, echo "..." > dosya) engellenmez.
#   1. Heredoc ve PowerShell here-string gövdeleri silinir (içerik veridir).
#   2. powershell/pwsh/bash/sh/cmd -c "..." sarmalayıcılarının içi ayrıca incelenir.
#   3. Kalan tırnaklı metinler silinir (echo "dotnet ef ..." argümandır, komut değil).
#   4. Komut ; && || | ve satır sonuna göre bölünür; her segmentin başında
#      dotnet ef / dotnet-ef çağrısı aranır.

$ErrorActionPreference = 'Stop'

function Test-MigrationCommand([string]$text, [int]$depth = 0) {
    if ($depth -gt 5) { return $false }

    # 1. Heredoc gövdeleri: << EOF ... EOF, <<- 'EOF' ... EOF
    $text = [regex]::Replace($text, '(?ms)<<-?\s*[''"]?(\w+)[''"]?[^\n]*\n.*?^\s*\1\s*$', '<<HEREDOC')
    # PowerShell here-string: @' ... '@ ve @" ... "@
    $text = [regex]::Replace($text, '(?ms)@''\r?\n.*?^''@', "''")
    $text = [regex]::Replace($text, '(?ms)@"\r?\n.*?^"@', "''")

    # 2. Kabuk sarmalayıcıları: içerik gerçekten çalıştırılır, ayrıca incelenir.
    $wrapper = '(?i)\b(?:powershell|pwsh|bash|sh|cmd)(?:\.exe)?\b[^"''\n;|&]*?(?:-c|-command|/c)\s+(?:"((?:[^"\\]|\\.)*)"|''([^'']*)'')'
    foreach ($m in [regex]::Matches($text, $wrapper)) {
        $inner = if ($m.Groups[1].Success) { $m.Groups[1].Value } else { $m.Groups[2].Value }
        if (Test-MigrationCommand $inner ($depth + 1)) { return $true }
    }
    # Tırnaksız sarmalayıcı: powershell -c dotnet ef ... -> sarmalayıcı öneki atılır.
    $text = [regex]::Replace($text, '(?i)\b(?:powershell|pwsh|bash|sh|cmd)(?:\.exe)?\b[^"''\n;|&]*?(?:-c|-command|/c)\s+(?=[^"''])', '')

    # 3. Tırnaklı metinler veridir.
    $text = [regex]::Replace($text, '"(?:[^"\\]|\\.)*"', '""')
    $text = [regex]::Replace($text, "'[^']*'", "''")

    # 4. Segment başında doğrudan dotnet ef / dotnet-ef çağrısı.
    foreach ($segment in [regex]::Split($text, '\r?\n|;|&&|\|\||\|')) {
        $s = $segment.Trim()
        $s = [regex]::Replace($s, '^(?:&\s*|\(\s*|\{\s*|(?:sudo|exec|env|time)\s+|\w+=\S*\s+)*', '')
        if ($s -match '(?i)^(?:dotnet(?:\.exe)?\s+ef|dotnet-ef(?:\.exe)?)\b.*\b(?:migrations\s+(?:add|remove)|database\s+update)\b') {
            return $true
        }
    }
    return $false
}

try {
    $raw = [Console]::In.ReadToEnd()
    $payload = $raw | ConvertFrom-Json
} catch {
    exit 0
}

$command = [string]$payload.tool_input.command
if ([string]::IsNullOrWhiteSpace($command)) { exit 0 }

if (-not (Test-MigrationCommand $command)) { exit 0 }

if ([string]$payload.agent_type -eq 'db-agent') { exit 0 }

$stderr = New-Object System.IO.StreamWriter([Console]::OpenStandardError(), (New-Object System.Text.UTF8Encoding($false)))
$stderr.Write("Migration üretme yetkisi sadece db-agent'a aittir. Bkz: docs/database.md. Takım liderine bildir.")
$stderr.Flush()
exit 2
