
DEL artifacts\*.nupkg

xcopy KdSoft.Reflection\bin\debug\*.nupkg artifacts
xcopy KdSoft.CodeConfig\bin\debug\*.nupkg artifacts
xcopy KdSoft.Common.VeryPortable\bin\debug\*.nupkg artifacts
xcopy KdSoft.Reflection.Portable\bin\debug\*.nupkg artifacts
xcopy KdSoft.StorageBase\bin\debug\*.nupkg artifacts
xcopy KdSoft.TransientStorage\bin\debug\*.nupkg artifacts
xcopy KdSoft.Utils\bin\debug\*.nupkg artifacts
xcopy KdSoft.Utils.Portable\bin\debug\*.nupkg artifacts
REM xcopy KdSoft.Utils.VeryPortable\bin\debug\*.nupkg artifacts
xcopy KdSoft.Quartz.Jobs\bin\debug\*.nupkg artifacts


pause


