param([Parameter(Mandatory)][string]$RunRoot)
$ErrorActionPreference='Stop'
$run=[IO.Path]::GetFullPath($RunRoot)
$results=[Collections.Generic.List[object]]::new()
function State($role){
    for($i=0;$i -lt 30;$i++){try{$s=Get-Content "$run/$role/transfer-state.json" -Raw | ConvertFrom-Json;if($s){return $s}}catch{};Start-Sleep -Milliseconds 20}
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
    throw "Timeout: $role"
}
function Check($pass,$name,$evidence){
    $results.Add([pscustomobject]@{name=$name;pass=[bool]$pass;evidence=$evidence})
    $results | ConvertTo-Json -Depth 8 | Set-Content "$run/assertions-transfer-bindings.json"
    if(!$pass){throw "FAIL: $name"};"PASS: $name"
}
function Setup($donor){
    $recipient=if($donor -eq 'host'){'client'}else{'host'}
    foreach($r in @('host','client')){Command $r "release`nterminal false`nchat false`nmenu false`nwall"}
    Command $recipient "target $((State $donor).id)`npose 1000 10 1000 0 fixed`noxygen 0.2"
    Command $donor "target $((State $recipient).id)`npose 1000 10 998.5 0`noxygen 0.8"
    Wait-State $donor {param($s) $s.eligible -and [Math]::Abs($s.amount-0.8) -lt 0.00001} | Out-Null
    Start-Sleep -Milliseconds 500
}
foreach($r in @('host','client')){Wait-State $r {param($s) $s.registered -and $s.tanks.Count -eq 2 -and $s.transferActionEnabled} | Out-Null}
foreach($donor in @('host','client')){
    Setup $donor
    Command $donor 'rebind r'
    $s=Wait-State $donor {param($s) $s.actionControls -contains '/Keyboard/r'}
    Check ($s.vanillaInteractControls -contains '/Keyboard/e') "${donor}: transfer rebind leaves Interact on E" $s.actionControls
    Command $donor 'rawkey E 1'
    Start-Sleep -Milliseconds 1300
    Check ((State host).sessionCount -eq 0 -and [Math]::Abs((State $donor).amount-0.8) -lt 0.00001) "${donor}: original Interact key no longer transfers" (State $donor).amount
    Command $donor 'rawkey R 1.5'
    Wait-State host {param($s) $s.sessionCount -eq 1} | Out-Null
    Start-Sleep -Milliseconds 1800
    Check ((State host).sessionCount -eq 0 -and (State $donor).amount -lt 0.77) "${donor}: custom key transfers and release stops" (State host).tanks
    Command $donor 'vanillabind f'
    Setup $donor
    Command $donor 'rawkey F 1'
    Start-Sleep -Milliseconds 1300
    Check ((State host).sessionCount -eq 0) "${donor}: rebinding vanilla Interact does not change transfer input" (State $donor).actionControls
    Command $donor 'rawkey R 8'
    Wait-State host {param($s) $s.sessionCount -eq 1} | Out-Null
    Command $donor 'rebind t'
    Wait-State host {param($s) $s.sessionCount -eq 0} | Out-Null
    Check $true "${donor}: rebind while held cancels transfer" (State $donor).actionControls
    Command $donor "release`nvanillabind e`nrebind e"
}
Command client 'rebind r'
foreach($mode in @('menu','chat','terminal')){
    Setup client
    Command client "$mode true`nrawkey R 1"
    Start-Sleep -Milliseconds 1300
    Check ((State host).sessionCount -eq 0) "Custom transfer key respects $mode input guard" (State client).amount
    Command client "$mode false"
}
Command client "release`nrebind e"
"Completed $($results.Count) independent transfer binding assertions."
