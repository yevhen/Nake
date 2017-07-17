@ECHO OFF
SET DIR=%~dp0%
%DIR%\Source\Nake\bin\Publish\Nake.exe -f %DIR%\Nake.csx -d %DIR% %*