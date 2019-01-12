@echo off
setlocal enabledelayedexpansion

set vswhere=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe

for /f "usebackq tokens=*" %%i in (`"%vswhere%" -latest -prerelease -products * -requires Microsoft.Component.MSBuild -property installationPath`) do (
  set InstallDir=%%i
)

set tool="!InstallDir!\MSBuild\Current\Bin\MSBuild.exe"
if not exist !tool! (
  set tool="!InstallDir!\MSBuild\15.0\Bin\MSBuild.exe"
  if not exist !tool! exit /b 2
)

!tool! %*