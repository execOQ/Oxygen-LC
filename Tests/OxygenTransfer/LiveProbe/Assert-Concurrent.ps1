param(
    [Parameter(Mandatory)][string]$RunRoot,
    [ValidateSet('parallel','refill','boost','inputs','death')][string]$Phase='parallel'
)
$ErrorActionPreference='Stop'
$run=[IO.Path]::GetFullPath($RunRoot)
$roles=@('host','client','client2')
$results=[Collections.Generic.List[object]]::new()
function State([string]$role){
    for($attempt=0;$attempt -lt 20;$attempt++){
        try{$s=Get-Content (Join-Path $run "$role/transfer-state.json") -Raw | ConvertFrom-Json;if($null -ne $s){return $s}}catch{}
        Start-Sleep -Milliseconds 20
    }
    throw "Incomplete diagnostic snapshot: $role"
}
function Command([string]$role,[string]$text){
    $path=Join-Path $run "$role/transfer-command.txt"
    $deadline=[datetime]::UtcNow.AddSeconds(10)
    while(Test-Path -LiteralPath $path){if([datetime]::UtcNow -gt $deadline){throw "Unconsumed $role command"};Start-Sleep -Milliseconds 50}
    [IO.File]::WriteAllText($path,$text)
}
function Wait-State([string]$role,[scriptblock]$condition){
    $deadline=[datetime]::UtcNow.AddSeconds(40)
    do{$s=State $role;if($s -and (& $condition $s)){return $s};Start-Sleep -Milliseconds 100}while([datetime]::UtcNow -lt $deadline)
    throw "Timed out: $role $($s | ConvertTo-Json -Depth 7 -Compress)"
}
function Check([bool]$pass,[string]$name,$evidence){
    $results.Add([pscustomobject]@{name=$name;pass=$pass;evidence=$evidence})
    $results | ConvertTo-Json -Depth 9 | Set-Content (Join-Path $run "assertions-$Phase.json")
    if(!$pass){throw "FAIL: $name"}; "PASS: $name"
}
function Total($state){[double]($state.tanks | Measure-Object amount -Sum).Sum}
function Amount($state,[string]$role){[double]($state.tanks | Where-Object id -eq (State $role).id).amount}
function Clean([string]$role){Command $role "release`nchat false`nmenu false`nterminal false`npause false`nwall"}
function Many([double]$recipient=0.1,[double]$first=0.8,[double]$second=0.6){
    foreach($role in $roles){Clean $role}
    Command host "target $((State client).id)`npose 1000 10 1000 0 fixed`noxygen $($recipient.ToString([Globalization.CultureInfo]::InvariantCulture))"
    Command client "target 0`npose 999.55 10 998.5 0`noxygen $($first.ToString([Globalization.CultureInfo]::InvariantCulture))"
    Command client2 "target 0`npose 1000.45 10 998.5 0`noxygen $($second.ToString([Globalization.CultureInfo]::InvariantCulture))"
    Wait-State client {param($s) $s.eligible} | Out-Null
    Wait-State client2 {param($s) $s.eligible} | Out-Null
    Start-Sleep -Milliseconds 2000
}
function Pair([string]$donor='client',[string]$recipient='host'){
    foreach($role in $roles){Clean $role}
    $parked=$roles | Where-Object {$_ -ne $donor -and $_ -ne $recipient}
    Command $parked 'pose 1006 10 1006 0 fixed'
    Command $recipient "target $((State $donor).id)`npose 1000 10 1000 0 fixed`noxygen 0.1"
    Command $donor "target $((State $recipient).id)`npose 1000 10 998.5 0`noxygen 0.4"
    Wait-State $donor {param($s) $s.eligible} | Out-Null
    Start-Sleep -Milliseconds 1800
}
foreach($role in $roles){Wait-State $role {param($s) $s.registered -and $s.tanks.Count -eq 3} | Out-Null}
Check $true 'Three native peers have synchronized tanks' ($roles | ForEach-Object {(State $_).id})

