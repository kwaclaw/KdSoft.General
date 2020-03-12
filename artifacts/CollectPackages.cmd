
@SETLOCAL enableextensions enabledelayedexpansion
@ECHO off

RMDIR /S /Q upload
MKDIR upload

PUSHD ..

FOR /R %%I IN (*.csproj) DO IF EXIST %%~fI (
  XCOPY "%%~dpIbin\release\*.nupkg" "artifacts\upload"
)

REM Add only files to the nuget folder that don't exist there already
REPLACE "artifacts\upload\*.nupkg" "artifacts\nuget" /A

POPD

ENDLOCAL

PAUSE