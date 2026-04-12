param(
  [Parameter(Mandatory=$true)][string]$cfgPath,
  [int]$timeoutSec = 5,
  [switch]$ResetSeqNumFlagY
)

$ErrorActionPreference = 'Stop'
$soh = [char]1

function Get-IniValue([string]$path,[string]$section,[string]$key) {
  $in = $false
  $val = $null
  foreach($raw in Get-Content -LiteralPath $path) {
    $line = $raw.Trim()
    if($line -eq '' -or $line.StartsWith('#') -or $line.StartsWith(';')) { continue }
    if($line -match '^\[(.+)\]$') { $in = ($matches[1].Trim().ToLower() -eq $section.ToLower()); continue }
    if(-not $in) { continue }
    if($line -match ('^' + [regex]::Escape($key) + '\s*=\s*(.*)$')) { $val = $matches[1].Trim() }
  }
  return $val
}

$cfgPath = (Resolve-Path -LiteralPath $cfgPath).Path

# IMPORTANT: don't use $host (collides with built-in $Host)
$fixHost = Get-IniValue $cfgPath 'session' 'SocketConnectHost'
$fixPort = Get-IniValue $cfgPath 'session' 'SocketConnectPort'
if(-not $fixHost) { $fixHost = Get-IniValue $cfgPath 'session' 'SocketConnectHost1' }
if(-not $fixPort) { $fixPort = Get-IniValue $cfgPath 'session' 'SocketConnectPort1' }

$sender  = Get-IniValue $cfgPath 'default' 'SenderCompID'
$target  = Get-IniValue $cfgPath 'session' 'TargetCompID'
$user    = Get-IniValue $cfgPath 'session' 'Username'
$pass    = Get-IniValue $cfgPath 'session' 'Password'
$hb      = Get-IniValue $cfgPath 'default' 'HeartBtInt'
if(-not $hb) { $hb = '30' }

if(-not $fixHost -or -not $fixPort -or -not $sender -or -not $target -or -not $user -or -not $pass) {
  Write-Host '[ERROR] Missing required cfg values. Need: SocketConnectHost/Port, SenderCompID, TargetCompID, Username, Password.'
  Write-Host ("fixHost=$fixHost fixPort=$fixPort sender=$sender target=$target user=$user")
  exit 3
}

$portInt = [int]$fixPort

Write-Host ("Config:  $cfgPath")
Write-Host ("Target:  $fixHost`:$portInt")
Write-Host ("Sender:  $sender")
Write-Host ("TargetID:$target")
Write-Host ""

Write-Host "Step 1/2: TCP check..."
$tnc = Test-NetConnection -ComputerName $fixHost -Port $portInt -WarningAction SilentlyContinue
if(-not $tnc.TcpTestSucceeded) {
  Write-Host "[FAIL] TCP NOT reachable"
  exit 20
}
Write-Host "[OK] TCP reachable"
Write-Host ""

Write-Host "Step 2/2: Send FIX Logon and read reply..."
$ts = (Get-Date).ToUniversalTime().ToString('yyyyMMdd-HH:mm:ss.fff')
$seq = 1

# Build FIXT.1.1 Logon body (minimal common fields)
$body =
  '35=A' + $soh +
  '34=' + $seq + $soh +
  '49=' + $sender + $soh +
  '52=' + $ts + $soh +
  '56=' + $target + $soh +
  '553=' + $user + $soh +
  '554=' + $pass + $soh +
  '98=0' + $soh +
  '108=' + $hb + $soh +
  '1137=9' + $soh

if($ResetSeqNumFlagY) {
  $body += '141=Y' + $soh
}

$head = '8=FIXT.1.1' + $soh + '9=' + ([Text.Encoding]::ASCII.GetByteCount($body)) + $soh
$msgNoCks = $head + $body

# Checksum is sum of all bytes up to (and including) the SOH before tag 10, mod 256
$bytesNoCks = [Text.Encoding]::ASCII.GetBytes($msgNoCks)
$sum = 0
foreach($b in $bytesNoCks) { $sum = ($sum + $b) % 256 }
$ck = $sum.ToString('000')

$msg = $msgNoCks + '10=' + $ck + $soh

Write-Host '--- Sending Logon (SOH shown as |) ---'
Write-Host ($msg -replace [char]1,'|')
Write-Host '-------------------------------------'
Write-Host ""

$client = [System.Net.Sockets.TcpClient]::new()
$client.ReceiveTimeout = $timeoutSec * 1000
$client.SendTimeout    = $timeoutSec * 1000
$client.Connect($fixHost, $portInt)

$stream = $client.GetStream()
$outBytes = [Text.Encoding]::ASCII.GetBytes($msg)
$stream.Write($outBytes, 0, $outBytes.Length)
$stream.Flush()

Write-Host ("Sent {0} bytes. Waiting up to {1}s for reply..." -f $outBytes.Length, $timeoutSec)

$buf = New-Object byte[] 65535
$ms  = New-Object System.IO.MemoryStream
$deadline = (Get-Date).AddSeconds($timeoutSec)

while((Get-Date) -lt $deadline) {
  if($stream.DataAvailable) {
    $n = $stream.Read($buf, 0, $buf.Length)
    if($n -le 0) { break }
    $ms.Write($buf, 0, $n) | Out-Null
    Start-Sleep -Milliseconds 100
  } else {
    Start-Sleep -Milliseconds 50
  }
}

$client.Close()

$respBytes = $ms.ToArray()
if($respBytes.Length -eq 0) {
  Write-Host "[RESULT] NO REPLY BYTES RECEIVED"
  exit 10
}

$resp = [Text.Encoding]::ASCII.GetString($respBytes)
Write-Host ("[RESULT] RECEIVED {0} bytes:" -f $respBytes.Length)
Write-Host ($resp -replace [char]1,'|')

if($resp -match '35=A')      { Write-Host "[HINT] Logon reply (35=A) seen." }
elseif($resp -match '35=5')  { Write-Host "[HINT] Logout (35=5) seen." }
elseif($resp -match '35=3')  { Write-Host "[HINT] Reject (35=3) seen." }

exit 0