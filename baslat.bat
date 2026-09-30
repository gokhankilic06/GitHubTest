@echo off
setlocal EnableExtensions
title Veraset Randevu Sistemi
cd /d "%~dp0"

:menu
cls
echo ============================================================
echo      Ankara Defterdarligi Veraset Islemleri Randevu
echo ============================================================
echo.
echo   Hangi surum calistirilsin?
echo.
echo     1 - Node.js    http://localhost:3000
echo     2 - Python     http://localhost:5000
echo     3 - C# .NET    http://localhost:5100
echo     4 - Cikis
echo.
set "SECIM="
set /p "SECIM=  Seciminiz [1-4]: "
if "%SECIM%"=="1" goto sifre
if "%SECIM%"=="2" goto sifre
if "%SECIM%"=="3" goto sifre
if "%SECIM%"=="4" exit /b 0
goto menu

:sifre
echo.
echo   Personel paneli /yonetim icin bir sifre belirleyin.
echo   Kullanici adi: yonetici
echo   Bos birakirsaniz personel paneli kapali olur.
echo.
set "ADMIN_PASSWORD="
set /p "ADMIN_PASSWORD=  Panel sifresi: "
echo.
if "%SECIM%"=="1" goto node
if "%SECIM%"=="2" goto python
if "%SECIM%"=="3" goto csharp
goto menu

rem ------------------------------------------------------------
:node
set "PORT=3000"
where node >nul 2>&1
if errorlevel 1 (
    echo [HATA] Node.js bulunamadi.
    echo Lutfen https://nodejs.org adresinden Node.js 22 LTS veya daha yeni bir surum kurun.
    goto hata
)
node -e "const [a,b]=process.versions.node.split('.').map(Number);process.exit(a>22||(a===22&&b>=13)?0:1)"
if errorlevel 1 (
    echo [HATA] Node.js 22.13 veya daha yeni bir surum gerekli. Kurulu surum:
    node -v
    echo Lutfen https://nodejs.org adresinden guncel LTS surumunu kurun.
    goto hata
)
if not exist "node\node_modules\express\" (
    echo Bagimliliklar yukleniyor, bu islem ilk seferde biraz surebilir...
    pushd node
    call npm install --no-fund --no-audit
    if errorlevel 1 (
        popd
        goto hata
    )
    popd
)
call :tarayici_ac 3
echo Sunucu baslatiliyor: http://localhost:%PORT%
echo Kapatmak icin bu pencerede Ctrl+C tuslarina basin.
echo.
node --no-warnings=ExperimentalWarning node\src\server.js
goto bitti

rem ------------------------------------------------------------
:python
set "PORT=5000"
set "PY="
py -3 --version >nul 2>&1
if not errorlevel 1 set "PY=py -3"
if not defined PY (
    python --version >nul 2>&1
    if not errorlevel 1 set "PY=python"
)
if not defined PY (
    echo [HATA] Python bulunamadi.
    echo Lutfen https://www.python.org adresinden Python 3.10 veya daha yeni bir surum kurun.
    echo Kurulumda "Add python.exe to PATH" secenegini isaretlemeyi unutmayin.
    goto hata
)
%PY% -c "import sys; sys.exit(0 if sys.version_info >= (3, 10) else 1)"
if errorlevel 1 (
    echo [HATA] Python 3.10 veya daha yeni bir surum gerekli. Kurulu surum:
    %PY% --version
    goto hata
)
if not exist "python\.venv\Scripts\python.exe" (
    echo Sanal ortam olusturuluyor...
    %PY% -m venv python\.venv
    if errorlevel 1 goto hata
)
echo Bagimliliklar kontrol ediliyor...
"python\.venv\Scripts\python.exe" -m pip install -q --disable-pip-version-check -r python\requirements.txt
if errorlevel 1 goto hata
call :tarayici_ac 3
echo Sunucu baslatiliyor: http://localhost:%PORT%
echo Kapatmak icin bu pencerede Ctrl+C tuslarina basin.
echo.
"python\.venv\Scripts\python.exe" python\app.py
goto bitti

rem ------------------------------------------------------------
:csharp
set "PORT=5100"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_NOLOGO=1"
where dotnet >nul 2>&1
if errorlevel 1 goto dotnet_yok
dotnet --list-sdks | findstr /r "^[89]\. ^[1-9][0-9]\." >nul
if errorlevel 1 goto dotnet_yok
echo Proje derleniyor, bu islem ilk seferde biraz surebilir...
dotnet build csharp -c Release -v quiet -nologo
if errorlevel 1 goto hata
call :tarayici_ac 4
echo Sunucu baslatiliyor: http://localhost:%PORT%
echo Kapatmak icin bu pencerede Ctrl+C tuslarina basin.
echo.
dotnet run --project csharp -c Release --no-build --no-launch-profile
goto bitti

:dotnet_yok
echo [HATA] .NET 8 SDK veya daha yeni bir surum bulunamadi.
echo Lutfen https://dotnet.microsoft.com/download adresinden .NET 8 SDK kurun.
goto hata

rem ------------------------------------------------------------
rem Sunucunun acilmasi icin verilen saniye kadar bekleyip tarayiciyi acar.
:tarayici_ac
start "" /min cmd /c "timeout /t %~1 /nobreak >nul & start "" http://localhost:%PORT%"
exit /b 0

:hata
echo.
echo Bir hata olustu. Yukaridaki mesaji kontrol edin.
pause
exit /b 1

:bitti
echo.
echo Sunucu durduruldu.
pause
exit /b 0
