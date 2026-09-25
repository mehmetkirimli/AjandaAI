# block-migrations.ps1 için regresyon testi.
# Her senaryo sahte bir PreToolUse JSON'u ile hook'a verilir ve exit code karşılaştırılır.
# Kullanım: powershell -ExecutionPolicy Bypass -File .claude/hooks/hook-test.ps1
# Başarısızlık varsa exit 1 döner. (bkz. docs/decisions/0008-migration-tekeli-ve-hook.md)

$hook = Join-Path $PSScriptRoot 'block-migrations.ps1'

# e = beklenen exit code: 2 engellenmeli, 0 geçmeli
$cases = @(
    @{ n = 1;  a = 'activity-agent'; e = 2; c = 'dotnet ef migrations add Test' }
    @{ n = 2;  a = 'activity-agent'; e = 2; c = 'cd c:\proje && dotnet ef database update' }
    @{ n = 3;  a = 'activity-agent'; e = 2; c = 'dotnet-ef migrations remove' }
    @{ n = 4;  a = 'db-agent';       e = 0; c = 'dotnet ef migrations add Test' }
    @{ n = 5;  a = 'activity-agent'; e = 0; c = "cat > docs/test.md << EOF`n# Not`ndotnet ef migrations add X`nEOF" }
    @{ n = 6;  a = 'activity-agent'; e = 0; c = 'echo "dotnet ef database update" > notes.txt' }
    @{ n = 7;  a = 'activity-agent'; e = 0; c = 'dotnet build' }
    @{ n = 8;  a = 'activity-agent'; e = 0; c = 'dotnet test' }
    @{ n = 9;  a = 'activity-agent'; e = 2; c = 'powershell -c "dotnet ef migrations add X"' }
    @{ n = 10; a = 'activity-agent'; e = 2; c = 'dotnet ef database update; echo done' }
    @{ n = 11; a = 'activity-agent'; e = 2; c = 'dotnet ef --project src/AjandaAI.Infrastructure migrations add X' }
    @{ n = 12; a = 'activity-agent'; e = 2; c = "cat > a.md << EOF`ntext`nEOF`ndotnet ef database update" }
    @{ n = 13; a = 'activity-agent'; e = 2; c = "bash -c 'cd src && dotnet ef migrations add X'" }
    @{ n = 14; a = 'activity-agent'; e = 2; c = '& dotnet ef migrations add X' }
    @{ n = 15; a = 'activity-agent'; e = 0; c = "Set-Content notes.txt @'`ndotnet ef migrations add X`n'@" }
    @{ n = 16; a = 'activity-agent'; e = 2; c = 'dotnet ef migrations add X > log.txt' }
    @{ n = 17; a = '';               e = 2; c = 'dotnet ef migrations add X' }
)

$failed = @()
foreach ($t in $cases) {
    $json = @{ agent_type = $t.a; tool_input = @{ command = $t.c } } | ConvertTo-Json -Compress
    $json | powershell -NoProfile -ExecutionPolicy Bypass -File $hook 2>$null | Out-Null
    $code = $LASTEXITCODE

    $status = if ($code -eq $t.e) { 'GECTI' } else { 'KALDI' }
    if ($status -eq 'KALDI') { $failed += $t.n }

    $agent = if ($t.a) { $t.a } else { '(lead)' }
    $cmd = $t.c -replace "`n", ' \n '
    '{0,2}. [{1}] beklenen={2} gercek={3}  {4,-15} {5}' -f $t.n, $status, $t.e, $code, $agent, $cmd
}

''
if ($failed.Count -eq 0) {
    "$($cases.Count)/$($cases.Count) gecti"
    exit 0
}
"$($cases.Count - $failed.Count)/$($cases.Count) gecti. Kalan senaryolar: $($failed -join ', ')"
exit 1
