@echo off
echo This cleans up the output directories (bin, obj) of all projects in the solution

set doClean=n
set /p doClean=Proceed [y/n] (default n)?:

if not '%doClean%'=='y' exit

:: Current directory is location of this batch file
pushd "%~dp0"

REM set msbuild="C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"

set CustomBeforeMicrosoftCommonTargets=%~dp0CleanUpBinObj.targets

set CustomBeforeMicrosoftCommonCrossTargetingTargets=%~dp0CleanUpBinObj.targets

tools/msbuild.cmd KdSoft.General.sln /t:CleanUpBinObj

popd

pause
