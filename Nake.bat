@ECHO OFF
SET DIR=%~dp0%
dotnet Nake -f %DIR%\Nake.csx -d %DIR% %*