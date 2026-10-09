# Kalite kapısı: bir teammate işi "bitti" dediğinde çalışır (TaskCompleted ve SubagentStop).
# Üç kontrol yapar; biri bile başarısızsa exit 2 ile tamamlanmayı engeller ve eksikleri
# stderr üzerinden agent'a geri bildirir:
#   1. dotnet build: 0 warning / 0 error
#   2. dotnet test: tüm testler geçer
#   3. Senaryo kapsamı: required-scenarios.txt içindeki her kimlik (AUTH-xx) tests/ altında
#      en az bir .cs dosyasında geçer (docs/auth-test-senaryolari.md: "testi yoksa iş bitmemiştir")
# Kapsam listesi aşamaya göre takım lideri tarafından güncellenir. Liste boşsa 3. kontrol atlanır.
# Korkuluktur, review'un yerini tutmaz: kimliğin geçmesi testin doğru olduğunu kanıtlamaz.

try {
    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
} catch {
    exit 0
}

# PowerShell 5.1'de native komutun stderr'i (2>&1) hata kaydı sayılır; Stop modunda script
# durur. dotnet çıktısı metin olarak toplanır, karar exit code ve içerikle verilir.
$ErrorActionPreference = 'Continue'

$root = if ($env:CLAUDE_PROJECT_DIR) { $env:CLAUDE_PROJECT_DIR } else { $payload.cwd }
Set-Location $root

$problems = @()

# 1. Build
$build = & dotnet build --nologo -v q 2>&1 | Out-String
if ($LASTEXITCODE -ne 0 -or $build -notmatch '\b0 Warning\(s\)' -or $build -notmatch '\b0 Error\(s\)') {
    $lines = ($build -split "`r?`n") | Where-Object { $_ -match ': (warning|error) ' } | Select-Object -Unique -First 10
    $problems += "BUILD temiz değil (0 warning / 0 error olmalı):`n" + ($lines -join "`n")
}

# 2. Test (build başarısızsa anlamsız)
if ($problems.Count -eq 0) {
    $test = & dotnet test --nologo --no-build -v q 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        $lines = ($test -split "`r?`n") | Where-Object { $_ -match '^\s*Failed\s|Failed!' } | Select-Object -First 15
        $problems += "TEST başarısız:`n" + ($lines -join "`n")
    }
}

# 3. Senaryo kapsamı
$scopeFile = Join-Path $PSScriptRoot 'required-scenarios.txt'
if (Test-Path $scopeFile) {
    $required = Get-Content $scopeFile | ForEach-Object { ($_ -replace '#.*$', '').Trim() } | Where-Object { $_ }
    if ($required.Count -gt 0) {
        $testText = Get-ChildItem -Path (Join-Path $root 'tests') -Recurse -Filter *.cs |
            Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' } |
            Get-Content -Raw | Out-String
        $missing = $required | Where-Object { $testText -notmatch "\b$([regex]::Escape($_))\b" }
        if ($missing.Count -gt 0) {
            $problems += "SENARYO KAPSAMI eksik; şu kimlikler hiçbir testte geçmiyor:`n" + ($missing -join ', ') +
                "`n(Her senaryonun testi olmalı; kimlik test adında veya yorumunda geçer. docs/auth-test-senaryolari.md)"
        }
    }
}

# Her çalışma quality-gate.log'a bir satır yazar: hook'un gerçekten tetiklendiği buradan görülür.
$who = if ($payload.agent_type) { $payload.agent_type } elseif ($payload.teammate_name) { $payload.teammate_name } else { '(lead)' }
$result = if ($problems.Count -eq 0) { 'GECTI' } else { 'ENGELLEDI: ' + (($problems | ForEach-Object { ($_ -split "`n")[0] }) -join ' | ') }
Add-Content -Path (Join-Path $PSScriptRoot 'quality-gate.log') -Encoding UTF8 `
    -Value ('{0:yyyy-MM-dd HH:mm:ss} {1} {2} {3}' -f (Get-Date), $payload.hook_event_name, $who, $result)

if ($problems.Count -eq 0) { exit 0 }

$stderr = New-Object System.IO.StreamWriter([Console]::OpenStandardError(), (New-Object System.Text.UTF8Encoding($false)))
$stderr.Write("Kalite kapısı: iş henüz tamamlanmış sayılmaz. Aşağıdakileri düzelt, sonra tekrar bitir.`n`n" + ($problems -join "`n`n"))
$stderr.Flush()
exit 2
