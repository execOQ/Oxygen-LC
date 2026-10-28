param([Parameter(Mandatory)][string]$RunRoot)
$ErrorActionPreference='Stop'
$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$workspace=[IO.Path]::GetFullPath((Join-Path $repository '..'))
$run=[IO.Path]::GetFullPath($RunRoot)
if(!$run.StartsWith($workspace+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Run must remain inside the Oxygen workspace'}
foreach($role in @('client2','client','host')){
    $recordPath=Join-Path $run ($role+'-process.json')
    if(!(Test-Path -LiteralPath $recordPath)){continue}
    $record=Get-Content -LiteralPath $recordPath -Raw | ConvertFrom-Json
    $process=Get-Process -Id $record.id -ErrorAction SilentlyContinue
    if($null -eq $process){continue}
    if($process.ProcessName -ne 'Lethal Company' -or $process.StartTime.ToUniversalTime() -ne ([datetime]$record.started).ToUniversalTime()){throw 'Process identity changed; refusing to stop it'}
    Set-Content -LiteralPath (Join-Path $record.profile 'transfer-command.txt') -Value 'quit'
    if(!$process.WaitForExit(10000)){Stop-Process -Id $record.id; $process.WaitForExit(5000) | Out-Null}
}
if(Get-Process -Name 'Lethal Company' -ErrorAction SilentlyContinue){throw 'A game is still running; refusing to restore active saves'}
$saveRoot='C:\Users\execo\AppData\LocalLow\ZeekerssRBLX\Lethal Company'
$prefix=[IO.Path]::GetFullPath($saveRoot)+[IO.Path]::DirectorySeparatorChar
$records=@(Get-Content -LiteralPath (Join-Path $run 'save-before.json') -Raw | ConvertFrom-Json)
foreach($file in Get-ChildItem -LiteralPath $saveRoot -File -Recurse | Where-Object {$_.Name -notlike 'Player*.log'}){
    $relative=[IO.Path]::GetRelativePath($saveRoot,$file.FullName)
    $destination=Join-Path (Join-Path $run 'save-after') $relative
    New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
    if($records.path -contains $relative){Copy-Item -LiteralPath $file.FullName -Destination $destination -Force}
    else{
        $resolved=[IO.Path]::GetFullPath($file.FullName)
        if(!$resolved.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Unexpected save path'}
        Move-Item -LiteralPath $resolved -Destination $destination -Force
    }
}
foreach($record in $records){
    $target=[IO.Path]::GetFullPath((Join-Path $saveRoot $record.path))
    if(!$target.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Unexpected restore path'}
    Copy-Item -LiteralPath (Join-Path (Join-Path $run 'save-before') $record.path) -Destination $target -Force
    if((Get-FileHash -LiteralPath $target).Hash -ne $record.sha256){throw 'Save restoration hash mismatch'}
}
"Restored and verified $($records.Count) original saves; test saves retained in save-after."
