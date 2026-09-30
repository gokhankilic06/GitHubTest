@echo off
REM Riba Veli Anket Sistemi - cift tiklayarak baslatin.
title Riba Veli Anket Sistemi
cd /d "%~dp0"

where node >nul 2>nul
if errorlevel 1 (
  echo.
  echo  [HATA] Node.js bulunamadi.
  echo  Acilan sayfadan "LTS" surumunu indirip kurun, sonra bu dosyayi tekrar cift tiklayin.
  echo.
  start "" https://nodejs.org
  pause
  exit /b 1
)

if not exist "node_modules\express" (
  echo.
  echo  Ilk kurulum yapiliyor, internet baglantisi gerekir. Birkac dakika surebilir...
  echo.
  call npm.cmd install
  if errorlevel 1 (
    echo.
    echo  [HATA] Kurulum basarisiz oldu. Yukaridaki hata yazisini kopyalayip gonderin.
    pause
    exit /b 1
  )
)

echo.
echo  Site baslatiliyor. Bu pencere acik kaldigi surece site calisir.
echo  Yonetici paneli: http://localhost:3000/admin   (sifre: admin123)
echo  Veli anketi:     http://localhost:3000
echo.
start "" cmd /c "timeout /t 3 >nul & start http://localhost:3000/admin"
node --disable-warning=ExperimentalWarning server.js
echo.
echo  Site durdu. Yukarida bir hata varsa kopyalayip gonderin.
pause
