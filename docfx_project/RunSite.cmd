echo off
start /B http://localhost:8091 && start docfx.exe serve _site -p 8091
exit

