@echo off
REM Library Discovery - Start Both Backend and Frontend

echo.
echo 📚 Library Discovery - Starting Application
echo.

REM Start Backend
echo 🚀 Starting Backend API ^(https://localhost:7194^)...
start "Library Discovery Backend" cmd /k "cd /d %~dp0 && dotnet run --project LibraryDiscovery"

REM Wait for backend to start
timeout /t 3 /nobreak

REM Start Frontend
echo 🚀 Starting Frontend ^(http://localhost:5173^)...
start "Library Discovery Frontend" cmd /k "cd /d %~dp0\frontend && npm install && npm run dev"

echo.
echo ✅ Application is starting!
echo.
echo 📱 Frontend: http://localhost:5173
echo 🔌 Backend API: https://localhost:7194
echo 📖 API Docs: https://localhost:7194/swagger
echo.
echo Close either command window to stop that service.
