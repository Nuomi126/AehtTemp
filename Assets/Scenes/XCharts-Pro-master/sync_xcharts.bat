@echo off
setlocal

call :CheckAndClone "XCharts"
call :CheckAndClone "XCharts-UI"
call :CheckAndClone "XCharts-Bar3DChart"
call :CheckAndClone "XCharts-FunnelChart"
call :CheckAndClone "XCharts-GanttChart"
call :CheckAndClone "XCharts-GaugeChart"
call :CheckAndClone "XCharts-LiquidChart"
call :CheckAndClone "XCharts-Pie3DChart"
call :CheckAndClone "XCharts-PictorialBarChart"
call :CheckAndClone "XCharts-PyramidChart"
call :CheckAndClone "XCharts-TreemapChart"
call :CheckAndClone "XCharts-SankeyChart"
call :CheckAndClone "XCharts-Line3DChart"
call :CheckAndClone "XCharts-GraphChart"

rmdir /s /q "XCharts-Demo~"
xcopy "..\..\..\XCharts-Demo\Assets\*" "XCharts-Demo~\" /E /I /Y /H /exclude:sync_exclude.txt

goto :eof

:CheckAndClone
set "FOLDER=%~1"
set "FOLDER_PATH=%FOLDER%\"
set "FOLDER_SOUR=..\..\..\XCharts\Assets\%FOLDER%\"

if exist "%FOLDER_PATH%" (
    rmdir /s /q "%FOLDER_PATH%"
)

xcopy /E /I /Y /H "%FOLDER_SOUR%" "%FOLDER_PATH%" /exclude:sync_exclude.txt

goto :eof
