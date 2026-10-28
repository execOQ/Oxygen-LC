param([Parameter(Mandatory)][string]$RunRoot)
$ErrorActionPreference='Stop'
$run=[IO.Path]::GetFullPath($RunRoot)
$results=[Collections.Generic.List[object]]::new()
function State($role){
    for($i=0;$i -lt 30;$i++){
        try{$s=Get-Content "$run/$role/transfer-state.json" -Raw | ConvertFrom-Json;if($s){return $s}}catch{}
        Start-Sleep -Milliseconds 20
    }
    throw "Incomplete state: $role"
}
function Command($role,$text){
    $path="$run/$role/transfer-command.txt";$end=[datetime]::UtcNow.AddSeconds(10)
    while(Test-Path $path){if([datetime]::UtcNow -gt $end){throw "Unconsumed command: $role"};Start-Sleep -Milliseconds 50}
    [IO.File]::WriteAllText($path,$text)
}
function Wait-State($role,[scriptblock]$predicate){
    $end=[datetime]::UtcNow.AddSeconds(30)
    do{$s=State $role;if(&$predicate $s){return $s};Start-Sleep -Milliseconds 100}while([datetime]::UtcNow -lt $end)
    throw "Timeout $role $($s | ConvertTo-Json -Depth 6 -Compress)"
}
function Check($pass,$name,$evidence){
    $results.Add([pscustomobject]@{name=$name;pass=[bool]$pass;evidence=$evidence})
    $results | ConvertTo-Json -Depth 9 | Set-Content "$run/assertions-gamepad.json"
    if(!$pass){throw "FAIL: $name"};"PASS: $name"
}
function Setup($donor){
    $recipient=if($donor -eq 'host'){'client'}else{'host'}
    foreach($r in @('host','client')){Command $r "release`nterminal false`nchat false`nmenu false`nwall"}
    Command $recipient "target $((State $donor).id)`npose 1000 10 1000 0 fixed`noxygen 0.2"
    Command $donor "target $((State $recipient).id)`npose 1000 10 998.5 0`noxygen 0.8"
    Wait-State $donor {param($s) $s.eligible} | Out-Null
    $from=(State $donor).id;$to=(State $recipient).id
    Wait-State host {param($s) @($s.startEligibility | Where-Object {$_.from -eq $from -and $_.to -eq $to -and $_.allowed}).Count -eq 1} | Out-Null
    Start-Sleep -Milliseconds 500
}
foreach($r in @('host','client')){Wait-State $r {param($s) $s.registered -and $s.tanks.Count -eq 2} | Out-Null}
foreach($donor in @('host','client')){
    Setup $donor
    Command $donor 'gamepad 3'
    $active=Wait-State host {param($s) $s.sessionCount -eq 1}
    Check $true "${donor}: virtual Gamepad West starts native interaction" $active.tanks
    $local=Wait-State $donor {param($s) $s.pressed -and $s.amount -lt 0.79}
    Check ($local.prompt -eq 'Transferring oxygen...') "${donor}: controller transfer status has no button label" $local.prompt
    Start-Sleep -Milliseconds 3300
    $after=State host;$owner=State $donor
    Check ($after.sessionCount -eq 0 -and !$owner.pressed -and $owner.amount -lt 0.72) "${donor}: hold drains donor HUD and release stops" $owner
    Check ([Math]::Abs([double]($after.tanks | Measure-Object amount -Sum).Sum-1) -lt 0.0001) "${donor}: transfer conserves oxygen" $after.tanks
    $amount=$owner.amount;Start-Sleep -Milliseconds 600
    Check ([Math]::Abs([double](State $donor).amount-[double]$amount) -lt 0.0001) "${donor}: oxygen remains stable after release" (State $donor).amount
    Setup $donor;Command $donor 'gamepad 8'
    Wait-State host {param($s) $s.sessionCount -eq 1} | Out-Null
    Command $donor 'unplug';Start-Sleep -Milliseconds 700
    Check ((State host).sessionCount -eq 0 -and !(State $donor).pressed) "${donor}: removing a held gamepad stops transfer" (State $donor)
}
foreach($mode in @('menu','chat','terminal')){
    Setup client;Command client "$mode true`ngamepad 2";Start-Sleep -Milliseconds 1000
    Check ((State host).sessionCount -eq 0 -and [Math]::Abs([double](State client).amount-0.8) -lt 0.0001) "Gamepad cannot start transfer while $mode is open" (State client)
    Setup client;Command client 'gamepad 8';Wait-State host {param($s) $s.sessionCount -eq 1} | Out-Null
    Command client "$mode true";Start-Sleep -Milliseconds 700
    Check ((State host).sessionCount -eq 0) "Opening $mode cancels held gamepad transfer" (State host).tanks
}
Setup client;Command client 'padbind buttonSouth';Start-Sleep -Milliseconds 400
Command client 'gamepad 1 West';Start-Sleep -Milliseconds 1400
Check ((State host).sessionCount -eq 0 -and [Math]::Abs([double](State client).amount-0.8) -lt 0.0001) 'Original West button stops working after rebinding' (State client)
Command client 'gamepad 2 South';Wait-State host {param($s) $s.sessionCount -eq 1} | Out-Null
$s=State client
Check ($s.prompt -eq 'Transferring oxygen...') 'Rebound South button starts transfer with no button label' $s.prompt
Start-Sleep -Milliseconds 2500
Check ((State host).sessionCount -eq 0 -and (State client).amount -lt 0.75) 'Rebound gamepad release stops transfer' (State client)
Command client 'padbind buttonWest';Setup client;Command client 'hold 2'
Wait-State host {param($s) $s.sessionCount -eq 1} | Out-Null
Start-Sleep -Milliseconds 2500
Check ((State host).sessionCount -eq 0 -and (State client).amount -lt 0.75) 'Keyboard interaction works after switching from gamepad' (State client)
"Completed $($results.Count) gamepad assertions."
