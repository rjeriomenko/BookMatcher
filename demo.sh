#!/bin/bash

echo "🚀 Starting BookMatcher Demo..."
echo ""

# Check if .env exists
if [ ! -f .env ]; then
    echo "⚠️  No .env file found. Creating from .env.example..."
    cp .env.example .env
    echo "📝 Please edit .env with your API keys before continuing."
    exit 1
fi

# Start the backend API
echo "🔧 Starting backend API with Docker Compose..."
docker-compose up -d

# Wait for API to be ready
echo "⏳ Waiting for API to be ready..."
sleep 5

# Start the frontend
echo "🎨 Starting frontend development server..."
cd BookMatcher.Web
npm run dev &
FRONTEND_PID=$!

echo ""
echo "✅ Demo is running!"
echo ""
echo "📖 Backend API: http://localhost:5000/swagger"
echo "🌐 Frontend: http://localhost:5173"
echo ""
echo "Press Ctrl+C to stop the demo"
echo ""

# Wait for Ctrl+C
trap "echo ''; echo '🛑 Stopping demo...'; docker-compose down; kill $FRONTEND_PID 2>/dev/null; exit" INT
wait
