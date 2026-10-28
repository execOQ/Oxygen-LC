param([Parameter(Mandatory)][string]$RunRoot,[ValidateSet('basic','edges','network')][string]$Phase='basic')
$ErrorActionPreference='Stop'
$run=[IO.Path]::GetFullPath($RunRoot)
$results=[Collections.Generic.List[object]]::new()
function State([string]$role){
    # The probe rewrites this file at 10 Hz; a sample may catch it mid-write.
    for($attempt=0;$attempt -lt 20;$attempt++){
        try{
            $sample=Get-Content -LiteralPath (Join-Path $run "$role/transfer-state.json") -Raw | ConvertFrom-Json
            if($null -ne $sample){return $sample}
        }catch{}
        Start-Sleep -Milliseconds 20
    }
    return $null
}
function Command([string]$role,[string]$text){
    $path=Join-Path $run "$role/transfer-command.txt"
    $deadline=[datetime]::UtcNow.AddSeconds(10)
    while(Test-Path -LiteralPath $path){if([datetime]::UtcNow -gt $deadline){throw 'Previous command was not consumed'};Start-Sleep -Milliseconds 100}
    [IO.File]::WriteAllText($path,$text)
}
function Wait-State([string]$role,[scriptblock]$condition){
    $deadline=[datetime]::UtcNow.AddSeconds(30)
    do{$state=State $role;if($state -and (& $condition $state)){return $state};Start-Sleep -Milliseconds 100}while([datetime]::UtcNow -lt $deadline)
    throw "Timed out waiting for $role. Last state: $($state | ConvertTo-Json -Depth 7 -Compress)"
}
function Check([bool]$pass,[string]$name,$evidence){
    $results.Add([pscustomobject]@{name=$name;pass=$pass;evidence=$evidence})
    $results | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $run "assertions-$Phase.json")
    if(!$pass){throw "FAIL: $name"}
    "PASS: $name"
}
function Rule([string]$json){
    $path=Join-Path $run 'proxy-17778.json'
    for($attempt=0;$attempt -lt 100;$attempt++){
        try{[IO.File]::WriteAllText($path,$json);return}catch [IO.IOException]{Start-Sleep -Milliseconds 20}
    }
    throw 'Could not update local proxy rule'
}
function Setup([string]$donor='host',[float]$from=0.8,[float]$to=0.2){
    $recipient=if($donor -eq 'host'){'client'}else{'host'}
    Command $recipient "release`nterminal false`nchat false`nmenu false`ncrouch false`nwall`npose 1000 10 1000 0 fixed`noxygen $($to.ToString([Globalization.CultureInfo]::InvariantCulture))"
    Command $donor "release`nterminal false`nchat false`nmenu false`ncrouch false`nwall`npose 1000 10 998.5 0`noxygen $($from.ToString([Globalization.CultureInfo]::InvariantCulture))"
    Start-Sleep -Milliseconds 1200
    Wait-State $donor {param($s) $s.eligible -and $s.tanks.Count -eq 2} | Out-Null
    # A reconnect starts the remote avatar at its ship spawn. Wait for the
    # large diagnostic teleport to settle on BOTH peers before applying loss.
    foreach($role in @('host','client')){
        Wait-State $role {param($s) $s.localPosition -like '(1000.000, 10.000,*' -and $s.remotePosition -like '(1000.000, 10.000,*'} | Out-Null
    }
}

