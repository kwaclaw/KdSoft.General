
for /d /r . %%d in (bin obj clientbin) do @if exist "%%d" echo "%%d" && rd /s/q "%%d"

del /s project.lock.json

pause


