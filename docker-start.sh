#!/bin/bash

# SoulSync Docker Desktop Quick Start Script

echo "🚀 Starting SoulSync Dating Platform..."
echo ""

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker Desktop and try again."
    exit 1
fi

echo "✅ Docker is running"
echo ""

# Build and start services
echo "📦 Building and starting services (this may take a few minutes on first run)..."
docker-compose up -d --build

echo ""
echo "⏳ Waiting for services to start..."
sleep 10

# Check database status
echo "📊 Checking database status..."
for i in {1..30}; do
    if docker exec soulsync-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "SoulSync2024!SecurePassword" -C -Q "SELECT 1" > /dev/null 2>&1; then
        echo "✅ Database is ready"
        break
    fi
    if [ $i -eq 30 ]; then
        echo "❌ Database failed to start. Check logs with: docker-compose logs db"
        exit 1
    fi
    sleep 2
done

# Check web application status
echo "🌐 Checking web application status..."
for i in {1..30}; do
    if curl -s http://localhost:8080/health > /dev/null 2>&1; then
        echo "✅ Web application is ready"
        break
    fi
    if [ $i -eq 30 ]; then
        echo "⚠️  Web application may still be starting. Check status with: docker-compose logs web"
    fi
    sleep 2
done

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "✨ SoulSync is running!"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "🌍 Application URL: http://localhost:8080"
echo "📊 Health Check:    http://localhost:8080/health"
echo ""
echo "👤 Test Accounts (pre-seeded):"
echo "   • alex.johnson@soulsync.demo"
echo "   • sam.rivera@soulsync.demo"
echo "   • jordan.chen@soulsync.demo"
echo "   • taylor.smith@soulsync.demo"
echo "   • casey.morgan@soulsync.demo"
echo ""
echo "📖 Full documentation: See RUNBOOK.md"
echo ""
echo "🔧 Useful Commands:"
echo "   View logs:        docker-compose logs -f"
echo "   Stop services:    docker-compose stop"
echo "   Restart services: docker-compose restart"
echo "   Full reset:       docker-compose down -v && docker-compose up -d"
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "💕 Happy Dating with AI! Built with .NET 9 + GenericAgents"
echo ""