try{
$hostState=Wait-State host {param($s) $s.server -and $s.registered -and $s.tanks.Count -eq 2}
$clientState=Wait-State client {param($s) !$s.server -and $s.registered -and $s.tanks.Count -eq 2}
Check $true 'Host and remote client initialized with two synchronized tanks' @{host=$hostState.id;client=$clientState.id}

if($Phase -eq 'basic'){
    Setup
    $before=State host
    Command host 'hold 3'
    Start-Sleep -Milliseconds 1800
    $during=State host
    Check ($during.sessionCount -eq 1 -and $during.pressed) 'Native held Interact starts server transfer' $during
    Check (@($during.gauges | Where-Object pump).Count -gt 0) 'Pump audio plays during confirmed transfer' $during.gauges
    Start-Sleep -Milliseconds 1700
    $after=State host
    $donated=0.8-[float]$after.amount
    Check ($donated -gt 0.10 -and $donated -lt 0.18) 'Transfer rate is approximately five percent per second' @{donated=$donated;duration=3}
    Check ([Math]::Abs(($after.tanks | Measure-Object amount -Sum).Sum-1) -lt 0.00002) 'Native transfer conserves total oxygen' $after.tanks
    Check ($after.sessionCount -eq 0 -and !$after.pressed) 'Releasing Interact stops transfer' $after
    $remote=State client
    Check ([Math]::Abs([float]$remote.amount-[float]($after.tanks | Where-Object id -eq $remote.id).amount) -lt 0.00002) 'Remote HUD matches server tank' $remote.tanks
    foreach($case in @(
        @{name='Side approach';pose='pose 1001.5 10 1000 270'},
        @{name='Front approach';pose='pose 1000 10 1001.5 180'},
        @{name='Aiming away';pose='pose 1000 10 998.5 180 fixed'}
    )){
        Command host $case.pose;Start-Sleep -Milliseconds 700
        $s=State host
        Check (!$s.eligible -and $s.prompt -notmatch 'Transfer oxygen') ($case.name+' has no transfer prompt') $s
        $initial=[float]$s.amount
        Command host 'hold 0.7';Start-Sleep -Milliseconds 1100
        $s=State host
        Check ($s.sessionCount -eq 0 -and [Math]::Abs([float]$s.amount-$initial) -lt 0.00002) ($case.name+' cannot start transfer') $s.tanks
    }
    Setup
    Command host 'wall 1000 11.3 999.25';Command client 'wall 1000 11.3 999.25'
    Start-Sleep -Milliseconds 700
    $s=State host
    Check (!$s.eligible) 'Solid wall blocks rear targeting' $s
    Command host 'hold 0.7';Start-Sleep -Milliseconds 1100
    $s=State host
    Check ($s.sessionCount -eq 0 -and [Math]::Abs([float]$s.amount-0.8) -lt 0.00002) 'Cannot transfer through a wall' $s.tanks
    Setup
    Command host 'hold 4';Start-Sleep -Milliseconds 1000
    Command client 'pose 1000 10 1000 180 fixed';Start-Sleep -Milliseconds 1100
    $s=State host
    Check ($s.sessionCount -eq 1 -and [float]$s.amount -lt 0.75) 'Recipient turn preserves an active transfer' $s
    Command host 'pose 1000 10 996 0';Start-Sleep -Milliseconds 800
    $s=State host
    Check ($s.sessionCount -eq 0) 'Moving outside range stops transfer while input remains held' $s
    Setup
    Command host 'shot backpack-rear'
    Start-Sleep -Milliseconds 500
    Check (Test-Path -LiteralPath (Join-Path $run 'host/backpack-rear.png')) 'Captured native backpack gauge screenshot' 'host/backpack-rear.png'
}elseif($Phase -eq 'edges'){
    Setup host 0.01 0.3
    Command host 'hold 1.5';Start-Sleep -Milliseconds 1900
    $s=State host
    Check ([float]$s.amount -lt 0.00001 -and $s.sessionCount -eq 0 -and [Math]::Abs(($s.tanks | Measure-Object amount -Sum).Sum-0.31) -lt 0.00002) 'Empty donor stops with no negative oxygen' $s
    Setup host 0.5 0.99
    Command host 'hold 1.5';Start-Sleep -Milliseconds 1900
    $s=State host
    Check ([Math]::Abs([float]$s.amount-0.49) -lt 0.00002 -and $s.sessionCount -eq 0) 'Recipient full stops without overflow' $s
    Setup client
    Command client 'hold 4';Start-Sleep -Milliseconds 1500
    $s=State host
    Check ($s.sessionCount -eq 1 -and [float]$s.amount -gt 0.23) 'Remote client can donate to the host' $s
    Command client 'terminal true';Start-Sleep -Milliseconds 800
    $s=State host
    Check ($s.sessionCount -eq 0) 'Opening terminal stops remote donation' $s
    Setup client
    Command client 'hold 4';Start-Sleep -Milliseconds 1200
    Command client 'pause true';Start-Sleep -Milliseconds 900
    $s=State host
    Check ($s.sessionCount -eq 0) 'Missing donor heartbeats expire on server' $s
    Command client "release`npause false"
}else{
    if((State client).id -gt 1){Check $true 'Late reconnect creates a fresh synchronized client tank' (State client).tanks}
    Rule '{"delayMs":0,"jitterMs":0,"dropPercent":0}'
    Start-Sleep -Milliseconds 700
    Setup client
    Rule '{"delayMs":100,"jitterMs":25,"dropPercent":5}'
    Command client 'hold 6';Start-Sleep -Milliseconds 3400
    $s=State host
    Check ([float]$s.amount -gt 0.25 -and $s.sessionCount -eq 1) 'Remote transfer progresses under 200 ms RTT, jitter and packet loss' $s
    Check ([Math]::Abs(($s.tanks | Measure-Object amount -Sum).Sum-1) -lt 0.00002) 'Impaired transport preserves total oxygen' $s.tanks
    Start-Sleep -Milliseconds 3300
    $s=State host
    Check ($s.sessionCount -eq 0) 'Release reaches server over delayed lossy transport' $s
    Rule '{"delayMs":0,"jitterMs":0,"dropPercent":0}'
    Start-Sleep -Milliseconds 700
    Setup client
    Command client 'hold 8';Start-Sleep -Milliseconds 1200
    Check ((State host).sessionCount -eq 1) 'Remote transfer active before simulated outage' (State host).tanks
    Rule '{"delayMs":0,"jitterMs":0,"dropPercent":100}'
    Start-Sleep -Milliseconds 1200
    $stopped=State host
    Check ($stopped.sessionCount -eq 0) 'Server expires remote transfer during complete packet loss' $stopped
    Command client 'release';Start-Sleep -Milliseconds 1000
    $s=State host
    Check ([Math]::Abs([float]$s.amount-[float]$stopped.amount) -lt 0.00002) 'No further oxygen moves while donor is unreachable' $s.tanks
    Rule '{"delayMs":0,"jitterMs":0,"dropPercent":0}'
    Start-Sleep -Milliseconds 2000
    $s=State host
    Check ($s.sessionCount -eq 0 -and [Math]::Abs(($s.tanks | Measure-Object amount -Sum).Sum-1) -lt 0.00002) 'Connection recovery does not duplicate or resume a released transfer' $s
    Setup client
    Command client 'hold 5';Start-Sleep -Milliseconds 1000
    Command host 'wall 1000 11.3 999.25';Command client 'wall 1000 11.3 999.25'
    Start-Sleep -Milliseconds 800
    Check ((State host).sessionCount -eq 0) 'A new wall interrupts an existing transfer' (State host)
    Setup client
    Command client 'rebind f';Start-Sleep -Milliseconds 500
    $s=State client
    # Unity displays physical F as Cyrillic А with the active Russian layout.
    Check ($s.prompt -match 'Transfer oxygen' -and $s.prompt -notmatch 'Hold E') 'Prompt reflects rebound Interact key and keyboard layout' $s.prompt
    $before=[float](State host).amount
    Command client 'hold 1.2';Start-Sleep -Milliseconds 1600
    Check ([float](State host).amount -gt $before+0.03) 'Rebound native Interact donates oxygen' (State host).tanks
    Setup host
    $clientId=(State client).id
    $standing=(State host).gauges | Where-Object id -eq $clientId
    function Height([string]$position){[float]::Parse(($position.Trim('(',')').Split(',')[1]),[Globalization.CultureInfo]::InvariantCulture)}
    Command client 'crouch true'
    # Wait for the actual replicated animation rather than assuming it has
    # reached the host within 900 ms on a loaded machine.
    $crouchedState=Wait-State host {param($s)
        $g=$s.gauges | Where-Object id -eq $clientId
        $g -and (Height $standing.anchorPosition)-(Height $g.anchorPosition) -gt 0.2
    }
    $crouched=$crouchedState.gauges | Where-Object id -eq $clientId
    Check ((Height $standing.anchorPosition)-(Height $crouched.anchorPosition) -gt 0.2) 'Gauge follows the animated spine while crouching' @{standing=$standing;crouched=$crouched}
    Command host 'shot backpack-crouched'
    $before=[float](State host).amount
    Command host 'hold 1.2';Start-Sleep -Milliseconds 1600
    Check ([float](State host).amount -lt $before-0.03) 'Crouched recipient remains reachable from behind' (State host).tanks
    Command client 'crouch false';Start-Sleep -Milliseconds 500
    Setup client
    Command client 'hold 4';Start-Sleep -Milliseconds 1000
    Command client 'die';Start-Sleep -Milliseconds 1000
    $s=State host
    Check ($s.sessionCount -eq 0 -and (State client).dead) 'Donor death stops the native transfer' $s
}
"Completed $($results.Count) native $Phase checks."
}finally{
    if($Phase -eq 'network'){Rule '{"delayMs":0,"jitterMs":0,"dropPercent":0}'}
}
