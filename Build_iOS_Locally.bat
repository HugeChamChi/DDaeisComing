@echo off
setlocal

:: Set variables
set "SOURCE_DIR=%~dp0"
:: Remove trailing backslash if present
if "%SOURCE_DIR:~-1%"=="\" set "SOURCE_DIR=%SOURCE_DIR:~0,-1%"

set "TARGET_DIR=%SOURCE_DIR%_iOS"
set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe"

echo =======================================================
echo 1. Syncing project to iOS clone directory...
echo From: %SOURCE_DIR%
echo To:   %TARGET_DIR%
echo =======================================================

:: Use robocopy to mirror the project.
:: /MIR: Mirror directory tree (equivalent to /e plus /purge)
:: /XD: Exclude directories we don't want to copy (Library, Logs, Temp, obj, .git, Builds)
robocopy "%SOURCE_DIR%" "%TARGET_DIR%" /MIR /XD Library Logs Temp obj .git .vs Builds

:: Robocopy exit codes: 0 = No files copied, 1 = Files copied. <=3 is usually fine.
if %ERRORLEVEL% GEQ 8 (
    echo Robocopy failed with error level %ERRORLEVEL%.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo =======================================================
echo 2. Running Unity Headless Build for iOS...
echo =======================================================

if not exist "%UNITY_EXE%" (
    echo [Error] Unity.exe not found at "%UNITY_EXE%".
    echo Please edit this script and update the UNITY_EXE path.
    pause
    exit /b 1
)

:: Run Unity in batch mode targeting the cloned project
"%UNITY_EXE%" -quit -batchmode -projectPath "%TARGET_DIR%" -executeMethod iOSBuildScript.BuildIOS -buildTarget iOS -logFile "%TARGET_DIR%\Logs\iOS_Build_Log.txt"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo =======================================================
    echo [Success] iOS Build completed!
    echo Xcode project is located at: %TARGET_DIR%\Builds\iOS
    echo =======================================================
) else (
    echo.
    echo =======================================================
    echo [Failed] Unity build failed with error code %ERRORLEVEL%.
    echo Check the log at: %TARGET_DIR%\Logs\iOS_Build_Log.txt
    echo =======================================================
)

pause
endlocal
