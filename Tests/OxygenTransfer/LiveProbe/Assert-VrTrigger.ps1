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
    $end=[datetime]::UtcNow.AddSeconds(40)
    do{$s=State $role;if(&$predicate $s){return $s};Start-Sleep -Milliseconds 100}while([datetime]::UtcNow -lt $end)
    throw "Timeout $role $($s | ConvertTo-Json -Depth 5 -Compress)"
}
function Check($pass,$name,$evidence){
    $results.Add([pscustomobject]@{name=$name;pass=[bool]$pass;evidence=$evidence})
    $results | ConvertTo-Json -Depth 9 | Set-Content "$run/assertions-vr-trigger.json"
    if(!$pass){throw "FAIL: $name"};"PASS: $name"
}
foreach($r in @('host','client')){Wait-State $r {param($s) $s.registered -and $s.tanks.Count -eq 2} | Out-Null}
foreach($donor in @('host','client')){
    $recipient=if($donor -eq 'host'){'client'}else{'host'}
    Command $recipient "release`ntarget $((State $donor).id)`npose 1000 10 1000 0 fixed`noxygen 0.2"
    Command $donor "release`ntarget $((State $recipient).id)`npose 1000 10 998.5 0`noxygen 0.8"
    Wait-State $donor {param($s) $s.eligible -and [Math]::Abs([double]$s.amount-0.8) -lt 0.0001} | Out-Null
    $from=(State $donor).id;$to=(State $recipient).id
    Wait-State host {param($s) @($s.startEligibility | Where-Object {$_.from -eq $from -and $_.to -eq $to -and $_.allowed}).Count -eq 1} | Out-Null
    Command $donor "xrassets`nxrprofile assets/content/inputs/vractions.inputactions`nxrstart"
    Wait-State $donor {param($s) $s.xrMode} | Out-Null
    $asset=Get-Content "$run/$donor/xr-profile.json" -Raw | ConvertFrom-Json
    $bindings=@($asset.maps.bindings | Where-Object action -eq Interact)
    Check ($bindings.Count -eq 1 -and $bindings[0].path -eq '<XRController>{RightHand}/gripButton') "${donor}: LCVR 1.5.0 Interact is right grip" $bindings
    foreach($control in @('triggerButton','trigger')){
        Command $donor "xrhold 2 $control"
        $during=Wait-State $donor {param($s) $s.xrUsePressed -and $s.xrActivatePressed}
        Check (!$during.xrPressed) "${donor}: $control activates Use/ActivateItem without Interact" @{use=$during.xrUsePressed;activateItem=$during.xrActivatePressed;interact=$during.xrPressed}
        Start-Sleep -Milliseconds 2400
        $after=State host;$local=State $donor
        Check ($after.sessionCount -eq 0 -and [Math]::Abs([double]$local.amount-0.8) -lt 0.0001) "${donor}: $control alone does not transfer oxygen" $after.tanks
    }
    Command $donor 'xrhold 3 gripButton'
    Wait-State host {param($s) $s.sessionCount -eq 1} | Out-Null
    $during=Wait-State $donor {param($s) $s.xrPressed -and $s.amount -lt 0.79}
    Check (!$during.xrUsePressed -and !$during.xrActivatePressed) "${donor}: grip transfers independently of item trigger" @{prompt=$during.prompt;amount=$during.amount}
    Start-Sleep -Milliseconds 3300
    $after=State host
    Check ($after.sessionCount -eq 0 -and (State $donor).amount -lt 0.72) "${donor}: grip hold drains donor HUD and release stops transfer" $after.tanks
    Check ([Math]::Abs([double]($after.tanks | Measure-Object amount -Sum).Sum-1) -lt 0.0001) "${donor}: grip transfer conserves oxygen" $after.tanks
    Command $donor 'xrstop';Wait-State $donor {param($s) !$s.xrMode} | Out-Null
}
"Completed $($results.Count) grip/trigger assertions with the LCVR 1.5.0 profile."
