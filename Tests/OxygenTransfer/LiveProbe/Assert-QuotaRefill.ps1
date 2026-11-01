param([Parameter(Mandatory)][string]$RunRoot,[switch]$ExpectBroken)
$ErrorActionPreference='Stop'
$run=[IO.Path]::GetFullPath($RunRoot)
$roles=@('host','client')
$rows=[Collections.Generic.List[object]]::new()
function State($role){
    for($i=0;$i -lt 30;$i++){try{$s=Get-Content "$run/$role/transfer-state.json" -Raw | ConvertFrom-Json;if($s){return $s}}catch{};Start-Sleep -Milliseconds 30}
    throw "Incomplete state: $role"
}
function Command($role,$text){
    $path="$run/$role/transfer-command.txt";$end=[datetime]::UtcNow.AddSeconds(10)
    while(Test-Path $path){if([datetime]::UtcNow -gt $end){throw "Unconsumed command: $role"};Start-Sleep -Milliseconds 50}
    [IO.File]::WriteAllText($path,$text)
}
function Wait-State($role,[scriptblock]$condition,[int]$seconds=30){
    $end=[datetime]::UtcNow.AddSeconds($seconds)
    do{$s=State $role;if(&$condition $s){return $s};Start-Sleep -Milliseconds 100}while([datetime]::UtcNow -lt $end)
    throw "Timed out: $role $($s.chargerInteraction | ConvertTo-Json -Compress -Depth 5)"
}
function Check($pass,$name,$evidence){
    $rows.Add([pscustomobject]@{name=$name;pass=[bool]$pass;evidence=$evidence})
    $rows | ConvertTo-Json -Depth 10 | Set-Content "$run/assertions-quota-refill.json"
    if(!$pass){throw "FAIL: $name"};"PASS: $name"
}
function Refill-Check($stage){
    foreach($r in $roles){
        Command $r "chargerhover true`noxygen 0.2"
        $s=Wait-State $r {param($s) $s.chargerInteraction.hoveringCharger -and $s.prompt -eq 'Fill oxygen'}
        Check $true "${r}: native Fill oxygen prompt $stage" $s.chargerInteraction
        Command $r 'rawkey E 3'
        $s=Wait-State $r {param($s) $s.amount -gt 0.99}
        Check $true "${r}: holding vanilla Interact refills $stage" $s.amount
        Command $r 'release'
    }
}
foreach($r in $roles){Wait-State $r {param($s) $s.registered -and $s.controlled -and $s.tanks.Count -eq 2} | Out-Null}
Refill-Check 'before quota transition'
Command host 'quotasuccess'
foreach($r in $roles){$s=Wait-State $r {param($s) $s.timesFulfilledQuota -ge 1};Check $true "${r}: native successful quota RPC completed" $s.timesFulfilledQuota}
Refill-Check 'after successful quota'
foreach($r in $roles){Command $r "release`nchargerhover false"}
Command host 'firequota'
foreach($r in $roles){Wait-State $r {param($s) $s.firing} | Out-Null}
foreach($r in $roles){
    $s=Wait-State $r {param($s) $s.suckingFurniture -and @($s.chargerInteraction.colliders | Where-Object enabled).Count -eq 0} 40
    Check $true "${r}: native firing disables charger colliders during ejection" $s.chargerInteraction
}
foreach($r in $roles){Wait-State $r {param($s) !$s.firing -and !$s.inputBlocked -and !$s.suckingFurniture} 80 | Out-Null}
if($ExpectBroken){
    foreach($r in $roles){
        Command $r 'chargerhover true';Start-Sleep -Milliseconds 500;$s=State $r
        Check (@($s.chargerInteraction.colliders | Where-Object enabled).Count -eq 0 -and !$s.chargerInteraction.hoveringCharger) "${r}: published 1.8.1 reproduces disabled colliders and missing prompt after firing" $s.chargerInteraction
    }
}else{
    Refill-Check 'after native firing and revival'
}
"Completed $($rows.Count) native quota/refill assertions."
