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
    $results | ConvertTo-Json -Depth 9 | Set-Content "$run/assertions-vr-input.json"
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
    Command $donor "xrassets`nxrprofile assets/content/inputs/vractions.inputactions`nxrstart"
    Wait-State $donor {param($s) $s.xrMode} | Out-Null
    $profile=Get-Content "$run/$donor/xr-profile.json" -Raw | ConvertFrom-Json
    $bindings=@($profile.maps.bindings | Where-Object action -eq Interact)
    Check ($bindings.Count -eq 1 -and $bindings[0].path -eq '<XRController>{RightHand}/gripButton') "${donor}: genuine LCVR 1.4.6 right-grip binding loaded" $bindings
    Command $donor 'xrhold 3'
    Wait-State host {param($s) $s.sessionCount -eq 1} | Out-Null
    $owner=Wait-State $donor {param($s) $s.xrPressed -and $s.amount -lt 0.79}
    Check ($owner.xrControl -match 'OxygenVirtualXR/gripButton' -and !$owner.pressed) "${donor}: XR grip transfers independently of desktop Interact" $owner
    Check ($owner.prompt -eq 'Transferring oxygen...') "${donor}: VR transfer status has no button label" $owner.prompt
    Start-Sleep -Milliseconds 3300
    $after=State host;$owner=State $donor
    Check ($after.sessionCount -eq 0 -and !$owner.xrPressed -and $owner.amount -lt 0.72) "${donor}: VR hold drains donor HUD and release stops" $owner
    Check ([Math]::Abs([double]($after.tanks | Measure-Object amount -Sum).Sum-1) -lt 0.0001) "${donor}: XR transfer conserves oxygen" $after.tanks
    $recipient=if($donor -eq 'host'){'client'}else{'host'}
    $remote=State $recipient
    Check ([Math]::Abs([double]$remote.amount-[double]($after.tanks | Where-Object id -eq $remote.id).amount) -lt 0.0001) "${donor}: recipient HUD agrees with server after VR transfer" $remote.amount
    foreach($mode in @('menu','chat','terminal')){
        Setup $donor;Command $donor "$mode true`nxrhold 2";Start-Sleep -Milliseconds 1000
        Check ((State host).sessionCount -eq 0 -and [Math]::Abs([double](State $donor).amount-0.8) -lt 0.0001) "${donor}: VR cannot start while $mode is open" (State $donor).amount
        Setup $donor;Command $donor 'xrhold 8';Wait-State host {param($s) $s.sessionCount -eq 1} | Out-Null
        Command $donor "$mode true";Start-Sleep -Milliseconds 700
        Check ((State host).sessionCount -eq 0) "${donor}: $mode cancels held VR transfer" (State host).tanks
    }
    Setup $donor;Command $donor 'pose 1000 10 998.5 180 fixed';Start-Sleep -Milliseconds 500
    Command $donor 'xrhold 8';Start-Sleep -Milliseconds 700
    Check ((State host).sessionCount -eq 0) "${donor}: VR grip cannot start while looking away" (State $donor).eligible
    Command $donor 'pose 1000 10 998.5 0';Wait-State $donor {param($s) $s.eligible} | Out-Null
    Start-Sleep -Milliseconds 500
    Check ((State host).sessionCount -eq 0) "${donor}: aiming back with grip held does not auto-start" (State $donor).amount
    Command $donor "release`nxrhold 8";Wait-State host {param($s) $s.sessionCount -eq 1} | Out-Null
    Command $donor 'pose 1000 10 996 0';Start-Sleep -Milliseconds 800
    Check ((State host).sessionCount -eq 0) "${donor}: moving beyond range cancels VR transfer" (State host).tanks
    Setup $donor;Command $donor 'xrhold 8';Wait-State host {param($s) $s.sessionCount -eq 1} | Out-Null
    Command $donor 'xrunplug';Start-Sleep -Milliseconds 700
    Check ((State host).sessionCount -eq 0 -and !(State $donor).xrPressed) "${donor}: removing held XR controller cancels transfer" (State $donor).amount
    Command $donor 'xrstop';Wait-State $donor {param($s) !$s.xrMode} | Out-Null
    Setup $donor;Command $donor 'hold 2';Wait-State host {param($s) $s.sessionCount -eq 1} | Out-Null
    Start-Sleep -Milliseconds 2500
    Check ((State host).sessionCount -eq 0 -and (State $donor).amount -lt 0.75) "${donor}: desktop input recovers after simulated VR exit" (State $donor).amount
}
"Completed $($results.Count) simulated VR input assertions."
