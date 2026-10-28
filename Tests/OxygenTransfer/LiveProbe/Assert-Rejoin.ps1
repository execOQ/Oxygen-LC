param([Parameter(Mandatory)][string]$RunRoot,[switch]$TransferOnly)
$ErrorActionPreference='Stop'
$run=[IO.Path]::GetFullPath($RunRoot)
$results=[Collections.Generic.List[object]]::new()
foreach($prior in @(Get-Content (Join-Path $run 'assertions-rejoin.json') -Raw|ConvertFrom-Json)){
    if(!$TransferOnly -or $prior.pass){$results.Add($prior)}
}
$before=Get-Content (Join-Path $run 'rejoin-before.json') -Raw|ConvertFrom-Json
function State([string]$role){
    for($attempt=0;$attempt -lt 20;$attempt++){
        try{$s=Get-Content (Join-Path $run "$role/transfer-state.json") -Raw|ConvertFrom-Json;if($null -ne $s){return $s}}catch{}
        Start-Sleep -Milliseconds 20
    }
    throw "Incomplete diagnostic snapshot: $role"
}
function Check([bool]$pass,[string]$name,$evidence){
    $results.Add([pscustomobject]@{name=$name;pass=$pass;evidence=$evidence})
    $results|ConvertTo-Json -Depth 8|Set-Content (Join-Path $run 'assertions-rejoin.json')
    if(!$pass){throw "FAIL: $name"};"PASS: $name"
}
if(!$TransferOnly){
$deadline=[datetime]::UtcNow.AddSeconds(40)
do{$h=State host;$c=State client;if($c.registered -and $h.tanks.Count -eq 2 -and $c.tanks.Count -eq 2){break};Start-Sleep -Milliseconds 100}while([datetime]::UtcNow -lt $deadline)
Check ($c.registered -and !$c.dead -and $c.id -ne $before.oldClientId) 'Reconnect registers a fresh living client identity' @{old=$before.oldClientId;new=$c.id}
Check ([Math]::Abs([double]$h.amount-[double]$before.hostAmount) -lt 0.00002) 'Host oxygen survives remote death and reconnect unchanged' @{before=$before.hostAmount;after=$h.amount}
Check ($h.gauges.Count -eq 2 -and $c.gauges.Count -eq 2 -and @($h.tanks|Where-Object id -eq $before.oldClientId).Count -eq 0) 'Both peers recreate current gauges without the disconnected tank' @{host=$h.gauges;client=$c.gauges}
$hostTank=$c.tanks|Where-Object id -eq 0
Check ([Math]::Abs([double]$hostTank.amount-[double]$h.amount) -lt 0.00002) 'Reconnecting client receives the existing host tank amount' $c.tanks
}
[IO.File]::WriteAllText((Join-Path $run 'host/transfer-command.txt'),"release`nterminal false`nchat false`nmenu false`npose 1000 10 1000 0 fixed`noxygen 0.2")
[IO.File]::WriteAllText((Join-Path $run 'client/transfer-command.txt'),"release`nterminal false`nchat false`nmenu false`ntarget 0`npose 1000 10 998.5 0`noxygen 0.8")
$deadline=[datetime]::UtcNow.AddSeconds(30)
do{
    $h=State host;$c=State client
    $ready=$c.eligible -and $h.remotePosition -eq $c.localPosition -and $c.remotePosition -eq $h.localPosition
    if(!$ready){Start-Sleep -Milliseconds 100}
}while(!$ready -and [datetime]::UtcNow -lt $deadline)
if(!$ready){throw 'Rejoin poses/targeting did not settle on both peers'}
Start-Sleep -Milliseconds 500
[IO.File]::WriteAllText((Join-Path $run 'client/transfer-command.txt'),'hold 1.5')
Start-Sleep -Milliseconds 2000
$h=State host;$c=State client
Check ([double]$h.amount -gt 0.25 -and $h.sessionCount -eq 0 -and [Math]::Abs([double]($h.tanks|Measure-Object amount -Sum).Sum-1) -lt 0.00002) 'Fresh remote client can donate, release and conserve oxygen after reconnect' $h.tanks
"Completed $($results.Count) reconnect checks."
