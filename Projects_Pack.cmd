
DEL artifacts\*.nupkg

nuget pack -o artifacts KdSoft.Reflection\project.json
nuget pack -o artifacts KdSoft.CodeConfig\project.json
nuget pack -o artifacts KdSoft.Common.VeryPortable\project.json
nuget pack -o artifacts KdSoft.Reflection.Portable\project.json
nuget pack -o artifacts KdSoft.StorageBase\project.json
nuget pack -o artifacts KdSoft.TransientStorage\project.json
nuget pack -o artifacts KdSoft.Utils\project.json
nuget pack -o artifacts KdSoft.Utils.Portable\project.json
REM nuget pack -o artifacts KdSoft.Utils.VeryPortable\project.json
nuget pack -o artifacts KdSoft.Quartz.Jobs\project.json

pause


