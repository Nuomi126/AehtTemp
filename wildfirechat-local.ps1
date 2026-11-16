param(
    [ValidateSet('Start', 'Verify', 'Duplex', 'Stop', 'ChatA', 'ChatB')]
    [string]$Action = 'Verify'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Project = Split-Path -Parent $MyInvocation.MyCommand.Path
$App = Join-Path (Split-Path -Parent $Project) 'app-server'
$Java = 'D:\JavaProjects\jdk-17\bin\java.exe'
$Python = 'D:\Anaconda\Anaconda3\python.exe'
$Client = Join-Path $Project 'wildfire_client.py'
$Container = 'wildfirechat-im'
$PidFile = Join-Path $Project 'app-server-local.pid'

function App-Listener {
    @(Get-NetTCPConnection -State Listen -LocalPort 8888 -ErrorAction SilentlyContinue)
}

function Assert-LocalBindings {
    $appListeners = @(App-Listener)
    if ($appListeners.Count -ne 1 -or $appListeners[0].LocalAddress -ne '127.0.0.1') {
        throw 'App must listen only on 127.0.0.1:8888'
    }
    $inspection = docker inspect $Container | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $inspection.Count -ne 1) { throw 'Cannot inspect IM container' }
    $ports = $inspection[0].NetworkSettings.Ports
    $mapping = @{ '80/tcp' = '18081'; '18080/tcp' = '28080'; '1883/tcp' = '18883';
                  '8083/tcp' = '18083'; '8084/tcp' = '18084' }
    foreach ($port in $mapping.Keys) {
        $entry = $ports.PSObject.Properties[$port]
        if ($null -eq $entry) { throw "IM port $port has no Docker binding" }
        $bindings = @($entry.Value)
        if ($bindings.Count -ne 1 -or $bindings[0].HostIp -ne '127.0.0.1' -or
            $bindings[0].HostPort -ne $mapping[$port]) {
            throw "IM port $port is not bound to the expected local port"
        }
    }
}

function Assert-AppProcess([int]$ProcessId) {
    $proc = Get-Process -Id $ProcessId -ErrorAction Stop
    if ($proc.Path -ne $Java) { throw "Port 8888 is held by an unexpected process ($ProcessId)" }
}

function Wait-Http([string]$Url, [int]$Seconds) {
    $deadline = (Get-Date).AddSeconds($Seconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $r = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3
            if ($r.StatusCode -eq 200) { return $r }
        } catch { }
        Start-Sleep -Seconds 2
    }
    throw "Timed out waiting for $Url"
}

function Start-Stack {
    if (-not (Test-Path -LiteralPath $Java)) { throw 'JDK 17 is missing' }
    if (-not (Test-Path -LiteralPath (Join-Path $App 'target\app-0.74.jar'))) { throw 'App JAR is missing' }
    $state = docker inspect $Container --format '{{.State.Status}}'
    if ($LASTEXITCODE -ne 0) { throw 'IM container is missing' }
    if ($state.Trim() -ne 'running') {
        docker start $Container | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'IM container failed to start' }
    }
    Wait-Http 'http://127.0.0.1:18081/api/version' 90 | Out-Null

    $listeners = @(App-Listener)
    if ($listeners.Count -eq 0) {
        $service = Get-Service -Name MySQL57 -ErrorAction Stop
        if ($service.Status -ne 'Running') { Start-Service -Name MySQL57 }
        $logDir = Join-Path $App 'logs'
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
        $stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
        $out = Join-Path $logDir "startup-$stamp.stdout.log"
        $err = Join-Path $logDir "startup-$stamp.stderr.log"
        $proc = Start-Process -FilePath $Java -ArgumentList @('-jar', 'target\app-0.74.jar') `
            -WorkingDirectory $App -RedirectStandardOutput $out -RedirectStandardError $err `
            -WindowStyle Hidden -PassThru
        Set-Content -LiteralPath $PidFile -Value $proc.Id -Encoding ascii
        try {
            Wait-Http 'http://127.0.0.1:8888/' 100 | Out-Null
        } catch {
            $proc.Refresh()
            if ($proc.HasExited) { Write-Output "App Java exit code: $($proc.ExitCode)" }
            Write-Output "App stdout: $out"
            Write-Output "App stderr: $err"
            throw
        }
    } else {
        Assert-AppProcess $listeners[0].OwningProcess
        Set-Content -LiteralPath $PidFile -Value $listeners[0].OwningProcess -Encoding ascii
    }
    Assert-LocalBindings
    Write-Output 'START PASS: app http://127.0.0.1:8888, IM 127.0.0.1 only'
}

function Verify-Stack {
    $im = Wait-Http 'http://127.0.0.1:18081/api/version' 10
    $health = Wait-Http 'http://127.0.0.1:28080/admin/health' 10
    $app = Wait-Http 'http://127.0.0.1:8888/' 10
    if (($health.Content | ConvertFrom-Json).code -ne 0 -or $app.Content.Trim() -ne 'Ok') {
        throw 'One of the service health responses was invalid'
    }
    Assert-LocalBindings
    Write-Output 'HTTP AND BINDING PASS'
    & $Python $Client
    if ($LASTEXITCODE -ne 0) { throw "Duplex client test failed with exit code $LASTEXITCODE" }
    Write-Output 'VERIFY PASS: app login, two IM logins, A to B, B to A'
}

function Stop-Stack {
    $listeners = @(App-Listener)
    if ($listeners.Count -gt 0) {
        if ($listeners.Count -ne 1 -or $listeners[0].LocalAddress -ne '127.0.0.1') {
            throw 'Unexpected process on port 8888; refusing to stop it'
        }
        $appPid = [int]$listeners[0].OwningProcess
        Assert-AppProcess $appPid
        Stop-Process -Id $appPid -ErrorAction Stop
        Write-Output "Stopped app-server PID $appPid"
    }
    if (Test-Path -LiteralPath $PidFile) { Remove-Item -LiteralPath $PidFile }
    $state = docker inspect $Container --format '{{.State.Status}}'
    if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect IM container' }
    if ($state.Trim() -eq 'running') {
        docker stop --timeout 90 $Container | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'IM container failed to stop' }
    }
    Write-Output 'STOP PASS: app and IM stopped; MySQL and data remain intact'
}

switch ($Action) {
    'Start' { Start-Stack }
    'Verify' { Verify-Stack }
    'Duplex' {
        & $Python $Client
        if ($LASTEXITCODE -ne 0) { throw "Duplex failed with exit code $LASTEXITCODE" }
    }
    'Stop' { Stop-Stack }
    'ChatA' { & $Python $Client --chat A; exit $LASTEXITCODE }
    'ChatB' { & $Python $Client --chat B; exit $LASTEXITCODE }
}
