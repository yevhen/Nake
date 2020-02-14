@ECHO OFF
SET DIR=%~dp0%
dotnet %DIR%\Tools\Nake\Nake.dll -f %DIR%\Nake.csx -d %DIR% %*