param([Parameter(Mandatory)][string]$RunRoot)
$ErrorActionPreference='Stop'
$run=[IO.Path]::GetFullPath($RunRoot)
$roles=@('host','client','client2')
$results=[Collections.Generic.List[object]]::new()
function State([string]$role){
    for($attempt=0;$attempt -lt 20;$attempt++){
        try{$s=Get-Content (Join-Path $run "$role/transfer-state.json") -Raw | ConvertFrom-Json;if($null -ne $s){return $s}}catch{}
        Start-Sleep -Milliseconds 30
    }
    throw "Incomplete snapshot: $role"
}
function Command([string]$role,[string]$text){
    $path=Join-Path $run "$role/transfer-command.txt"
    $deadline=[datetime]::UtcNow.AddSeconds(10)
    while(Test-Path -LiteralPath $path){if([datetime]::UtcNow -gt $deadline){throw "Unconsumed $role command"};Start-Sleep -Milliseconds 50}
    [IO.File]::WriteAllText($path,$text)
}
function Wait-State([string]$role,[scriptblock]$condition){
    $deadline=[datetime]::UtcNow.AddSeconds(30)
    do{$s=State $role;if(& $condition $s){return $s};Start-Sleep -Milliseconds 100}while([datetime]::UtcNow -lt $deadline)
    throw "Timed out: $role $($s | ConvertTo-Json -Compress -Depth 5)"
}
function Check([string]$name,$evidence){
    $results.Add([pscustomobject]@{name=$name;pass=$true;evidence=$evidence})
    $results | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $run 'assertions-reserve-gauge.json')
    "PASS: $name"
}
# Precondition: start host/client, set limited reserve to 0.25 with capacity 1,
# then start client2. Its initial network snapshot must already be a quarter full.
$late=Wait-State client2 {param($s) $s.connected -and $s.reserveLimited -and $s.reserveCapacity -eq 1 -and $s.reserveFraction -eq 0.25}
Check 'Late client receives consumed reserve, initial capacity and limited mode' $late
foreach($role in $roles){Command $role "limited`noxygen 0.5"}
Command host 'reserve 1'
foreach($role in $roles){Wait-State $role {param($s) $s.reserveFraction -eq 1 -and $s.reserveLimited -and [Math]::Abs($s.amount-0.5) -lt 0.00001} | Out-Null}
Command client 'refill'
Command client2 'refill'
$states=foreach($role in $roles){Wait-State $role {param($s) $s.reserve -lt 0.00001 -and $s.reserveCapacity -eq 1 -and $s.reserveFraction -lt 0.00001 -and $s.reserveLimited}}
Wait-State client {param($s) $s.amount -eq 1} | Out-Null
Wait-State client2 {param($s) $s.amount -eq 1} | Out-Null
Check 'Competing client refills exhaust reserve and all three gauges agree on zero' $states
Command host 'orbit'
$states=foreach($role in $roles){Wait-State $role {param($s) !$s.reserveLimited -and $s.reserve -lt 0.00001}}
Check 'Server orbit mode overrides clients still configured for Eclipsed' $states
