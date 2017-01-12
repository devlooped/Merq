:: Optional batch file to quickly build with some defaults.
:: Alternatively, this batch file can be invoked passing msbuild parameters, like: build.cmd "/v:detailed" "/t:Rebuild"

@ECHO OFF

:: Ensure MSBuild can be located. Allows for a better error message below.
set msb="%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe"
IF NOT EXIST %msb% set msb="%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe"
IF NOT EXIST %msb% set msb="%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"
IF NOT EXIST %msb% set msb="%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe"
IF NOT EXIST %msb% set msb="%ProgramFiles(x86)%\MSBuild\12.0\Bin\MSBuild.exe"

IF NOT EXIST %msb% (
    echo "Please ensure MSBuild (12.0, 14.0 or 15.0) is installed."
    exit /b -1
)

SETLOCAL ENABLEEXTENSIONS ENABLEDELAYEDEXPANSION
PUSHD "%~dp0" >NUL

IF EXIST .nuget\nuget.exe goto restore
IF NOT EXIST .nuget md .nuget
echo Downloading latest version of NuGet.exe...
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "$ProgressPreference = 'SilentlyContinue'; Invoke-WebRequest 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile .nuget/nuget.exe"

:restore
:: Build script packages have no version in the path, so we install them to .nuget\packages to avoid conflicts with 
:: solution/project packages.
IF NOT EXIST packages.config goto run
.nuget\nuget.exe install packages.config -OutputDirectory .nuget\packages -ExcludeVersion -Verbosity quiet

:run
IF "%Verbosity%"=="" (
    set Verbosity=minimal
)

ECHO ON
%msb% build.proj /v:%Verbosity% /nr:false %1 %2 %3 %4 %5 %6 %7 %8 %9
@ECHO OFF

POPD >NUL
ENDLOCAL
