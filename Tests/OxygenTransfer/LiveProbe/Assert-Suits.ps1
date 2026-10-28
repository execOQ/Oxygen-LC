param([Parameter(Mandatory)][string]$RunRoot)
$ErrorActionPreference='Stop'
$run=[IO.Path]::GetFullPath($RunRoot)
$results=[Collections.Generic.List[object]]::new()
function State([string]$role){
    for($attempt=0;$attempt -lt 20;$attempt++){
        try{$s=Get-Content (Join-Path $run "$role/transfer-state.json") -Raw | ConvertFrom-Json;if($null -ne $s){return $s}}catch{}
        Start-Sleep -Milliseconds 20
    }
    throw "Incomplete diagnostic snapshot: $role"
}
function Check([bool]$pass,[string]$name,$evidence){
    $results.Add([pscustomobject]@{name=$name;pass=$pass;evidence=$evidence})
    $results | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $run 'assertions-suits.json')
    if(!$pass){throw "FAIL: $name"};"PASS: $name"
}
$deadline=[datetime]::UtcNow.AddSeconds(40)
do{
    $h=State host;$c=State client
    if($h.registered -and $c.registered -and $h.tanks.Count -eq 2 -and $c.tanks.Count -eq 2){break}
    Start-Sleep -Milliseconds 100
}while([datetime]::UtcNow -lt $deadline)
Check ($h.plugins -contains 'x753.More_Suits' -and $c.plugins -contains 'x753.More_Suits') 'More Suits 1.5.4 loaded on both native peers' @($h.plugins,$c.plugins)
Check ($h.suits -contains 'Kirby' -and $h.suits -contains 'Skeleton' -and $c.suits -contains 'Kirby' -and $c.suits -contains 'Skeleton') 'Additional suit materials exist on both peers' @($h.suits,$c.suits)
foreach($role in @('host','client')){
    [IO.File]::WriteAllText((Join-Path $run "$role/transfer-command.txt"),"suit 0 Kirby`nsuit 1 Skeleton")
}
Start-Sleep -Milliseconds 1200
$h=State host;$c=State client
Check (@($h.wornSuits | Where-Object { $_.suit -gt 0 }).Count -eq 2 -and @($c.wornSuits | Where-Object { $_.suit -gt 0 }).Count -eq 2) 'Both avatars wear the selected extra suits on both peers' @($h.wornSuits,$c.wornSuits)
Check ($h.gauges.Count -eq 2 -and $c.gauges.Count -eq 2 -and @($h.gauges.renderers | Where-Object {!$_.enabled}).Count -eq 0 -and @($c.gauges.renderers | Where-Object {!$_.enabled}).Count -eq 0) 'Changing suits preserves both visible oxygen gauges' @($h.gauges,$c.gauges)
"Completed $($results.Count) native suit compatibility checks."
