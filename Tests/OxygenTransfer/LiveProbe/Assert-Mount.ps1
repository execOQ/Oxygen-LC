param([Parameter(Mandatory)][string]$RunRoot)
$ErrorActionPreference='Stop'
$run=[IO.Path]::GetFullPath($RunRoot)
function Command([string]$role,[string]$text){
    $path=Join-Path $run "$role/transfer-command.txt"
    $deadline=[datetime]::UtcNow.AddSeconds(15)
    while(Test-Path -LiteralPath $path){
        if([datetime]::UtcNow -gt $deadline){throw "Pending command for $role"}
        Start-Sleep -Milliseconds 50
    }
    [IO.File]::WriteAllText($path,$text)
}
function State([string]$role){
    for($attempt=0;$attempt -lt 20;$attempt++){
        try{
            $state=Get-Content -LiteralPath (Join-Path $run "$role/transfer-state.json") -Raw | ConvertFrom-Json
            if($null -ne $state){return $state}
        }catch{}
        Start-Sleep -Milliseconds 20
    }
    throw "Cannot sample $role"
}
$deadline=[datetime]::UtcNow.AddSeconds(90)
do{
    $ready=$true
    foreach($role in @('host','client')){
        $state=State $role
        $ready=$ready -and $state.registered -and $state.tanks.Count -eq 2
    }
    if(!$ready){Start-Sleep -Milliseconds 200}
}while(!$ready -and [datetime]::UtcNow -lt $deadline)
if(!$ready){throw 'Both peers must be registered'}
Command host "release`nterminal false`nchat false`nmenu false`npose 1000 10 998.5 0`ncrouch false"
Command client "release`nterminal false`nchat false`nmenu false`npose 1000 10 1000 0 fixed`ncrouch false"
Start-Sleep -Seconds 2
foreach($role in @('host','client')){Command $role 'mountcheck 12'}
for($step=0;$step -lt 5;$step++){
    $crouch=if($step % 2 -eq 0){'true'}else{'false'}
    $x=(1000+$step*0.25).ToString([Globalization.CultureInfo]::InvariantCulture)
    Command client "pose $x 10 1000 $($step*45) fixed`ncrouch $crouch"
    Command host "pose 1000 10 998.5 $($step*30) fixed`ncrouch $crouch"
    Start-Sleep -Seconds 2
}
Start-Sleep -Seconds 3
foreach($role in @('host','client')){
    $result=Get-Content -LiteralPath (Join-Path $run "$role/mount-check.json") -Raw | ConvertFrom-Json
    if(!$result.pass){throw "FAIL: $role mount: $($result | ConvertTo-Json -Compress)"}
    "PASS: $role - $($result.samples) rendered samples, position error $($result.maxPositionErrorMetres) m, angle error $($result.maxAngleErrorDegrees) degrees"
    Command $role 'crouch false'
}
