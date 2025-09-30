#!/bin/bash

WORKSPACE=..
LUBAN_DLL=$WORKSPACE/../AstrumTool/Luban/Luban.dll
CONF_ROOT=.

dotnet $LUBAN_DLL \
    -t all \
    -d json \
    --conf $CONF_ROOT/luban.conf \
    -x outputDataDir=output