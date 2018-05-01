@echo off
SET _input=%~1
SET _input=%_input:~-4%
if not "%_input%" == ".bmd" (
    if not "%_input%" == ".bdl"  (
        "%~dp0SuperBMD.exe" %1 --rotate
    )
)
if errorlevel 1 pause