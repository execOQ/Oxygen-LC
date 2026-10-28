param(
    [Parameter(Mandatory)][ValidateSet('host','client','client2')][string]$Role,
    [Parameter(Mandatory)][string]$RunRoot,
    [string]$GameDirectory='E:\Games\SteamLibrary\steamapps\common\Lethal Company',
    [switch]$Restart,
    [int]$ProxyPort=0,
    [switch]$MoreSuits,
    [switch]$LcvR,
    [switch]$Visible,
    [ValidateRange(1,10000)][float]$OxygenTickSeconds=5,
    [int]$Width=960,
    [int]$Height=540
)
$ErrorActionPreference='Stop'
$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$workspace=[IO.Path]::GetFullPath((Join-Path $repository '..'))
$run=[IO.Path]::GetFullPath($RunRoot)
if(!$run.StartsWith($workspace+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Run must remain inside the Oxygen workspace'}
$saveRoot='C:\Users\execo\AppData\LocalLow\ZeekerssRBLX\Lethal Company'
$runningGames=@(Get-Process -Name 'Lethal Company' -ErrorAction SilentlyContinue)
if($Role -eq 'host' -and $runningGames.Count -gt 0){throw 'A game is already running; do not launch another host'}
if($Role -ne 'host'){
    $hostRecord=Get-Content -LiteralPath (Join-Path $run 'host-process.json') -Raw | ConvertFrom-Json
    $expected=@($hostRecord)
    if($Role -eq 'client2'){$expected+=Get-Content -LiteralPath (Join-Path $run 'client-process.json') -Raw | ConvertFrom-Json}
    if($runningGames.Count -ne $expected.Count){throw 'Only the recorded test peers may be running'}
    foreach($record in $expected){
        $actual=$runningGames | Where-Object Id -eq $record.id
        if(!$actual -or $actual.StartTime.ToUniversalTime() -ne ([datetime]$record.started).ToUniversalTime()){throw 'Recorded test process identity changed'}
    }
}
if($Role -eq 'host' -and !$Restart){
    if(Get-Process -Name 'Lethal Company' -ErrorAction SilentlyContinue){throw 'Close existing game processes before starting this test'}
    if(Test-Path -LiteralPath $run){throw 'Use a fresh run directory'}
    New-Item -ItemType Directory -Path $run | Out-Null
    $records=@(foreach($file in Get-ChildItem -LiteralPath $saveRoot -File -Recurse | Where-Object {$_.Name -notlike 'Player*.log'}){
        $relative=[IO.Path]::GetRelativePath($saveRoot,$file.FullName)
        $destination=Join-Path (Join-Path $run 'save-before') $relative
        New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $destination
        [pscustomobject]@{path=$relative;sha256=(Get-FileHash -LiteralPath $file.FullName).Hash}
    })
    ConvertTo-Json -InputObject $records | Set-Content -LiteralPath (Join-Path $run 'save-before.json')
} elseif(!(Test-Path -LiteralPath (Join-Path $run 'host-process.json'))){throw 'Start the test host first'}
if($Restart -and !(Test-Path -LiteralPath (Join-Path $run 'save-before.json'))){throw 'Original save backup is required'}
$profile=Join-Path $run $Role
if(!$Restart -and (Test-Path -LiteralPath $profile)){throw 'Profile already exists'}
New-Item -ItemType Directory -Path (Join-Path $profile 'BepInEx') -Force | Out-Null
$source=Join-Path $workspace '.multiplayer-test/host/BepInEx'
if(!$Restart){
foreach($part in @('core','patchers','plugins','config')){
    Copy-Item -LiteralPath (Join-Path $source $part) -Destination (Join-Path $profile 'BepInEx') -Recurse
}
}
Copy-Item -LiteralPath (Join-Path $repository 'bin/Release/netstandard2.1/Oxygen.dll') -Destination (Join-Path $profile 'BepInEx/plugins/Oxygen.dll') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'bin/Release/netstandard2.1/LiveProbe.dll') -Destination (Join-Path $profile 'BepInEx/plugins/LiveProbe.dll') -Force
if($LcvR){
    $vrPackage=Join-Path $workspace '.multiplayer-test/lcvr-runtime-20261025/packages/LCVR/BepInEx'
    foreach($part in @('plugins','patchers')){
        Get-ChildItem -LiteralPath (Join-Path $vrPackage $part) | ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $profile "BepInEx/$part") -Recurse -Force
        }
    }
    $vrConfig="[General]`nDisableVR = false`nAskOnStartup = false`nIntroScreenSeen = true`n`n[Performance]`nCameraResolution = 0.5`n`n[UI]`nDisableArmHUD = false`n"
    [IO.File]::WriteAllText((Join-Path $profile 'BepInEx/config/io.daxcess.lcvr.cfg'),$vrConfig)
    [IO.File]::WriteAllText((Join-Path $profile 'native-vr.txt'),'LCVR real runtime test; do not override VrCompatibility.Active')
    # Let LCVR own its init scene and native menu flow. Startup shortcuts can
    # remove the volume/objects that its init scene expects to find.
    foreach($relative in @('plugins/CTNOriginals-DevelopmentStartup/DevelopmentStartup.dll','plugins/flerouwu-LC_FastStartup/FastStartup.dll','patchers/Hamunii-TypeLoadExceptionFixer/com.github.Hamunii.TypeLoadExceptionFixer.dll')){
        $path=Join-Path $profile "BepInEx/$relative"
        if(Test-Path -LiteralPath $path){Move-Item -LiteralPath $path -Destination ($path+'.disabled') -Force}
    }
}
if($MoreSuits){
    $suitSource=Join-Path $workspace '.multiplayer-test/CompanyDemands/packages/x753-More_Suits-1.5.4/BepInEx/plugins'
    Copy-Item -LiteralPath (Join-Path $suitSource 'MoreSuits.dll') -Destination (Join-Path $profile 'BepInEx/plugins/MoreSuits.dll') -Force
    if(!(Test-Path -LiteralPath (Join-Path $profile 'BepInEx/plugins/moresuits'))){
        Copy-Item -LiteralPath (Join-Path $suitSource 'moresuits') -Destination (Join-Path $profile 'BepInEx/plugins') -Recurse
    }
}
$developmentConfig=Join-Path $profile 'BepInEx/config/DevelopmentStartup.cfg'
$content=[IO.File]::ReadAllText($developmentConfig)
$content=$content.Replace('AutoJoinLAN = false','AutoJoinLAN = true')
[IO.File]::WriteAllText($developmentConfig,$content)
$oxygenConfig=Join-Path $profile 'BepInEx/config/consequential.Oxygen.cfg'
$content=[IO.File]::ReadAllText($oxygenConfig)
$interval=$OxygenTickSeconds.ToString([Globalization.CultureInfo]::InvariantCulture)
$content=[regex]::Replace($content,'(?m)^secTimer = .*$', "secTimer = $interval")
$content=[regex]::Replace($content,'(?m)^autoFillingOnShip = .*$', 'autoFillingOnShip = Off')
[IO.File]::WriteAllText($oxygenConfig,$content)
if($ProxyPort -gt 0){[IO.File]::WriteAllText((Join-Path $profile 'proxy-port.txt'),$ProxyPort.ToString())}
$arguments='--doorstop-enabled true --doorstop-target-assembly "'+(Join-Path $profile 'BepInEx/core/BepInEx.Preloader.dll')+'" -screen-fullscreen 0 -screen-width '+$Width+' -screen-height '+$Height+' -logFile "'+(Join-Path $profile 'Player.log')+'"'
$windowStyle=if($Visible){'Normal'}else{'Hidden'}
$process=Start-Process -FilePath (Join-Path $GameDirectory 'Lethal Company.exe') -WorkingDirectory $GameDirectory -ArgumentList $arguments -WindowStyle $windowStyle -PassThru
$record=[ordered]@{id=$process.Id;started=$process.StartTime.ToUniversalTime().ToString('O');profile=$profile;dllSha256=(Get-FileHash -LiteralPath (Join-Path $profile 'BepInEx/plugins/Oxygen.dll')).Hash}
$record | ConvertTo-Json | Tee-Object -FilePath (Join-Path $run ($Role+'-process.json'))
if($process.WaitForExit(15000)){"Exited during startup with code $($process.ExitCode)"}
else{
    "Still running after startup: $($process.Id). Keeping the launcher alive until the game exits."
    $process.WaitForExit()
}
