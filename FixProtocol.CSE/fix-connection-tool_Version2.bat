@echo off
setlocal EnableExtensions EnableDelayedExpansion

REM =========================
REM Usage:
REM   fix-connection-tool.bat path\to\fix.cfg
REM If omitted, defaults to .\fix.cfg
REM =========================

set "CFG=%~1"
if "%CFG%"=="" set "CFG=%CD%\fix.cfg"
if not exist "%CFG%" (
  echo [ERROR] Config file not found: "%CFG%"
  exit /b 2
)

REM ---- parse config ----
call :ReadCfg "%CFG%"

echo.
echo ===== Parsed FIX config =====
echo BeginString      = %BeginString%
echo SenderCompID     = %SenderCompID%
echo TargetCompID     = %TargetCompID%
echo Host             = %SocketConnectHost%
echo Port             = %SocketConnectPort%
echo Username         = %Username%
echo StartTime-EndTime= %StartTime% - %EndTime%
echo ReconnectInterval= %ReconnectInterval% seconds
echo ============================

REM ---- basic validation ----
if "%SocketConnectHost%"=="" (
  echo [ERROR] SocketConnectHost missing in cfg.
  exit /b 3
)
if "%SocketConnectPort%"=="" (
  echo [ERROR] SocketConnectPort missing in cfg.
  exit /b 3
)

REM ---- main menu ----
:MENU
echo.
echo Choose:
echo   1) Check reachability (ping + TCP port)
echo   2) Monitor & retry until port is reachable
echo   3) Quick fixes (flushdns / winsock reset / renew IP)
echo   4) Kill & restart a FIX client process (optional)
echo   5) Show parsed config again
echo   0) Exit
set /p CHOICE="Enter choice: "

if "%CHOICE%"=="1" goto :CHECK
if "%CHOICE%"=="2" goto :MONITOR
if "%CHOICE%"=="3" goto :FIXES
if "%CHOICE%"=="4" goto :KILLRESTART
if "%CHOICE%"=="5" goto :SHOW
if "%CHOICE%"=="0" exit /b 0
echo Invalid choice.
goto :MENU

:SHOW
echo.
echo BeginString      = %BeginString%
echo SenderCompID     = %SenderCompID%
echo TargetCompID     = %TargetCompID%
echo Host             = %SocketConnectHost%
echo Port             = %SocketConnectPort%
echo Username         = %Username%
echo StartTime-EndTime= %StartTime% - %EndTime%
echo ReconnectInterval= %ReconnectInterval% seconds
goto :MENU

:CHECK
call :CheckPing "%SocketConnectHost%"
call :CheckTcp "%SocketConnectHost%" "%SocketConnectPort%"
goto :MENU

:MONITOR
set "RETRY=%ReconnectInterval%"
if "%RETRY%"=="" set "RETRY=2"

echo.
echo Monitoring %SocketConnectHost%:%SocketConnectPort% (retry every %RETRY%s). Press Ctrl+C to stop.
:MON_LOOP
call :CheckTcp "%SocketConnectHost%" "%SocketConnectPort%"
if "%TcpOk%"=="1" (
  echo [OK] TCP reachable. Exiting monitor.
  goto :MENU
)
timeout /t %RETRY% /nobreak >nul
goto :MON_LOOP

:FIXES
echo.
echo Fix options:
echo   1) ipconfig /flushdns
echo   2) netsh winsock reset   (requires reboot)
echo   3) ipconfig /release + /renew  (will disrupt network)
echo   0) Back
set /p F="Enter choice: "

if "%F%"=="1" (
  ipconfig /flushdns
  goto :MENU
)
if "%F%"=="2" (
  netsh winsock reset
  echo [NOTE] Reboot Windows to complete Winsock reset.
  goto :MENU
)
if "%F%"=="3" (
  ipconfig /release
  ipconfig /renew
  goto :MENU
)
if "%F%"=="0" goto :MENU
echo Invalid choice.
goto :FIXES

:KILLRESTART
echo.
echo This is OPTIONAL. It can "kick" a stuck FIX client by killing it and restarting it.
echo Provide:
echo   - Process image name (e.g. MyFixClient.exe)
echo   - Start command (full command line)
echo.
set /p PNAME="Process image name (or blank to cancel): "
if "%PNAME%"=="" goto :MENU

set /p STARTCMD="Start command (e.g. ""C:\path\client.exe"" -c ""%CFG%""): "
if "%STARTCMD%"=="" (
  echo [INFO] No start command entered. Will only kill the process.
)

