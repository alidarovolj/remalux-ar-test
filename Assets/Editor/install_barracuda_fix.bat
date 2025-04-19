@echo off
REM Batch script to fix Barracuda dependencies in Unity projects
REM This addresses circular reference issues with Barracuda in Unity

echo Fixing Barracuda dependencies for Windows...

REM Create Plugins directory
if not exist "Assets\Plugins\BarracudaFix" (
    echo Creating Plugins directory...
    mkdir "Assets\Plugins\BarracudaFix"
)

REM Create assembly reference file
echo Creating assembly reference file...
echo { > "Assets\asmref-barracuda-fix.asmref"
echo     "reference": "Unity.Barracuda" >> "Assets\asmref-barracuda-fix.asmref"
echo } >> "Assets\asmref-barracuda-fix.asmref"

REM Find Barracuda DLL
echo Searching for Barracuda DLL...
set "FOUND_DLL="
for /r "Library" %%f in (Unity.Barracuda.dll) do (
    set "FOUND_DLL=%%f"
    goto :found_dll
)

:found_dll
if defined FOUND_DLL (
    echo Found DLL at: %FOUND_DLL%
    echo Copying DLL to Plugins folder...
    copy "%FOUND_DLL%" "Assets\Plugins\BarracudaFix\" /Y
) else (
    echo ERROR: Could not find Unity.Barracuda.dll in the Library folder.
    echo Please ensure Unity is properly installed and the project has been opened at least once.
    goto :end
)

REM Delete temporary Unity files to force recompilation
echo Deleting temporary compiler files...
if exist "Temp" (
    rd /s /q "Temp"
)
if exist "Library\ScriptAssemblies" (
    rd /s /q "Library\ScriptAssemblies"
)

echo.
echo Barracuda fix completed!
echo -------------------------
echo Please restart Unity to apply the changes.
echo After restart, go to "Tools > Fix All Barracuda Issues" in the Unity menu.

:end
pause 