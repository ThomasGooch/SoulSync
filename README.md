# 💕 SoulSync - AI-Powered Dating Platform

[![Build](https://github.com/your-username/SoulSyncDatingApp/actions/workflows/build.yml/badge.svg)](https://github.com/your-username/SoulSyncDatingApp/actions/workflows/build.yml)
[![Tests](https://github.com/your-username/SoulSyncDatingApp/actions/workflows/test.yml/badge.svg)](https://github.com/your-username/SoulSyncDatingApp/actions/workflows/test.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![GenericAgents](https://img.shields.io/badge/GenericAgents-1.2.0-blue.svg)](https://www.nuget.org/packages/GenericAgents.Core/)

**An intelligent dating platform built with .NET 8, Blazor, and AI-powered agents following 12-Factor App methodology.**

## 🚀 Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Git](https://git-scm.com/)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

### Setup
```bash
# Clone the repository
git clone https://github.com/your-username/SoulSyncDatingApp.git
cd SoulSyncDatingApp

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
SoulSyncDatingApp/
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

### Intelligent Matching (Phase 2 ✅)
- **Compatibility Analysis**: Multi-factor AI scoring using personality, interests, lifestyle, and values
- **Preference Learning**: Adaptive algorithms that learn from user match history and behavior
- **Smart Recommendations**: Personalized match ranking with preference-weighted scoring
- **Fallback Mechanisms**: Algorithmic compatibility calculation when AI services are unavailable

### Communication Enhancement  
- **Real-time Safety Monitoring**: AI-powered content moderation and harassment detection
- **Conversation Coaching**: Intelligent suggestions to improve communication
- **Message Analysis**: Sentiment analysis and relationship health insights

### Premium Features
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

## 📋 Implementation Phases

### ✅ Phase 1: Foundation & User Management (Completed)
- User registration with AI profile analysis
- Secure authentication and authorization
- Basic profile management
- **Status**: ✅ Completed with 62 tests

### ✅ Phase 2: AI-Powered Matching Engine (Completed)
- Intelligent compatibility scoring
- Multi-factor analysis algorithms
- Dynamic preference learning
- AI-driven match ranking
- **Status**: ✅ Completed with 26 tests

### 🔄 Phase 3: Communication & Messaging
- Real-time messaging system
- AI-powered safety monitoring
- Conversation enhancement features
- **Status**: Planned

### 🔄 Phase 4: Advanced Features & Monetization
- Premium subscription management
- Advanced analytics and insights
- Revenue optimization features
- **Status**: Planned

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
- **Issues**: [GitHub Issues](https://github.com/your-username/SoulSyncDatingApp/issues)
- **Discussions**: [GitHub Discussions](https://github.com/your-username/SoulSyncDatingApp/discussions)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Built with ❤️ using .NET 8, GenericAgents, and AI innovation**