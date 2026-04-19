#!/bin/bash

# Library Discovery - Start Both Backend and Frontend

echo "📚 Library Discovery - Starting Application"
echo ""

# Function to cleanup on exit
cleanup() {
    echo ""
    echo "🛑 Shutting down..."
    kill $BACKEND_PID 2>/dev/null
    kill $FRONTEND_PID 2>/dev/null
    wait $BACKEND_PID 2>/dev/null
    wait $FRONTEND_PID 2>/dev/null
}

trap cleanup EXIT INT TERM

# Start Backend
echo "🚀 Starting Backend API (https://localhost:7194)..."
cd "$(dirname "$0")"
dotnet run --project LibraryDiscovery/LibraryDiscovery.csproj &
BACKEND_PID=$!

# Wait for backend to start
sleep 3

# Start Frontend
echo "🚀 Starting Frontend (http://localhost:5173)..."
cd frontend
npm install > /dev/null 2>&1 &
sleep 2
npm run dev &
FRONTEND_PID=$!

echo ""
echo "✅ Application is running!"
echo ""
echo "📱 Frontend: http://localhost:5173"
echo "🔌 Backend API: https://localhost:7194"
echo "📖 API Docs: https://localhost:7194/swagger"
echo ""
echo "Press Ctrl+C to stop both services"

# Keep script running
wait