echo.
echo Killing "%PNAME%" ...
taskkill /f /im "%PNAME%" >nul 2>&1
if errorlevel 1 (
  echo [WARN] Could not kill "%PNAME%" (maybe not running).
) else (
  echo [OK] Killed "%PNAME%".
)

if not "%STARTCMD%"=="" (
  echo Starting: %STARTCMD%
  start "" %STARTCMD%
)
goto :MENU

REM =========================
REM Functions
REM =========================

:ReadCfg
set "BeginString="
set "SenderCompID="
set "TargetCompID="
set "SocketConnectHost="
set "SocketConnectPort="
set "Username="
set "StartTime="
set "EndTime="
set "ReconnectInterval="

set "SECTION="

for /f "usebackq delims=" %%L in ("%~1") do (
  set "LINE=%%L"

  REM trim leading spaces
  for /f "tokens=* delims= " %%A in ("!LINE!") do set "LINE=%%A"

  REM skip empty
  if "!LINE!"=="" goto :CONT

  REM skip comment lines starting with # or ;
  if "!LINE:~0,1!"=="#" goto :CONT
  if "!LINE:~0,1!"==";" goto :CONT

  REM section header
  if "!LINE:~0,1!"=="[" (
    set "SECTION=!LINE!"
    goto :CONT
  )

  REM key=value
  for /f "tokens=1,* delims==" %%K in ("!LINE!") do (
    set "K=%%K"
    set "V=%%L"
    REM V currently full line; re-split properly:
    set "V=%%~L"
  )

  for /f "tokens=1,* delims==" %%K in ("!LINE!") do (
    set "K=%%K"
    set "V=%%L"
  )

  REM Normalize (strip spaces around key and value)
  for /f "tokens=* delims= " %%A in ("!K!") do set "K=%%A"
  for /f "tokens=* delims= " %%A in ("!V!") do set "V=%%A"

  REM Drop inline comments after a space + # (best-effort)
  for /f "delims=#" %%A in ("!V!") do set "V=%%A"
  for /f "tokens=* delims= " %%A in ("!V!") do set "V=%%A"

  REM Only capture the first host/port (ignore Host1/Port1 unless Host missing)
  if /i "!K!"=="BeginString" set "BeginString=!V!"
  if /i "!K!"=="SenderCompID" set "SenderCompID=!V!"
  if /i "!K!"=="TargetCompID" set "TargetCompID=!V!"
  if /i "!K!"=="SocketConnectHost" set "SocketConnectHost=!V!"
  if /i "!K!"=="SocketConnectPort" set "SocketConnectPort=!V!"
  if /i "!K!"=="Username" set "Username=!V!"
  if /i "!K!"=="StartTime" set "StartTime=!V!"
  if /i "!K!"=="EndTime" set "EndTime=!V!"
  if /i "!K!"=="ReconnectInterval" set "ReconnectInterval=!V!"

  :CONT
)

REM fallback to Host1/Port1 if primary missing
if "%SocketConnectHost%"=="" (
  for /f "usebackq tokens=1,* delims==" %%K in (`findstr /i /r "^SocketConnectHost1=" "%~1"`) do set "SocketConnectHost=%%L"
)
if "%SocketConnectPort%"=="" (
  for /f "usebackq tokens=1,* delims==" %%K in (`findstr /i /r "^SocketConnectPort1=" "%~1"`) do set "SocketConnectPort=%%L"
)

REM cleanup possible trailing spaces
for /f "tokens=* delims= " %%A in ("%SocketConnectHost%") do set "SocketConnectHost=%%A"
for /f "tokens=* delims= " %%A in ("%SocketConnectPort%") do set "SocketConnectPort=%%A"
exit /b 0

:CheckPing
set "HOST=%~1"
echo.
echo [PING] %HOST%
ping -n 2 "%HOST%" >nul
if errorlevel 1 (
  echo [FAIL] Ping failed.
) else (
  echo [OK] Ping succeeded.
)
exit /b 0

:CheckTcp
set "HOST=%~1"
set "PORT=%~2"
set "TcpOk=0"
echo.
echo [TCP] %HOST%:%PORT%

REM Requires PowerShell (built-in on modern Windows)
for /f "usebackq delims=" %%R in (`powershell -NoProfile -Command ^
  "$r=Test-NetConnection -ComputerName '%HOST%' -Port %PORT% -WarningAction SilentlyContinue; if($r.TcpTestSucceeded){'OK'} else {'FAIL'}"`) do (
  set "RES=%%R"
)

if /i "%RES%"=="OK" (
  echo [OK] TCP port reachable.
  set "TcpOk=1"
) else (
  echo [FAIL] TCP port NOT reachable.
  set "TcpOk=0"
)
exit /b 0