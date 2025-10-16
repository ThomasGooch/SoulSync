# 💕 SoulSync - AI-Powered Dating Platform

[![Build](https://github.com/ThomasGooch/SoulSync/actions/workflows/build.yml/badge.svg)](https://github.com/ThomasGooch/SoulSync/actions/workflows/build.yml)
[![Tests](https://github.com/ThomasGooch/SoulSync/actions/workflows/test.yml/badge.svg)](https://github.com/ThomasGooch/SoulSync/actions/workflows/test.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![GenericAgents](https://img.shields.io/badge/GenericAgents-1.2.0-blue.svg)](https://www.nuget.org/packages/GenericAgents.Core/)

**An intelligent dating platform built with .NET 8, Blazor, and AI-powered agents following 12-Factor App methodology.**

## 🚀 Quick Start

### Option 1: Docker Desktop (Recommended) 🐳

**Get running in 5 minutes!**

```bash
# Clone the repository
git clone https://github.com/ThomasGooch/SoulSync.git
cd SoulSync

# Start with Docker Desktop
./docker-start.sh       # macOS/Linux
docker-start.bat        # Windows

# Or manually
docker-compose up -d
```

**Access**: http://localhost:8080

**Test Accounts** (pre-seeded):
- alex.johnson@soulsync.demo
- sam.rivera@soulsync.demo
- jordan.chen@soulsync.demo

📖 **Full Docker Tutorial**: See [RUNBOOK.md](RUNBOOK.md) or [DOCKER-QUICKSTART.md](DOCKER-QUICKSTART.md)

---

### Option 2: Local Development

**Prerequisites**:
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Git](https://git-scm.com/)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

**Setup**:
```bash
# Clone the repository
git clone https://github.com/ThomasGooch/SoulSync.git
cd SoulSync

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Start the web application
cd src/SoulSync.Web
dotnet run
```

Visit `https://localhost:7001` to access the application.

## 🏗️ Architecture

### Technology Stack
- **.NET 8** - Modern cross-platform framework
- **Blazor Server/WASM** - Interactive web UI with C#
- **GenericAgents** - AI-powered agent framework
- **Entity Framework Core** - Data access and ORM
- **SignalR** - Real-time communication
- **xUnit + NSubstitute + FluentAssertions** - Comprehensive testing

### Project Structure
```
SoulSync/
├── 📁 src/
│   ├── 🌐 SoulSync.Web/          # Blazor frontend application
│   ├── 🔧 SoulSync.Core/         # Domain models & core interfaces  
│   ├── 🤖 SoulSync.Agents/       # AI agent implementations
│   ├── 💾 SoulSync.Data/         # Data access & Entity Framework
│   └── ⚙️ SoulSync.Services/     # Business logic & application services
├── 📁 tests/                     # Comprehensive test suite
├── 📁 .github/workflows/         # CI/CD automation
└── 📄 DATING-APP-PHASED-IMPLEMENTATION.md
```

## 🤖 AI-Powered Features

### User Onboarding & Profiles (Phase 1 ✅)
- **AI Profile Analysis**: Automated extraction of personality traits, interests, and values
- **Smart Validation**: Intelligent form validation with helpful suggestions
- **Profile Completion**: AI-assisted profile enhancement recommendations

### Intelligent Matching (Phase 2 ✅)
- **Compatibility Analysis**: Multi-factor AI scoring using personality, interests, lifestyle, and values
- **Preference Learning**: Adaptive algorithms that learn from user match history and behavior
- **Smart Recommendations**: Personalized match ranking with preference-weighted scoring
- **Fallback Mechanisms**: Algorithmic compatibility calculation when AI services are unavailable

### Communication Enhancement (Phase 3 ✅)
- **Real-time Message Processing**: Instant message validation and delivery tracking
- **AI Safety Monitoring**: Automated content moderation with multi-level escalation
- **Conversation Coaching**: Intelligent suggestions to improve communication quality
- **Sentiment Analysis**: Real-time conversation health monitoring
- **Smart Flagging**: Automated detection and handling of inappropriate content

### Premium Features (Phase 4)
- **AI Date Suggestions**: Personalized date ideas based on couple compatibility
- **Relationship Insights**: Deep analytics on communication patterns and compatibility
- **Smart Notifications**: Intelligent timing and content for user engagement

## 🧪 Testing Strategy

### Test-Driven Development (TDD)
All features are developed using TDD methodology with comprehensive test coverage:

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/SoulSync.Agents.Tests/

# Watch tests during development
dotnet test --watch
```

### Current Test Coverage
- **Total Tests**: 217 (100% pass rate)
- **Phase 1 Tests**: 62 (User management & profiles)
- **Phase 2 Tests**: 26 (Matching engine)
- **Phase 3 Tests**: 49 (Communication system)
- **Phase 4 Tests**: 39 (Premium features)
- **Phase 5 Tests**: 21 (UI components)
- **Infrastructure Tests**: 20 (Services & infrastructure)

### Test Categories
- **Unit Tests**: Individual component testing with >90% coverage
- **Integration Tests**: Agent workflow and service interaction testing  
- **Component Tests**: Blazor UI component testing
- **End-to-End Tests**: Full user journey validation

## 🔧 Development

### Environment Setup
Create `appsettings.Development.json`:
```json
{
  "AI": {
    "Provider": "OpenAI",
    "ApiKey": "your-api-key-here",
    "ModelId": "gpt-4",
    "MaxTokens": 2000
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SoulSyncDev;Trusted_Connection=true"
  },
  "JWT": {
    "SigningKey": "your-development-signing-key-256-bit",
    "Issuer": "SoulSync",
    "Audience": "SoulSyncUsers"
  }
}
```

### Development Commands
```bash
# Watch for file changes
cd src/SoulSync.Web && dotnet watch run

# Database migrations
cd src/SoulSync.Data
dotnet ef migrations add InitialCreate
dotnet ef database update

# Code formatting
dotnet format

# Package restore
dotnet restore
```

## 🚀 Deployment

### CI/CD Pipeline
The project includes GitHub Actions workflows for automated:
- **Build Validation**: Code compilation and package restore
- **Test Execution**: Comprehensive test suite with coverage reporting
- **Quality Gates**: Code quality and security checks
- **Deployment**: Automated deployment to staging/production environments

### Environment Configuration
Production deployments use environment variables for configuration:
- `AI__ApiKey` - OpenAI/Azure AI service key
- `ConnectionStrings__DefaultConnection` - Database connection
- `JWT__SigningKey` - JWT token signing key
- `Azure__KeyVault__Uri` - Secret management

## 🐳 Docker Deployment

### What's Included

The Docker setup provides:
- **Complete Application Stack**: Web + Database
- **Pre-seeded Test Data**: 5 user accounts with profiles
- **Zero Configuration**: Works out of the box
- **12-Factor Compliance**: Environment-based configuration

### Services

| Service | Port | Description |
|---------|------|-------------|
| Web Application | 8080 | Blazor frontend + .NET backend |
| SQL Server | 1433 | Database with test data |

### Quick Commands

```bash
# Start
docker-compose up -d

# View logs
docker-compose logs -f

# Stop
docker-compose stop

# Restart
docker-compose restart

# Reset (deletes data)
docker-compose down -v
```

📖 **Comprehensive Guide**: [RUNBOOK.md](RUNBOOK.md) - Complete Docker Desktop tutorial with:
- GenericAgents integration showcase
- User flow demonstrations
- Test account details
- Troubleshooting guide
- Known limitations documentation

---

## 📋 Implementation Phases

### ✅ Phase 1: Foundation & User Management (Completed)
- User registration with AI profile analysis
- Secure authentication and authorization (JWT)
- Basic profile management (CRUD)
- Environment-specific configuration
- **Status**: ✅ Completed with 62 tests
- **Documentation**: [phase1.md](docs/phase1.md)

### ✅ Phase 2: AI-Powered Matching Engine (Completed)
- Intelligent compatibility scoring (4 factors)
- Multi-factor analysis algorithms
- Dynamic preference learning
- AI-driven match ranking with boosting
- **Status**: ✅ Completed with 26 tests
- **Documentation**: [phase2.md](docs/phase2.md)

### ✅ Phase 3: Communication & Messaging System (Completed)
- Real-time message processing
- AI-powered safety monitoring (5 escalation levels)
- Conversation coaching with AI suggestions
- Message status tracking (sent, delivered, read)
- Automated content moderation
- **Status**: ✅ Completed with 49 tests
- **Documentation**: [phase3.md](docs/phase3.md)

### 🚧 Phase 4: Advanced Features & Monetization (Partially Completed)
- Premium subscription management (4 tiers: Free, Basic, Premium, Elite)
- AI-powered date suggestions with fallback
- Subscription lifecycle management (create, upgrade, downgrade, cancel, renew)
- Date suggestion workflow (generate, accept, reject, schedule, complete)
- Feature access control based on subscription tier
- 🚧 Advanced analytics and user engagement insights (planned, not yet implemented)
- 🚧 Observability and metrics with Prometheus, Grafana, OpenTelemetry (planned, not yet implemented)
- 🚧 Payment processing integration with Stripe (planned, not yet implemented)
- Comprehensive test coverage for premium features (39 new tests, 190 total)
- Documentation: [phase4.md](docs/phase4.md), [phase4-analytics.md](docs/phase4-analytics.md), [phase4-observability.md](docs/phase4-observability.md), [phase4-payments.md](docs/phase4-payments.md)
- **Status**: Partially Completed (only subscription and date suggestion features implemented)

### ✅ Phase 5: Demo UI Components (Completed)
- **Login Page** (`/login`) - User authentication with form validation
- **Profile Page** (`/profile/{id}`) - User profile display with avatar, bio, and interests
- **Match Discovery** (`/matches`) - Browse potential matches with compatibility scores
- **Chat/Messaging** (`/chat/{conversationId}`) - Real-time messaging interface
- **Navigation Menu** - Updated with links to all major features
- **Status**: ✅ Completed with 21 UI tests (217 total tests)
- **Documentation**: [phase5-demo-ui.md](docs/phase5-demo-ui.md)

## 📊 12-Factor App Compliance

SoulSync follows [12-Factor App](https://12factor.net/) methodology:

| Factor | Implementation |
|--------|----------------|
| **Codebase** | Single repo with version control |
| **Dependencies** | Explicit NuGet package management |
| **Config** | Environment-based configuration |
| **Backing Services** | AI services as attached resources |
| **Build/Release/Run** | Automated CI/CD pipeline |
| **Processes** | Stateless agent operations |
| **Port Binding** | Self-contained service binding |
| **Concurrency** | Horizontal agent scaling |
| **Disposability** | Graceful startup/shutdown |
| **Dev/Prod Parity** | Environment consistency |
| **Logs** | Structured event streaming |
| **Admin Processes** | Management utilities |

## 🤝 Contributing

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Write** tests first (TDD approach)
4. **Implement** the feature with passing tests
5. **Commit** changes (`git commit -m 'Add amazing feature'`)
6. **Push** to branch (`git push origin feature/amazing-feature`)
7. **Open** a Pull Request

### Code Standards
- Follow TDD methodology
- Maintain >90% test coverage
- Use consistent naming conventions
- Document public APIs
- Follow async/await patterns

## 📞 Support

- **Documentation**: See [DATING-APP-PHASED-IMPLEMENTATION.md](./DATING-APP-PHASED-IMPLEMENTATION.md)
- **Issues**: [GitHub Issues](https://github.com/ThomasGooch/SoulSync/issues)
- **Discussions**: [GitHub Discussions](https://github.com/ThomasGooch/SoulSync/discussions)

## ⚠️ Known Limitations

Some features are **implemented in the backend** but not fully available in the Docker environment:

| Feature | Backend | Frontend | Status |
|---------|---------|----------|--------|
| Real-time Chat | ✅ Complete (49 tests) | ❌ Missing | Backend only |
| Advanced Analytics | ⚠️ Basic | ❌ Missing | Planned |
| Observability Dashboard | ✅ Framework | ❌ Missing | Needs Grafana |
| Payment Integration | ✅ Logic (39 tests) | ❌ Missing | No Stripe |

**Details**: See [docs/failures/](docs/failures/) for comprehensive documentation on each limitation, implementation guides, and workarounds.

**Note**: The Docker setup uses a **Mock AI Service**. To enable real OpenAI/Azure AI:
1. Get an API key from OpenAI/Azure
2. Update `docker-compose.yml`:
   ```yaml
   environment:
     - AI__Provider=OpenAI
     - AI__ApiKey=sk-your-actual-key
   ```

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Built with ❤️ using .NET 9, Blazor, GenericAgents, and AI innovation**