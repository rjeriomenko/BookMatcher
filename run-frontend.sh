#!/bin/bash

echo "=========================================="
echo "BookMatcher Frontend Setup"
echo "=========================================="
echo ""

# Check if Node.js is installed
if ! command -v node &> /dev/null; then
    echo "Error: Node.js is not installed. Please install Node.js 18+ from:"
    echo "https://nodejs.org/"
    exit 1
fi

# Check if npm is installed
if ! command -v npm &> /dev/null; then
    echo "Error: npm is not installed. Please install Node.js with npm from:"
    echo "https://nodejs.org/"
    exit 1
fi

cd frontend

# Create .env.local if it doesn't exist
if [ ! -f .env.local ]; then
    echo "Creating frontend/.env.local..."
    echo "VITE_API_URL=http://localhost:5000" > .env.local
    echo "✓ Created frontend/.env.local"
    echo ""
fi

# Install dependencies if needed
if [ ! -d "node_modules" ]; then
    echo "Installing frontend dependencies..."
    npm install
    echo ""
fi

echo "=========================================="
echo "Starting frontend development server..."
echo "=========================================="
echo ""
echo "Frontend: http://localhost:5173"
echo "Backend API: http://localhost:5000 (make sure it's running)"
echo ""
echo "Press Ctrl+C to stop the frontend server"
echo ""

npm run dev
