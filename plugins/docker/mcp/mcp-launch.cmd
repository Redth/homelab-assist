@echo off
REM Generic Windows launcher for homelab-assist native-AOT MCP servers.
REM Usage (from .mcp.json): command = mcp-launch.cmd, args = ["<name>"]  (e.g. "npm", "dockhand").
REM Binary base = "<name>-mcp"; release tag pinned in mcp\<name>\VERSION. No .NET runtime required.
setlocal enabledelayedexpansion

if "%~1"=="" ( echo mcp-launch: server name argument required 1>&2 & exit /b 2 )
set "NAME=%~1"
shift

set "ROOT=%~dp0"
set "DIR=%ROOT%%NAME%"
set "BINBASE=%NAME%-mcp"
set "REPO=Redth/homelab-assist"
set /p VERSION=<"%DIR%\VERSION"
set "RID=win-x64"

if defined CLAUDE_PLUGIN_DATA ( set "CACHE=%CLAUDE_PLUGIN_DATA%" ) else ( set "CACHE=%DIR%\.cache" )
set "BIN=%CACHE%\%BINBASE%-%VERSION%-%RID%.exe"

if not exist "%BIN%" (
  if not exist "%CACHE%" mkdir "%CACHE%"
  set "URL=https://github.com/%REPO%/releases/download/%VERSION%/%BINBASE%-%RID%.exe"
  echo %BINBASE%: downloading %VERSION% (%RID%)... 1>&2
  curl -fsSL "!URL!" -o "%BIN%"
  if errorlevel 1 ( echo %BINBASE%: download failed 1>&2 & exit /b 1 )
)

REM Resolve configured secrets from Windows Credential Vault if homelab-secret.ps1 convention is used.
REM (Env values set by the caller win; see mcp\<name>\SECRETS. Left to the user/session on Windows.)

"%BIN%" %*
