#!/bin/bash

check_and_clone() {
    FOLDER=$1
    FOLDER_PATH=$FOLDER
    FOLDER_SOUR="../../../XCharts/Assets/$FOLDER"

    # If the folder exists, remove it
    if [ -d "$FOLDER_PATH" ]; then
        rm -rf "$FOLDER_PATH"
    fi

    # Copy files from source to destination
    rsync -av --exclude-from=sync_exclude.txt "$FOLDER_SOUR/" "$FOLDER_PATH/"
}

# Call the check_and_clone function for each folder
check_and_clone "XCharts"
check_and_clone "XCharts-UI"
check_and_clone "XCharts-Bar3DChart"
check_and_clone "XCharts-FunnelChart"
check_and_clone "XCharts-GanttChart"
check_and_clone "XCharts-GaugeChart"
check_and_clone "XCharts-LiquidChart"
check_and_clone "XCharts-Pie3DChart"
check_and_clone "XCharts-PictorialBar"
check_and_clone "XCharts-PyramidChart"
check_and_clone "XCharts-TreemapChart"
check_and_clone "XCharts-SankeyChart"
check_and_clone "XCharts-Line3DChart"
check_and_clone "XCharts-GraphChart"

rm -rf XCharts-Demo~
rsync -av --exclude-from=sync_exclude.txt ../../../XCharts-Demo/Assets/ XCharts-Demo~/
