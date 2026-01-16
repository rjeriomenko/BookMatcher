#!/bin/bash

# This is an AI-generated quickstart script
# BookMatcher Local Setup Script
# This script helps you configure API keys and run the application

set -e

echo "=========================================="
echo "BookMatcher Local Setup"
echo "=========================================="
echo ""

# Check if appsettings.json exists
APPSETTINGS_PATH="BookMatcher.Api/appsettings.json"
if [ ! -f "$APPSETTINGS_PATH" ]; then
    echo "Error: appsettings.json not found at $APPSETTINGS_PATH"
    exit 1
fi

# Check if API keys are already configured
GEMINI_KEY=$(grep -o '"ApiKey": "[^"]*"' "$APPSETTINGS_PATH" | head -1 | cut -d'"' -f4)
OPENAI_KEY=$(grep -o '"ApiKey": "[^"]*"' "$APPSETTINGS_PATH" | tail -1 | cut -d'"' -f4)

if [[ "$GEMINI_KEY" == "YOUR_GEMINI_API_KEY_HERE" ]] || [[ "$OPENAI_KEY" == "YOUR_OPENAI_API_KEY_HERE" ]]; then
    echo "API keys not configured. Let's set them up."
    echo ""

    # Prompt for Gemini API key
    echo "Enter your Gemini API key (get one at https://aistudio.google.com/apikey):"
    read -r GEMINI_API_KEY

    # Prompt for OpenAI API key
    echo "Enter your OpenAI API key (get one at https://platform.openai.com/api-keys):"
    read -r OPENAI_API_KEY

    # Update appsettings.json with the provided keys
    if [[ "$OSTYPE" == "darwin"* ]]; then
        # macOS
        sed -i '' "s|YOUR_GEMINI_API_KEY_HERE|$GEMINI_API_KEY|g" "$APPSETTINGS_PATH"
        sed -i '' "s|YOUR_OPENAI_API_KEY_HERE|$OPENAI_API_KEY|g" "$APPSETTINGS_PATH"
    else
        # Linux
        sed -i "s|YOUR_GEMINI_API_KEY_HERE|$GEMINI_API_KEY|g" "$APPSETTINGS_PATH"
        sed -i "s|YOUR_OPENAI_API_KEY_HERE|$OPENAI_API_KEY|g" "$APPSETTINGS_PATH"
    fi

    echo ""
    echo "API keys configured successfully!"
    echo ""
else
    echo "API keys already configured in appsettings.json"
    echo ""
fi

# Ask user how they want to run the application
echo "How would you like to run the application?"
echo "1) Docker (recommended - no .NET installation required)"
echo "2) .NET CLI (requires .NET 10 SDK installed)"
echo ""
read -p "Enter your choice (1 or 2): " choice

echo ""

case $choice in
    1)
        echo "Starting application with Docker..."
        echo ""

        # Check if Docker is installed
        if ! command -v docker &> /dev/null; then
            echo "Error: Docker is not installed. Please install Docker Desktop from:"
            echo "https://www.docker.com/products/docker-desktop"
            exit 1
        fi

        # Check if docker-compose is available
        if command -v docker-compose &> /dev/null; then
            COMPOSE_CMD="docker-compose"
        elif docker compose version &> /dev/null; then
            COMPOSE_CMD="docker compose"
        else
            echo "Error: docker-compose is not available"
            exit 1
        fi

        # Build and run with docker-compose
        echo "Building Docker image (this may take a few minutes on first run)..."
        $COMPOSE_CMD build

        echo ""
        echo "Starting containers..."
        $COMPOSE_CMD up -d

        echo ""
        echo "=========================================="
        echo "BookMatcher API is now running!"
        echo "=========================================="
        echo ""
        echo "API URL: http://localhost:5000"
        echo "Swagger UI: http://localhost:5000/swagger"
        echo ""
        echo "Example request:"
        echo "curl \"http://localhost:5000/api/bookMatch/match?query=the%20cat%20in%20the%20hat&model=0&temperature=0.7\""
        echo ""
        echo "To view logs: $COMPOSE_CMD logs -f"
        echo "To stop: $COMPOSE_CMD down"
        echo ""
        ;;

    2)
        echo "Starting application with .NET CLI..."
        echo ""

        # Check if .NET is installed
        if ! command -v dotnet &> /dev/null; then
            echo "Error: .NET SDK is not installed. Please install .NET 10 SDK from:"
            echo "https://dotnet.microsoft.com/download/dotnet/10.0"
            exit 1
        fi

        # Check .NET version
        DOTNET_VERSION=$(dotnet --version | cut -d'.' -f1)
        if [ "$DOTNET_VERSION" -lt 10 ]; then
            echo "Warning: .NET 10 SDK is required. You have version $(dotnet --version)"
            echo "Please install .NET 10 SDK from: https://dotnet.microsoft.com/download/dotnet/10.0"
            exit 1
        fi

        # Restore and run
        echo "Restoring NuGet packages..."
        dotnet restore

        echo ""
        echo "Building and starting the API..."
        cd BookMatcher.Api

        echo ""
        echo "=========================================="
        echo "BookMatcher API is now running!"
        echo "=========================================="
        echo ""
        echo "API URL: http://localhost:5000"
        echo "Swagger UI: http://localhost:5000/swagger"
        echo ""
        echo "Example request:"
        echo "curl \"http://localhost:5000/api/bookMatch/match?query=the%20cat%20in%20the%20hat&model=0&temperature=0.7\""
        echo ""
        echo "Press Ctrl+C to stop the server"
        echo ""

        dotnet run --urls "http://localhost:5000"
        ;;

    *)
        echo "Invalid choice. Please run the script again and choose 1 or 2."
        exit 1
        ;;
esac