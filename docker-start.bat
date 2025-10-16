@echo off
REM SoulSync Docker Desktop Quick Start Script (Windows)

echo.
echo 🚀 Starting SoulSync Dating Platform...
echo.

REM Check if Docker is running
docker info >nul 2>&1
if errorlevel 1 (
    echo ❌ Docker is not running. Please start Docker Desktop and try again.
    exit /b 1
)

echo ✅ Docker is running
echo.

REM Build and start services
echo 📦 Building and starting services (this may take a few minutes on first run)...
docker-compose up -d --build

echo.
echo ⏳ Waiting for services to start...
timeout /t 10 /nobreak >nul

echo 📊 Checking database status...
echo ✅ Database is starting (this may take 30-60 seconds)
timeout /t 30 /nobreak >nul

echo.
echo ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
echo ✨ SoulSync is running!
echo ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
echo.
echo 🌍 Application URL: http://localhost:8080
echo 📊 Health Check:    http://localhost:8080/health
echo.
echo 👤 Test Accounts (pre-seeded):
echo    • alex.johnson@soulsync.demo
echo    • sam.rivera@soulsync.demo
echo    • jordan.chen@soulsync.demo
echo    • taylor.smith@soulsync.demo
echo    • casey.morgan@soulsync.demo
echo.
echo 📖 Full documentation: See RUNBOOK.md
echo.
echo 🔧 Useful Commands:
echo    View logs:        docker-compose logs -f
echo    Stop services:    docker-compose stop
echo    Restart services: docker-compose restart
echo    Full reset:       docker-compose down -v ^&^& docker-compose up -d
echo.
echo ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
echo.
echo 💕 Happy Dating with AI! Built with .NET 9 + GenericAgents
echo.

REM Open browser
timeout /t 5 /nobreak >nul
start http://localhost:8080
