#!/bin/bash
set -e

# Path to app directory
APP_DIR="/app/"

# Path to the main application DLL
APP_DLL="${APP_DIR}Portfolio.dll"

# Start nginx and then run the ASP.NET app (exec so signals are forwarded)
if command -v service >/dev/null 2>&1; then
  service nginx start || true
else
  if command -v nginx >/dev/null 2>&1; then
    nginx || true
  fi
fi

if [ -n "$APP_DLL" ]; then
  echo "Starting app: $APP_DLL"
  exec dotnet "$APP_DLL" --urls http://0.0.0.0:5000
else
  echo "No app DLL found. Sleeping..."
  sleep infinity
fi

