@ECHO OFF
SET DIR=%~dp0%
%DIR%\Tools\Nake\Nake.exe -f %DIR%\Nake.csx -d %DIR% %*