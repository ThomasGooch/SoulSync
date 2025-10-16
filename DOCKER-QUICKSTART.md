# 🐳 Docker Quick Start Guide

**Get SoulSync running locally in 5 minutes!**

---

## Prerequisites

1. **Docker Desktop** installed and running
   - Download: https://www.docker.com/products/docker-desktop
   - Minimum 4GB RAM allocated
   - Minimum 20GB disk space

2. **Git** (to clone repository)

---

## Quick Start (3 Steps)

### 1️⃣ Clone the Repository

```bash
git clone https://github.com/ThomasGooch/SoulSync.git
cd SoulSync
```

### 2️⃣ Start the Application

**macOS / Linux**:
```bash
./docker-start.sh
```

**Windows**:
```cmd
docker-start.bat
```

**Or manually**:
```bash
docker-compose up -d
```

### 3️⃣ Access the Application

Open your browser to: **http://localhost:8080**

---

## What Gets Started?

- **🌐 Web Application** (Port 8080) - Blazor frontend + .NET backend
- **🗄️ SQL Server Database** (Port 1433) - With pre-seeded test data

---

## Test Accounts (Pre-Seeded)

Try these accounts in the application:

| Name | Email | Age | Occupation |
|------|-------|-----|------------|
| Alex Johnson | alex.johnson@soulsync.demo | 25 | Software Engineer |
| Sam Rivera | sam.rivera@soulsync.demo | 28 | UX Designer |
| Jordan Chen | jordan.chen@soulsync.demo | 27 | Data Scientist |
| Taylor Smith | taylor.smith@soulsync.demo | 30 | Marketing Manager |
| Casey Morgan | casey.morgan@soulsync.demo | 26 | Teacher |

---

## Useful Commands

```bash
# View logs
docker-compose logs -f

# Stop (preserves data)
docker-compose stop

# Start again
docker-compose start

# Restart
docker-compose restart

# Stop and remove containers (preserves data volumes)
docker-compose down

# Complete reset (⚠️ DELETES ALL DATA)
docker-compose down -v
```

---

## Health Check

Verify the application is running:

```bash
curl http://localhost:8080/health
```

---

## Architecture

```
┌─────────────────────────────────┐
│     Docker Desktop              │
│                                 │
│  ┌──────────┐   ┌───────────┐ │
│  │  Web     │   │  Database │ │
│  │  :8080   │◄─►│  :1433    │ │
│  │          │   │           │ │
│  │ .NET 9   │   │ SQL 2022  │ │
│  │ Blazor   │   │ 5 Users   │ │
│  └──────────┘   └───────────┘ │
└─────────────────────────────────┘
```

---

## What Can You Explore?

### ✅ Implemented Features

1. **User Registration & Authentication**
   - AI-powered profile analysis
   - Interest extraction
   - Personality insights

2. **Intelligent Matching**
   - Multi-factor compatibility scoring
   - Preference learning
   - Match ranking

3. **Messaging System** (Backend)
   - Message processing
   - AI safety monitoring
   - Conversation coaching

4. **Premium Features**
   - Subscription management
   - AI date suggestions

### ⚠️ Known Limitations

- **No Real-time Chat UI** - Backend complete, frontend not implemented
- **Mock AI Service** - Using test mode, not real OpenAI/Azure AI
- **No Payment Integration** - Subscription logic works, Stripe not connected
- **No Analytics Dashboard** - Metrics collection framework in place

See [docs/failures/](docs/failures/) for detailed documentation.

---

## GenericAgents Showcase

SoulSync uses **12 GenericAgents NuGet packages** (v1.2.0):

| Package | Purpose | Usage in SoulSync |
|---------|---------|-------------------|
| **GenericAgents.Core** | Base agent framework | All 10 agents inherit from BaseAgent |
| **GenericAgents.AI** | AI service integration | Profile analysis, matching, safety |
| **GenericAgents.Configuration** | Environment config | 12-Factor App compliance |
| **GenericAgents.Security** | Auth & authorization | JWT authentication, RBAC |
| **GenericAgents.Communication** | Inter-agent messaging | Agent coordination |
| **GenericAgents.Orchestration** | Workflow engine | Multi-agent workflows |
| **GenericAgents.Tools** | Tool framework | Extensible tool system |
| **GenericAgents.Registry** | Tool discovery | Dynamic tool registration |
| **GenericAgents.Tools.Samples** | Pre-built tools | Common utilities |
| **GenericAgents.DI** | Dependency injection | Service registration |
| **GenericAgents.Observability** | Monitoring & metrics | Health checks, logging |

**Test Coverage**: 197 tests, 100% passing ✅

---

## Troubleshooting

### Port Already in Use

```bash
# Change port in docker-compose.yml
ports:
  - "8081:8080"  # Use 8081 instead
```

### Database Connection Failed

```bash
# Restart database
docker-compose restart db

# Wait 30 seconds for initialization
```

### Slow First Start

- **Normal**: First build takes 3-5 minutes
- **After that**: Starts in ~30 seconds
- **Tip**: Increase Docker Desktop RAM to 6GB+

### Reset Everything

```bash
docker-compose down -v && docker-compose up -d --build
```

---

## Next Steps

1. **📖 Read the Full Tutorial**: See [RUNBOOK.md](RUNBOOK.md)
2. **🧪 Run Tests**: `dotnet test`
3. **🔍 Explore Code**: Check `src/SoulSync.Agents/` for agent implementations
4. **📊 View Database**: Connect to localhost:1433 with SQL tools

---

## Support

- **Documentation**: [RUNBOOK.md](RUNBOOK.md)
- **Architecture**: [12-FACTOR-AGENTIC-APP-SHOWCASE.md](12-FACTOR-AGENTIC-APP-SHOWCASE.md)
- **Issues**: [GitHub Issues](https://github.com/ThomasGooch/SoulSync/issues)

---

## Key Features Highlighted

### 🤖 AI-Powered Intelligence
- Profile analysis extracts interests and personality traits
- Compatibility scoring considers multiple factors
- Safety monitoring prevents harassment
- Conversation coaching improves communication

### 🏗️ Production-Ready Architecture
- 12-Factor App methodology
- Comprehensive test coverage (197 tests)
- Clean agent-based design
- Environment-based configuration

### 📦 GenericAgents Integration
- 10 specialized agents
- Reusable agent patterns
- AI service abstraction
- Workflow orchestration

---

**💕 Enjoy exploring SoulSync with Docker Desktop!**

*Built with .NET 9, Blazor, and GenericAgents NuGet packages*
