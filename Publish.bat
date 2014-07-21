@ECHO OFF
SET DIR=%~dp0%
%DIR%\Tools\Nake\Nake.exe -f %DIR%\Publish.csx -d %DIR% --runner publish %*