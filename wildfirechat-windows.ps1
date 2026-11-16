param(
    [ValidateSet('Start', 'Verify', 'Communication', 'Backup', 'Stop')]
    [string]$Action = 'Verify'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Project = Split-Path -Parent $MyInvocation.MyCommand.Path
$Container = 'wildfirechat-im'

function Assert-DockerSuccess([string]$Step) {
    if ($LASTEXITCODE -ne 0) { throw "$Step failed (Docker exit code $LASTEXITCODE)" }
}

function Wait-Server {
    for ($i = 0; $i -lt 24; $i++) {
        try {
            $r = Invoke-WebRequest 'http://127.0.0.1:18081/api/version' -UseBasicParsing -TimeoutSec 3
            if ($r.StatusCode -eq 200) { return }
        } catch { }
        Start-Sleep -Seconds 3
    }
    throw 'IM server did not become ready within 72 seconds'
}

function Get-AdminSecret {
    $path = Join-Path $Project 'config\wildfirechat.conf'
    $line = Get-Content -LiteralPath $path -Encoding utf8 |
        Where-Object { $_ -match '^\s*http\.admin\.secret_key\s+\S+' } |
        Select-Object -Last 1
    if (-not $line) { throw 'http.admin.secret_key is missing' }
    return ($line.Trim() -split '\s+')[1]
}

function Invoke-Admin([string]$Path, [object]$Body) {
    $nonce = [guid]::NewGuid().ToString('N')
    $timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds().ToString()
    $material = "$nonce|$(Get-AdminSecret)|$timestamp"
    $sha = [Security.Cryptography.SHA1]::Create()
    try {
        $hash = $sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($material))
        $sign = -join ($hash | ForEach-Object { $_.ToString('x2') })
    } finally { $sha.Dispose() }
    $headers = @{ nonce = $nonce; timestamp = $timestamp; sign = $sign }
    $json = $Body | ConvertTo-Json -Compress -Depth 12
    $r = Invoke-WebRequest -Method Post -Uri ('http://127.0.0.1:28080' + $Path) `
        -Headers $headers -ContentType 'application/json; charset=utf-8' `
        -Body $json -UseBasicParsing -TimeoutSec 20
    if ($r.StatusCode -ne 200) { throw "$Path returned HTTP $($r.StatusCode)" }
    $result = $r.Content | ConvertFrom-Json
    if ($result.code -ne 0) { throw "$Path returned code $($result.code): $($result.msg)" }
    return $result
}

function Backup-H2 {
    $state = docker inspect $Container --format '{{.State.Status}}'
    Assert-DockerSuccess 'Inspect container'
    $wasRunning = $state.Trim() -eq 'running'
    $dest = Join-Path $Project ('h2db_volume_backup_' + (Get-Date -Format 'yyyyMMdd_HHmmss'))
    New-Item -ItemType Directory -Path $dest | Out-Null
    try {
        if ($wasRunning) {
            docker stop --timeout 90 $Container | Out-Null
            Assert-DockerSuccess 'Stop container for consistent backup'
        }
        docker cp "${Container}:/opt/im-server/h2db/." $dest | Out-Null
        Assert-DockerSuccess 'Copy H2 volume'
        $db = Join-Path $dest 'wfchat.mv.db'
        if (-not (Test-Path -LiteralPath $db) -or (Get-Item -LiteralPath $db).Length -eq 0) {
            throw 'H2 backup is missing or empty'
        }
        Write-Output "H2 backup: $dest"
        Write-Output "H2 SHA256: $((Get-FileHash -LiteralPath $db -Algorithm SHA256).Hash)"
    } finally {
        if ($wasRunning) {
            docker start $Container | Out-Null
            Assert-DockerSuccess 'Restart container after backup'
            Wait-Server
        }
    }
}

switch ($Action) {
    'Start' {
        docker start $Container | Out-Null
        Assert-DockerSuccess 'Start container'
        Wait-Server
        'START PASS'
    }
    'Verify' {
        Wait-Server
        $health = Invoke-WebRequest 'http://127.0.0.1:28080/admin/health' -UseBasicParsing -TimeoutSec 10
        if ($health.StatusCode -ne 200 -or ($health.Content | ConvertFrom-Json).code -ne 0) {
            throw 'Admin health failed'
        }
        $users = Invoke-Admin '/admin/user/all' @{ count = 1; offset = 0 }
        if ($null -eq $users.result.userInfoList) { throw 'H2 user read failed' }
        $tcp = [Net.Sockets.TcpClient]::new()
        try { $tcp.Connect('127.0.0.1', 18883) } finally { $tcp.Dispose() }
        'VERIFY PASS: version, admin health, signed H2 read, TCP 18883'
    }
    'Backup' { Backup-H2 }
    'Communication' {
        Wait-Server
        Backup-H2
        $id = 'wfctest_' + (Get-Date -Format 'yyyyMMddHHmmss')
        $sender = $id + '_a'
        $target = $id + '_b'
        foreach ($user in @($sender, $target)) {
            $created = Invoke-Admin '/admin/user/create' @{
                userId = $user; name = $user; displayName = "IM test $user"
            }
            if ($created.result.userId -ne $user) { throw "User creation failed: $user" }
        }
        $marker = "wildfirechat e2e $id"
        $sent = Invoke-Admin '/admin/message/send' @{
            sender = $sender
            conv = @{ type = 0; target = $target; line = 0 }
            payload = @{ type = 1; searchableContent = $marker; persistFlag = 3 }
            isUserMessage = $true
        }
        $read = Invoke-Admin '/admin/message/get_one' @{ messageUid = $sent.result.messageUid }
        if ($read.result.messageId -ne $sent.result.messageUid -or
            $read.result.sender -ne $sender -or
            $read.result.conv.target -ne $target -or
            $read.result.payload.searchableContent -ne $marker) {
            throw 'Message readback differs from sent message'
        }
        "COMMUNICATION PASS: $sender -> $target; messageUid=$($sent.result.messageUid)"
    }
    'Stop' {
        docker stop --timeout 90 $Container | Out-Null
        Assert-DockerSuccess 'Stop container'
        'STOP PASS'
    }
}
