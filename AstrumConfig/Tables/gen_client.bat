set GEN_CLIENT=..\..\AstrumTool\Luban\Luban.dll
set CONF_ROOT=.

dotnet %GEN_CLIENT% ^
    -t client ^
    -c cs-bin ^
    -d bin ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=..\..\AstrumProj\Assets\Script\Generated\Table ^
    -x outputDataDir=output\Client

pause








