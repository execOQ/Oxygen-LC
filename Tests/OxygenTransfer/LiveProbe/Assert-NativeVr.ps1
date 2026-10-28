param([Parameter(Mandatory)][string]$RunRoot)
$ErrorActionPreference='Stop'
$run=[IO.Path]::GetFullPath($RunRoot)
$results=[Collections.Generic.List[object]]::new()
function State($role){
    for($i=0;$i -lt 30;$i++){
        try{$s=Get-Content "$run/$role/transfer-state.json" -Raw | ConvertFrom-Json;if($s){return $s}}catch{}
        Start-Sleep -Milliseconds 30
    }
    throw "Incomplete snapshot: $role"
}
function Command($role,$text){
    $path="$run/$role/transfer-command.txt";$end=[datetime]::UtcNow.AddSeconds(10)
    while(Test-Path $path){if([datetime]::UtcNow -gt $end){throw "Unconsumed command: $role"};Start-Sleep -Milliseconds 50}
    [IO.File]::WriteAllText($path,$text)
}
function Wait-State($role,[scriptblock]$predicate){
    $end=[datetime]::UtcNow.AddSeconds(40)
    do{$s=State $role;if(&$predicate $s){return $s};Start-Sleep -Milliseconds 100}while([datetime]::UtcNow -lt $end)
    throw "Timeout $role $($s | ConvertTo-Json -Depth 5 -Compress)"
}
function Check($pass,$name,$evidence){
    $results.Add([pscustomobject]@{name=$name;pass=[bool]$pass;evidence=$evidence})
    $results | ConvertTo-Json -Depth 9 | Set-Content "$run/assertions-native-vr.json"
    if(!$pass){throw "FAIL: $name"};"PASS: $name"
}
function Vector($value){@($value.Trim('(',')').Split(',') | ForEach-Object {[double]::Parse($_,[Globalization.CultureInfo]::InvariantCulture)})}
function N([double]$value){$value.ToString('R',[Globalization.CultureInfo]::InvariantCulture)}
function StageHost([double]$direction,[bool]$aim){
    $s=State client;$p=Vector $s.localPosition;$f=Vector $s.localForward
    $yaw=[Math]::Atan2($f[0],$f[2])*180/[Math]::PI
    $fixed=if($aim){''}else{' fixed'}
    Command host ("release`npose $(N ($p[0]+$direction*1.15*$f[0])) $(N $p[1]) $(N ($p[2]+$direction*1.15*$f[2])) $(N $yaw)$fixed")
    Start-Sleep -Milliseconds 1700
}
$client=Wait-State client {param($s) $s.registered -and $s.tanks.Count -eq 2 -and $s.vrActiveNative -and $s.vrReadyNative -and $s.xrDisplayRunning}
Check ($client.stereoCamera -and !$client.xrMode) 'Real LCVR gameplay session runs without emulated VR activation' $client
Check ((State host).oxygenTickSeconds -eq 5 -and $client.oxygenTickSeconds -eq 5) 'Both peers initially use the normal five-second oxygen interval' $client.oxygenTickSeconds
try{
    Command host 'tickseconds 10000';foreach($r in @('host','client')){Wait-State $r {param($s) $s.oxygenTickSeconds -eq 10000} | Out-Null}
    StageHost 1 $false
    Command host 'oxygen 0.3';Command client "release`noxygen 0.8"
    Command client "vrinput`nvrpoint"
    $tip=Wait-State client {param($s) $s.eligible -and $s.prompt -match 'Transfer oxygen'}
    Check ($tip.vrControllerHover -and $tip.cursorActive -and $tip.cursorEnabled) 'LCVR native hover pass leaves a visible oxygen prompt' @{prompt=$tip.prompt;rayOrigin=$tip.transferAimOrigin;vrHover=$tip.vrControllerHover}
    Command client 'xrhold 3 gripButton'
    $during=Wait-State host {param($s) $s.sessionCount -eq 1}
    Check $true 'Desktop host accepts the native LCVR hand ray over command v2' $during.tanks
    Start-Sleep -Milliseconds 3600
    $after=State host;$owner=State client
    Check ($after.sessionCount -eq 0 -and $owner.amount -lt 0.72 -and $after.amount -gt 0.38) 'VR donation drains donor HUD, fills host and stops on release' $after.tanks
    Check ([Math]::Abs([double]($after.tanks | Measure-Object amount -Sum).Sum-1.1) -lt 0.0001) 'VR hand-ray transfer conserves oxygen' $after.tanks
    Check ([Math]::Abs([double]$owner.amount-[double]($after.tanks | Where-Object id -eq $owner.id).amount) -lt 0.0001) 'Native VR HUD oxygen matches the authoritative donor tank' $owner.amount
    Command host 'oxygen 1'
    $full=Wait-State client {param($s) $s.prompt -eq 'Oxygen tank is full'}
    Check $full.vrControllerHover 'LCVR displays a full-tank explanation instead of hiding interaction' $full.prompt
    Command client "release`nvrpointoff`nvrinputstop`noxygen 0.3"
    Command host 'oxygen 0.8'
    StageHost -1 $true
    $pcTip=Wait-State host {param($s) $s.eligible -and $s.prompt -match 'Transfer oxygen'}
    Check $true 'Desktop camera can target the visible VR-player gauge' $pcTip.prompt
    Command host 'gamepad 3'
    Wait-State host {param($s) $s.sessionCount -eq 1} | Out-Null
    Start-Sleep -Milliseconds 3600
    $after=State host;$recipient=State client
    Check ($after.sessionCount -eq 0 -and $after.amount -lt 0.72 -and $recipient.amount -gt 0.38) 'Desktop-to-VR donation changes both HUD values and stops on release' $after.tanks
    Check ([Math]::Abs([double]($after.tanks | Measure-Object amount -Sum).Sum-1.1) -lt 0.0001) 'Desktop-to-VR transfer conserves oxygen' $after.tanks
    StageHost 1 $false
    Command host 'oxygen 0.35';Command client 'oxygen 0.8'
}finally{
    Command client "release`nvrpointoff`nvrinputstop"
    Command host "release`nunplug`nfree`ntickseconds 5"
}
foreach($r in @('host','client')){Wait-State $r {param($s) $s.oxygenTickSeconds -eq 5} | Out-Null}
Check $true 'Normal oxygen consumption restored on both peers' (State host).oxygenTickSeconds
"Completed $($results.Count) native LCVR assertions (controller ray/button events are staged; headset activation and native hover are real)."