switch($Phase){
    'parallel'{
        Many
        $before=State host
        Command client 'hold 4';Command client2 'hold 4'
        $active=Wait-State host {param($s) $s.sessionCount -eq 2}
        Check $true 'Two remote donors transfer simultaneously to the host' $active
        Start-Sleep -Milliseconds 4500
        $after=State host
        Check ([Math]::Abs((Total $after)-(Total $before)) -lt 0.0001) 'Two-donor transfer conserves oxygen' $after.tanks
        Check ((Amount $after client) -lt 0.7 -and (Amount $after client2) -lt 0.5 -and [double]$after.amount -gt 0.3) 'Both donors decrease while recipient increases' $after.tanks
        Check (@($roles | Where-Object {(State $_).gauges.Count -ne 3}).Count -eq 0) 'All three peers have exactly three gauges' $after.gauges
        Many 0.98 0.04 0.04
        Command client 'hold 2';Command client2 'hold 2';Start-Sleep -Milliseconds 2700
        $after=State host
        Check ([Math]::Abs([double]$after.amount-1) -lt 0.00002 -and $after.sessionCount -eq 0) 'Competing donors stop at recipient capacity' $after
        Check ([Math]::Abs((Total $after)-1.06) -lt 0.0001 -and @($after.tanks | Where-Object { $_.amount -lt 0 -or $_.amount -gt 1 }).Count -eq 0) 'Competing allocation neither duplicates nor overdraws oxygen' $after.tanks
        Pair client2 client
        $before=State host
        Command client2 'hold 2';Wait-State host {param($s) $s.sessionCount -eq 1} | Out-Null
        Start-Sleep -Milliseconds 2500
        $after=State host
        Check ((Amount $after client) -gt 0.15 -and (Amount $after client2) -lt 0.35) 'Late remote client can donate to an earlier remote client' $after.tanks
        Check ([Math]::Abs((Total $after)-(Total $before)) -lt 0.0001) 'Remote-to-remote donation conserves oxygen on host' $after.tanks
    }
    'refill'{
        Pair
        foreach($role in $roles){Command $role 'limited'}
        Command host 'reserve 0.2';Start-Sleep -Milliseconds 800
        $before=State host
        Check ([Math]::Abs([double]$before.reserve-0.2) -lt 0.00002) 'Finite charger reserve prepared on host' $before.reserve
        Command client 'hold 4';Wait-State host {param($s) $s.sessionCount -eq 1} | Out-Null
        Command client 'refill';Start-Sleep -Milliseconds 900
        $during=State host
        Check ($during.sessionCount -eq 1 -and [double]$during.reserve -lt 0.00002) 'Native refill grant completes while donation continues' $during
        Start-Sleep -Milliseconds 3800
        $after=State host
        Check ([Math]::Abs((Total $after)+[double]$after.reserve-(Total $before)-[double]$before.reserve) -lt 0.0001) 'Charger plus three player tanks conserve total oxygen' $after
        Check (@($roles | Where-Object {[Math]::Abs([double](State $_).reserve-[double]$after.reserve) -gt 0.00002}).Count -eq 0) 'All peers agree on remaining charger reserve' $after.reserve
    }
    'boost'{
        Pair host client
        Command host 'boostspawn';Start-Sleep -Milliseconds 1200
        $hostId=(State host).id
        foreach($role in $roles){
            Wait-State $role {param($s)
                @($s.boostHolders | Where-Object {$_.holder -eq $hostId -and $_.previousHolder -eq $hostId}).Count -gt 0
            } | Out-Null
        }
        Check $true 'Native pickup replicates OxyBoost holder on all three peers' @($roles|ForEach-Object{(State $_).boostHolders})
        $before=State host
        Command host "boostuse 3`nhold 3.5"
        $active=Wait-State host {param($s) $s.boostGas -and $s.sessionCount -eq 1}
        Check $true 'Production OxyBoost emits while its owner donates' $active
        Start-Sleep -Milliseconds 4000
        $after=State host
        $added=[double]$after.boostAdded-[double]$before.boostAdded
        Check ($added -gt 0.02 -and [double]$after.boostFuel -lt [double]$before.boostFuel) 'OxyBoost adds oxygen and consumes real item fuel' $after
        Check ([Math]::Abs((Total $after)-(Total $before)-$added) -lt 0.0003) 'Only measured OxyBoost output increases total tank oxygen' @{added=$added;before=$before.tanks;after=$after.tanks}
        Check ((Amount $after client) -gt 0.2) 'OxyBoost use does not interrupt recipient oxygen gain' $after.tanks
    }
    'inputs'{
        Pair client2 host
        $before=State host
        Command client2 'gamepad 2'
        $active=Wait-State host {param($s) $s.sessionCount -eq 1}
        Check $true 'Gamepad West binding starts native transfer' $active
        Start-Sleep -Milliseconds 2400
        $after=State host
        Check ($after.sessionCount -eq 0 -and [double]$after.amount -gt [double]$before.amount+0.04) 'Gamepad hold transfers and release stops' $after
        foreach($mode in @('chat','menu')){
            Pair
            Command client 'hold 4';Wait-State host {param($s) $s.sessionCount -eq 1} | Out-Null
            Command client "$mode true";Start-Sleep -Milliseconds 700
            Check ((State host).sessionCount -eq 0) ("Opening $mode stops remote donation") (State host)
            Command client "$mode false"
        }
    }
    'death'{
        Many
        Command client 'hold 6';Command client2 'hold 6'
        Wait-State host {param($s) $s.sessionCount -eq 2} | Out-Null
        Command host 'die';Start-Sleep -Milliseconds 1000
        $after=State host
        Check ($after.dead -and $after.sessionCount -eq 0) 'Recipient death stops both remote donors on the host' $after
        Check ((State client).target -eq [ulong]::MaxValue -and (State client2).target -eq [ulong]::MaxValue) 'Both clients clear their dead recipient' @((State client),(State client2))
    }
}
"Completed $($results.Count) native $Phase checks."
