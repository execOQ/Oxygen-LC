param([Parameter(Mandatory)][string]$RunRoot,[int]$Cycles=36,[int]$CycleSeconds=5)
$ErrorActionPreference='Stop'
$run=[IO.Path]::GetFullPath($RunRoot)
$roles=@('host','client','client2')
$results=[Collections.Generic.List[object]]::new()
function State([string]$role){
    # The helper rewrites this diagnostic file at 10 Hz. Retry a read that lands
    # between truncation and completion instead of treating an empty file as a tank.
    for($attempt=0;$attempt -lt 20;$attempt++){
        try{$snapshot=Get-Content (Join-Path $run "$role/transfer-state.json") -Raw|ConvertFrom-Json;if($null -ne $snapshot){return $snapshot}}catch{}
        Start-Sleep -Milliseconds 20
    }
    throw "Could not read a complete diagnostic snapshot: $role"
}
function Command([string]$role,[string]$text){
    $file=Join-Path $run "$role/transfer-command.txt"
    $deadline=[datetime]::UtcNow.AddSeconds(10)
    while(Test-Path -LiteralPath $file){if([datetime]::UtcNow -gt $deadline){throw "Unconsumed command: $role"};Start-Sleep -Milliseconds 40}
    [IO.File]::WriteAllText($file,$text)
}
function Wait-State([scriptblock]$condition){
    $deadline=[datetime]::UtcNow.AddSeconds(8)
    do{$s=State host;if($s -and (& $condition $s)){return $s};Start-Sleep -Milliseconds 80}while([datetime]::UtcNow -lt $deadline)
    throw "Timed out waiting for soak state: $($s|ConvertTo-Json -Depth 4 -Compress)"
}
function Check([bool]$pass,[string]$name,$evidence){
    $results.Add([pscustomobject]@{name=$name;pass=$pass;evidence=$evidence})
    $results|ConvertTo-Json -Depth 8|Set-Content (Join-Path $run 'assertions-soak.json')
    if(!$pass){throw "FAIL: $name"};"PASS: $name"
}
function Total($state){[double]($state.tanks|Measure-Object amount -Sum).Sum}
$started=[datetime]::UtcNow
$ids=@{};foreach($role in $roles){$s=State $role;if(!$s.registered -or $s.dead){throw "Not ready: $role"};$ids[$role]=$s.id}
Check (@($ids.Values|Select-Object -Unique).Count -eq 3) 'Three live peers begin repeated-transfer soak' $ids
for($cycle=0;$cycle -lt $Cycles;$cycle++){
    $cycleStart=[datetime]::UtcNow
    foreach($role in $roles){Command $role "release`nchat false`nmenu false`nterminal false`npause false`nwall"}
    $pattern=$cycle%3
    if($pattern -eq 0){
        $amounts=@{host=0.1;client=0.8;client2=0.6};$recipient='host';$donors=@('client','client2')
        Command host "target $($ids.client)`npose 1000 10 1000 0 fixed`noxygen 0.1"
        Command client "target $($ids.host)`npose 999.55 10 998.5 0`noxygen 0.8"
        Command client2 "target $($ids.host)`npose 1000.45 10 998.5 0`noxygen 0.6"
    }elseif($pattern -eq 1){
        $amounts=@{host=0.5;client=0.2;client2=0.8};$recipient='client';$donors=@('client2')
        Command host "pose 1006 10 1000 0 fixed`noxygen 0.5"
        Command client "pose 1000 10 1000 0 fixed`noxygen 0.2"
        Command client2 "target $($ids.client)`npose 1000 10 998.5 0`noxygen 0.8"
    }else{
        $amounts=@{host=0.8;client=0.5;client2=0.2};$recipient='client2';$donors=@('host')
        Command client2 "pose 1000 10 1000 0 fixed`noxygen 0.2"
        Command client "pose 1006 10 1000 0 fixed`noxygen 0.5"
        Command host "target $($ids.client2)`npose 1000 10 998.5 0`noxygen 0.8"
    }
    $before=Wait-State {param($s)
        if($s.sessionCount -ne 0 -or $s.tanks.Count -ne 3){return $false}
        foreach($role in $roles){if([Math]::Abs([double]($s.tanks|Where-Object id -eq $ids[$role]).amount-$amounts[$role]) -gt 0.00002){return $false}}
        return $true
    }
    # Both the donor and server must see a valid start pose. A fixed delay can
    # expire before the game's remote view-angle interpolation has caught up.
    $readyDeadline=[datetime]::UtcNow.AddSeconds(15)
    do{
        $server=State host;$ready=$true
        foreach($role in $donors){
            $donor=State $role
            $serverAllows=@($server.startEligibility | Where-Object {$_.from -eq $ids[$role] -and $_.to -eq $ids[$recipient] -and $_.allowed}).Count -eq 1
            $ready=$ready -and $donor.eligible -and $serverAllows
        }
        if(!$ready){Start-Sleep -Milliseconds 100}
    }while(!$ready -and [datetime]::UtcNow -lt $readyDeadline)
    if(!$ready){throw 'Soak start pose is not eligible on both donor and server'}
    foreach($role in $donors){Command $role 'hold 3'}
    $active=Wait-State {param($s) $s.sessionCount -eq $donors.Count}
    Start-Sleep -Milliseconds 1400
    $during=State host
    foreach($role in $donors){Command $role 'release'}
    Start-Sleep -Milliseconds 700
    $after=State host
    $recipientGain=[double]($after.tanks|Where-Object id -eq $ids[$recipient]).amount-$amounts[$recipient]
    $badBounds=@($after.tanks|Where-Object {$_.amount -lt 0 -or $_.amount -gt 1}).Count
    $peers=@(foreach($role in $roles){State $role})
    $consistent=$true
    foreach($peer in $peers){
        if(!$peer.registered -or $peer.dead -or $peer.gauges.Count -ne 3 -or $peer.tanks.Count -ne 3){$consistent=$false;continue}
        foreach($tank in $after.tanks){if([Math]::Abs([double]($peer.tanks|Where-Object id -eq $tank.id).amount-[double]$tank.amount) -gt 0.0001){$consistent=$false}}
        if(@($peer.gauges|Where-Object pump).Count -gt 0){$consistent=$false}
    }
    $pass=[Math]::Abs((Total $during)-1.5) -lt 0.0001 -and [Math]::Abs((Total $after)-1.5) -lt 0.0001 -and $badBounds -eq 0 -and $after.sessionCount -eq 0 -and $recipientGain -gt 0.04 -and $consistent
    Check $pass ("Cycle $($cycle+1)/${Cycles}: transfer, conservation, release and all-peer agreement") @{pattern=$pattern;recipient=$recipient;gain=$recipientGain;tanks=$after.tanks;gauges=@($peers|ForEach-Object {$_.gauges.Count});elapsed=([datetime]::UtcNow-$started).TotalSeconds}
    $remaining=$CycleSeconds*1000-([datetime]::UtcNow-$cycleStart).TotalMilliseconds
    if($remaining -gt 0){Start-Sleep -Milliseconds ([int]$remaining)}
}
Start-Sleep -Milliseconds 800
$last=State host
Check ($last.sessionCount -eq 0 -and [Math]::Abs((Total $last)-1.5) -lt 0.0001) 'Repeated-transfer soak ends idle without oxygen drift' @{cycles=$Cycles;elapsedSeconds=([datetime]::UtcNow-$started).TotalSeconds;tanks=$last.tanks}
"Completed $($results.Count) soak checks."
