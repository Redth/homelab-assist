@echo off
REM Windows launcher for the npm-mcp Native AOT MCP server.
REM Downloads the win-x64 binary from GitHub Releases (tag pinned in VERSION) on first run,
REM caches it, and runs it. No .NET runtime required.
setlocal enabledelayedexpansion

set "DIR=%~dp0"
set "REPO=Redth/homelab-assist"
set /p VERSION=<"%DIR%VERSION"
set "RID=win-x64"

if defined CLAUDE_PLUGIN_DATA ( set "CACHE=%CLAUDE_PLUGIN_DATA%" ) else ( set "CACHE=%DIR%.cache" )
set "BIN=%CACHE%\npm-mcp-%VERSION%-%RID%.exe"

if not exist "%BIN%" (
  if not exist "%CACHE%" mkdir "%CACHE%"
  set "URL=https://github.com/%REPO%/releases/download/%VERSION%/npm-mcp-%RID%.exe"
  echo npm-mcp: downloading %VERSION% (%RID%)... 1>&2
  curl -fsSL "!URL!" -o "%BIN%"
  if errorlevel 1 (
    echo npm-mcp: download failed 1>&2
    exit /b 1
  )
)

"%BIN%" %*
