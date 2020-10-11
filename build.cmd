@echo off
setlocal EnableDelayedExpansion
rem ############################################
rem # Build script
rem ############################################

rem ### Build Script Optional Configuration ###
set PROJECT=Xim
set VERSION=%1

rem ### Build variables
set ROOT_DIR=%~dp0
set ROOT_DIR=%ROOT_DIR:~0,-1%
set BUILD_DIR=%ROOT_DIR%\build
set PACKAGES_DIR=%BUILD_DIR%\packages
set SLN=%ROOT_DIR%\%PROJECT%.sln

if "%VERSION%"=="" set VERSION=0.0.0

echo.
echo ##########################################################
echo # %PROJECT% Build Script (%~nx0)
echo #   Repository Directory:     %ROOT_DIR%
echo #   Build Directory:          %BUILD_DIR%
echo #   Build Packages Directory: %PACKAGES_DIR%
echo #
echo #   Solution:                 %SLN%
echo #
echo #   Version:                  %VERSION%
echo #
echo.

:PREREQUISITES
cd "%ROOT_DIR%"
if exist "%PACKAGES_DIR%" rmdir /S /Q "%PACKAGES_DIR%"
if %ERRORLEVEL% NEQ 0 goto QUIT
if not exist "%BUILD_DIR%" (
    echo Creating directory "%BUILD_DIR%" ...
    mkdir "%BUILD_DIR%"
    if %ERRORLEVEL% NEQ 0 goto QUIT
)
if not exist "%PACKAGES_DIR%" (
    echo Creating directory "%PACKAGES_DIR%" ...
    mkdir "%PACKAGES_DIR%"
    if %ERRORLEVEL% NEQ 0 goto QUIT
)
if not exist "%ROOT_DIR%\global.json" (
    set GLOBAL_JSON=global.json_a657b76d
    dotnet new globaljson --sdk-version 3.1
    if %ERRORLEVEL% NEQ 0 goto QUIT
)
dotnet build-server shutdown
IF %ERRORLEVEL% NEQ 0 GOTO QUIT

:NUCLEAN
if "%VERSION%" NEQ "0.0.0" goto CLEAN
for %%a IN (Xim.Core Xim.Simulators.Api Xim.Simulators.ServiceBus) DO (
    set PKGCORE=%USERPROFILE%\.nuget\packages\%%a\0.0.0
    if exist "!PKGCORE!" (
        echo Removing old local package "!PKGCORE!" ...
        rmdir /S /Q "!PKGCORE!"
        if %ERRORLEVEL% NEQ 0 goto QUIT
    )
    echo.
)

:CLEAN
echo Cleaning %PROJECT% ...
dotnet clean "%SLN%" --configuration Release
if %ERRORLEVEL% NEQ 0 goto QUIT
echo Clean completed.
echo.

:BUILD
echo Building %PROJECT% ...
dotnet build "%SLN%" --configuration Release
if %ERRORLEVEL% NEQ 0 goto QUIT
echo Build completed.
echo.

:TEST
echo Testing %PROJECT% ...
dotnet test "%SLN%" --configuration Release --blame --no-build --no-restore --settings "%ROOT_DIR%\.runsettings" --results-directory "%BUILD_DIR%\tests"
if %ERRORLEVEL% NEQ 0 goto QUIT
echo Tests completed.
echo.

:SIGN
echo Signing %PROJECT% ...
dotnet build "%ROOT_DIR%\src\Xim\Xim.csproj" --configuration Signed -p:Version=%VERSION%.0
if %ERRORLEVEL% NEQ 0 goto QUIT
dotnet build "%ROOT_DIR%\src\Xim.Simulators.Api\Xim.Simulators.Api.csproj" --configuration Signed -p:Version=%VERSION%.0
if %ERRORLEVEL% NEQ 0 goto QUIT
dotnet build "%ROOT_DIR%\src\Xim.Simulators.ServiceBus\Xim.Simulators.ServiceBus.csproj" --configuration Signed -p:Version=%VERSION%.0
if %ERRORLEVEL% NEQ 0 goto QUIT
echo Signing completed.
echo.

:PACK
echo Packaging %PROJECT% ...
dotnet pack --nologo --no-build "%ROOT_DIR%\src\Xim\Xim.csproj" -p:NuspecFile="%ROOT_DIR%\src\Xim\Xim.nuspec" -p:NuspecBasePath="%ROOT_DIR%\src\Xim" -p:NuspecProperties="version=%VERSION%" --output "%PACKAGES_DIR%"
if %ERRORLEVEL% NEQ 0 goto QUIT
dotnet pack --nologo --no-build "%ROOT_DIR%\src\Xim.Simulators.Api\Xim.Simulators.Api.csproj" -p:NuspecFile="%ROOT_DIR%\src\Xim.Simulators.Api\Xim.Simulators.Api.nuspec" -p:NuspecBasePath="%ROOT_DIR%\src\Xim.Simulators.Api" -p:NuspecProperties="version=%VERSION%" --output "%PACKAGES_DIR%"
if %ERRORLEVEL% NEQ 0 goto QUIT
dotnet pack --nologo --no-build "%ROOT_DIR%\src\Xim.Simulators.ServiceBus\Xim.Simulators.ServiceBus.csproj" -p:NuspecFile="%ROOT_DIR%\src\Xim.Simulators.ServiceBus\Xim.Simulators.ServiceBus.nuspec" -p:NuspecBasePath="%ROOT_DIR%\src\Xim.Simulators.ServiceBus" -p:NuspecProperties="version=%VERSION%" --output "%PACKAGES_DIR%"
if %ERRORLEVEL% NEQ 0 goto QUIT
echo Packaging completed.
echo.

:DONE
if "%GLOBAL_JSON%"=="global.json_a657b76d" (
    if exist global.json del /F global.json
)
echo.
echo Done.
echo.
exit /b

:QUIT
set ERR=%ERRORLEVEL%
if "%GLOBAL_JSON%"=="global.json_a657b76d" (
    if exist global.json del /F global.json
)
echo.
echo Build failed.
echo.
exit /b %ERR%