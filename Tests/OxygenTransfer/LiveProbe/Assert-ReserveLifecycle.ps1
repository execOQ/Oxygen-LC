param([Parameter(Mandatory)][string]$RunRoot,[ValidateSet('all','remaining')][string]$Phase='all')
$ErrorActionPreference='Stop'
$run=[IO.Path]::GetFullPath($RunRoot)
$roles=@('host','client')
$results=[Collections.Generic.List[object]]::new()
if($Phase -eq 'remaining'){
    foreach($row in (Get-Content (Join-Path $run 'assertions-reserve-lifecycle.json') -Raw | ConvertFrom-Json | Where-Object {$_.name -match 'landing|manual'})){$results.Add($row)}
}
function State([string]$role){
    for($attempt=0;$attempt -lt 30;$attempt++){
        try{$s=Get-Content (Join-Path $run "$role/transfer-state.json") -Raw | ConvertFrom-Json;if($null -ne $s){return $s}}catch{}
        Start-Sleep -Milliseconds 30
    }
    throw "Incomplete snapshot: $role"
}
function Command([string]$role,[string]$text){
    $path=Join-Path $run "$role/transfer-command.txt";$end=[datetime]::UtcNow.AddSeconds(10)
    while(Test-Path -LiteralPath $path){if([datetime]::UtcNow -gt $end){throw "Unconsumed $role command"};Start-Sleep -Milliseconds 50}
    [IO.File]::WriteAllText($path,$text)
}
function Wait-State([string]$role,[scriptblock]$condition,[int]$seconds=40){
    $end=[datetime]::UtcNow.AddSeconds($seconds)
    do{$s=State $role;if(& $condition $s){return $s};Start-Sleep -Milliseconds 100}while([datetime]::UtcNow -lt $end)
    throw "Timed out: $role $($s | ConvertTo-Json -Compress -Depth 4)"
}
function Check([bool]$pass,[string]$name,$evidence){
    $results.Add([pscustomobject]@{name=$name;pass=$pass;evidence=$evidence})
    $results | ConvertTo-Json -Depth 12 | Set-Content (Join-Path $run 'assertions-reserve-lifecycle.json')
    if(!$pass){throw "FAIL: $name"};"PASS: $name"
}
function Near($value,$expected){[Math]::Abs([double]$value-[double]$expected) -lt 0.00001}
foreach($role in $roles){Wait-State $role {param($s) $s.registered -and $s.connected -and $s.tanks.Count -eq 2} | Out-Null}
if($Phase -eq 'all'){
foreach($role in $roles){Command $role 'weather Eclipsed';Wait-State $role {param($s) $s.weather -eq 'Eclipsed'} | Out-Null}
Command host 'land Eclipsed'
foreach($role in $roles){
    $s=Wait-State $role {param($s) $s.landed -and !$s.inShipPhase -and !$s.shipLeaving -and $s.weather -eq 'Eclipsed' -and $s.reserveLimited} 120
    Check ((Near $s.reserve 1) -and (Near $s.reserveCapacity 1) -and (Near $s.reserveFraction 1)) "${role}: real Eclipsed landing initializes full shared reserve" $s
}
Command host 'noon'
foreach($role in $roles){Command $role 'oxygen 0.5';Wait-State $role {param($s) (Near $s.amount 0.5)} | Out-Null}
Command host 'refill'
foreach($role in $roles){$s=Wait-State $role {param($s) (Near $s.reserveFraction 0.5)};Check $true "${role}: manual refill updates real arc to half" $s.reserveGaugeDetails}
Command client 'refill'
foreach($role in $roles){
    $s=Wait-State $role {param($s) (Near $s.reserve 0) -and $s.reserveGaugeDetails.empty -and !$s.reserveGaugeDetails.unlimited}
    Check (Near $s.reserveFraction 0) "${role}: remote manual refill displays empty" $s.reserveGaugeDetails
}
}
Command host 'refillmode Off'
foreach($role in $roles){Command $role 'oxygen 0.3';Wait-State $role {param($s) (Near $s.amount 0.3)} | Out-Null}
Command host "reserve 0.5`nrefillmode WhenPlayerOnShip"
foreach($role in $roles){
    $s=Wait-State $role {param($s) (Near $s.reserve 0) -and $s.reserveGaugeDetails.empty -and $s.amount -gt 0.3}
    Check ((Near $s.reserveFraction 0) -and !$s.reserveGaugeDetails.unlimited) "${role}: natural automatic refill updates shared gauge to empty" $s
}
Command host 'refillmode Off'
Check (Near ((State host).amount+(State client).amount) 1.1) 'Automatic filling conserves the half-tank reserve across both players' @((State host).amount,(State client).amount)
Command host 'reserve 0.496'
foreach($role in $roles){Wait-State $role {param($s) (Near $s.reserveFraction 0.496)} | Out-Null}
$initial=State host;$mount=$initial.reserveGaugeDetails
foreach($pose in @('2 1 -3 90 0 0','2.5 1 -3 90 90 0','2 1 -3 302.086 351.514 187.202')){
    $before=(State host).reserveGaugeDetails.chargerRotation
    Command host "chargerplace $pose"
    foreach($role in $roles){
        $s=Wait-State $role {param($s) $s.reserveGaugeDetails.chargerRotation -ne $before}
        $g=$s.reserveGaugeDetails
        Check ($g.localPosition -eq $mount.localPosition -and $g.localRotation -eq $mount.localRotation -and $g.localScale -eq $mount.localScale -and $g.parentIsCharger -and $g.count -eq 1 -and $g.colliders -eq 0 -and (Near $s.reserveFraction 0.496)) "${role}: native placement $pose preserves mount and reading" $g
    }
}
for($cycle=1;$cycle -le 3;$cycle++){
    $oldId=(State host).reserveGaugeDetails.networkId
    Command host 'chargerstore'
    foreach($role in $roles){Wait-State $role {param($s) $null -eq $s.reserveGaugeDetails} | Out-Null}
    Command host 'chargerreturn'
    foreach($role in $roles){
        $s=Wait-State $role {param($s) $null -ne $s.reserveGaugeDetails -and $s.reserveGaugeDetails.networkId -ne $oldId -and $s.reserveGaugeDetails.colliders -eq 0 -and (Near $s.reserveFraction 0.496)}
        $g=$s.reserveGaugeDetails
        Check ($g.localPosition -eq $mount.localPosition -and $g.localRotation -eq $mount.localRotation -and $g.localScale -eq $mount.localScale -and $g.count -eq 1 -and $g.stockShaders -and $g.colliders -eq 0 -and (Near $s.reserveCapacity 1)) "${role}: storage cycle $cycle recreates one gauge without replenishing reserve" $g
    }
}
Command host "showcase`nreserveview true`nfilllight 10"
Start-Sleep -Seconds 2
Command host 'shot reserve-after-storage'
"Completed $($results.Count) native Eclipsed/refill/placement/storage assertions."
