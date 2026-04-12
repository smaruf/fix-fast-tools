param(
  [Parameter(Mandatory=$true)][string]$cfgPath,
  [int]$timeoutSec = 5,
  [int]$startSeq = 1,
  [int]$maxAttempts = 10,
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

function Build-FixLogon([string]$sender,[string]$target,[string]$user,[string]$pass,[int]$seq,[string]$hb,[switch]$resetY) {
  $ts = (Get-Date).ToUniversalTime().ToString('yyyyMMdd-HH:mm:ss.fff')

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

  if($resetY) { $body += '141=Y' + $soh }

  $head = '8=FIXT.1.1' + $soh + '9=' + ([Text.Encoding]::ASCII.GetByteCount($body)) + $soh
  $msgNoCks = $head + $body

  $bytesNoCks = [Text.Encoding]::ASCII.GetBytes($msgNoCks)
  $sum = 0
  foreach($b in $bytesNoCks) { $sum = ($sum + $b) % 256 }
  $ck = $sum.ToString('000')

  return ($msgNoCks + '10=' + $ck + $soh)
}

function Send-And-Read([string]$fixHost,[int]$port,[string]$msg,[int]$timeoutSec) {
  $client = [System.Net.Sockets.TcpClient]::new()
  $client.ReceiveTimeout = $timeoutSec * 1000
  $client.SendTimeout = $timeoutSec * 1000
  $client.Connect($fixHost, $port)

  $stream = $client.GetStream()
  $outBytes = [Text.Encoding]::ASCII.GetBytes($msg)
  $stream.Write($outBytes, 0, $outBytes.Length)
  $stream.Flush()

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
  return ,$ms.ToArray()
}

function Get-TagValue([string]$fix,[string]$tag) {
  # fix uses SOH delimiters; for our printing we often replace with |, but parsing should use SOH or |
  $pattern = "(?:^|[\x01\|])" + [regex]::Escape($tag) + "=(.*?)(?:[\x01\|]|$)"
  $m = [regex]::Match($fix, $pattern)
  if($m.Success) { return $m.Groups[1].Value }
  return $null
}

function Parse-ExpectedSeqFromText([string]$text) {
  # Example: "MsgSeqNum too low, expecting 2 but received 1"
  $m = [regex]::Match($text, 'expecting\s+(\d+)\s+but\s+received\s+(\d+)', 'IgnoreCase')
  if($m.Success) { return [int]$m.Groups[1].Value }
  return $null
}

$cfgPath = (Resolve-Path -LiteralPath $cfgPath).Path

# don't use $host (collides with built-in $Host)
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
  exit 3
}

$portInt = [int]$fixPort

Write-Host ("Config:   $cfgPath")
Write-Host ("Target:   $fixHost`:$portInt")
Write-Host ("Sender:   $sender")
Write-Host ("TargetID: $target")
Write-Host ""

Write-Host "Step 1/2: TCP check..."
$tnc = Test-NetConnection -ComputerName $fixHost -Port $portInt -WarningAction SilentlyContinue
if(-not $tnc.TcpTestSucceeded) { Write-Host "[FAIL] TCP NOT reachable"; exit 20 }
Write-Host "[OK] TCP reachable"
Write-Host ""

$seq = $startSeq
for($attempt=1; $attempt -le $maxAttempts; $attempt++) {
  Write-Host ("Attempt {0}/{1}: Logon with 34={2}" -f $attempt, $maxAttempts, $seq)

  $msg = Build-FixLogon -sender $sender -target $target -user $user -pass $pass -seq $seq -hb $hb -resetY:$ResetSeqNumFlagY
  Write-Host '--- Sending Logon (SOH shown as |) ---'
  Write-Host ($msg -replace [char]1,'|')
  Write-Host '-------------------------------------'

  $respBytes = Send-And-Read -fixHost $fixHost -port $portInt -msg $msg -timeoutSec $timeoutSec
  if($respBytes.Length -eq 0) {
    Write-Host "[RESULT] NO REPLY BYTES RECEIVED"
    exit 10
  }

  $resp = [Text.Encoding]::ASCII.GetString($respBytes)
  Write-Host ("[RESULT] RECEIVED {0} bytes:" -f $respBytes.Length)
  Write-Host ($resp -replace [char]1,'|')

  $msgType = Get-TagValue $resp '35'
  if($msgType -eq 'A') {
    Write-Host "[OK] LOGON SUCCESS (35=A)"
    exit 0
  }

  if($msgType -eq '5') {
    $text = Get-TagValue $resp '58'
    if($text) {
      $expected = Parse-ExpectedSeqFromText $text
      if($expected) {
        Write-Host ("[INFO] Server expects next MsgSeqNum={0}. Retrying..." -f $expected)
        $seq = $expected
        continue
      }
    }
    Write-Host "[FAIL] LOGOUT received (35=5) not due to seq-too-low (or could not parse expected seq)."
    exit 11
  }

  if($msgType -eq '3') {
    Write-Host "[FAIL] REJECT received (35=3)."
    exit 12
  }

  Write-Host ("[FAIL] Unexpected MsgType 35={0}" -f $msgType)
  exit 13
}

Write-Host "[FAIL] Max attempts reached without logon."
exit 14