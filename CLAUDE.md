# 💕 SoulSync Dating App - AI Agent Context

## Project Overview
**SoulSync** is an intelligent dating platform built with **.NET 8 Blazor** and **GenericAgents** packages, following **12-Factor App methodology** and **Test-Driven Development (TDD)** principles.

## Architecture & Technology Stack

### Core Technologies
- **.NET 8** - Latest .NET framework
- **Blazor Server/WebAssembly** - Modern web UI framework
- **GenericAgents** (v1.2.0) - AI-powered agent framework
- **xUnit + NSubstitute + FluentAssertions** - Testing stack
- **Entity Framework Core** - Data access
- **SignalR** - Real-time communication

### GenericAgents Package Distribution
```
SoulSync.Core:
├── GenericAgents.Core - Base agent abstractions
├── GenericAgents.Configuration - Environment configuration
└── GenericAgents.Security - Authentication & authorization

SoulSync.Agents:  
├── GenericAgents.AI - AI service integration
├── GenericAgents.Tools - Custom tool framework
├── GenericAgents.Registry - Tool discovery system
├── GenericAgents.Tools.Samples - Pre-built tools
├── GenericAgents.Communication - Inter-agent messaging
└── GenericAgents.Orchestration - Workflow engine

SoulSync.Services:
├── GenericAgents.Observability - Metrics & monitoring
└── GenericAgents.DI - Dependency injection patterns
```

## Project Structure
```
SoulSyncDatingApp/
├── src/
│   ├── SoulSync.Web/          # Blazor frontend
│   ├── SoulSync.Core/         # Domain models & interfaces
│   ├── SoulSync.Agents/       # AI agent implementations
│   ├── SoulSync.Data/         # Data access layer
│   └── SoulSync.Services/     # Business logic services
└── tests/
    ├── SoulSync.Core.Tests/
    ├── SoulSync.Agents.Tests/
    ├── SoulSync.Services.Tests/
    └── SoulSync.Web.Tests/
```

## Implementation Phases

### Phase 1: Foundation & User Management (Current)
**Focus**: User registration, profile management, basic AI analysis
**Agents**: UserRegistrationAgent, ProfileAnalysisAgent
**Testing**: TDD with comprehensive unit tests

### Phase 2: AI-Powered Matching Engine
**Focus**: Compatibility scoring, preference learning, match ranking
**Agents**: CompatibilityAgent, PreferenceLearningAgent, MatchRankingAgent
**Tools**: InterestExtractionTool, PersonalityAnalysisTool

### Phase 3: Communication & Messaging System
**Focus**: Real-time messaging, safety monitoring, conversation coaching
**Agents**: MessageProcessorAgent, SafetyMonitoringAgent, ConversationCoachAgent
**Workflows**: Message processing pipeline with safety checks

### Phase 4: Advanced Features & Monetization
**Focus**: Premium features, analytics, subscription management
**Agents**: DateSuggestionAgent, SubscriptionAgent, AnalyticsAgent

## Development Guidelines

### Code Standards
- **TDD First**: Write failing tests before implementation
- **Agent Pattern**: Use BaseAgent for all AI-powered operations  
- **12-Factor Compliance**: Environment-based configuration, stateless processes
- **Clean Architecture**: Domain-driven design with clear separation of concerns

### Testing Strategy
- **Unit Tests**: >90% code coverage using xUnit + NSubstitute
- **Integration Tests**: Agent workflow testing
- **End-to-End Tests**: Blazor component testing
- **Performance Tests**: Load testing for matching algorithms

### AI Agent Best Practices
- Always use dependency injection for agent services
- Implement proper error handling and retry logic
- Use structured logging with correlation IDs
- Follow async/await patterns consistently
- Implement comprehensive monitoring and metrics

### Build & Deployment
- **CI/CD**: GitHub Actions with build → test → deploy pipeline
- **Environments**: Development, Staging, Production
- **Configuration**: Environment variables for all external dependencies
- **Secrets**: Azure Key Vault for production secrets

## Key Interfaces & Contracts

### Core Agents
```csharp
IUserRegistrationAgent - User onboarding and profile creation
ICompatibilityAgent - AI-powered matching analysis  
IMessageProcessorAgent - Real-time message handling
ISafetyMonitoringAgent - Content moderation and safety
```

### Services
```csharp
IAIService - AI model integration (OpenAI/Azure)
IUserRepository - User data persistence
IMatchService - Matching algorithm orchestration
INotificationService - Real-time user notifications
```

## Environment Configuration

### Required Environment Variables
```bash
# AI Configuration
AI__Provider=OpenAI
AI__ApiKey=${AI_API_KEY}
AI__ModelId=gpt-4
AI__MaxTokens=2000

# Database
ConnectionStrings__DefaultConnection=${DB_CONNECTION_STRING}

# Authentication  
JWT__SigningKey=${JWT_SIGNING_KEY}
JWT__Issuer=SoulSync
JWT__Audience=SoulSyncUsers

# External Services
Azure__KeyVault__Uri=${KEYVAULT_URI}
SignalR__ConnectionString=${SIGNALR_CONNECTION}
```

## Commands

### Development
```bash
# Build solution
dotnet build

# Run tests
dotnet test

# Run web application
cd src/SoulSync.Web && dotnet run

# Watch for changes during development
cd src/SoulSync.Web && dotnet watch run
```

### Testing
```bash
# Run all tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/SoulSync.Agents.Tests/

# Run tests with logging
dotnet test --logger "console;verbosity=detailed"
```

## Important Implementation Notes

### AI Agent Development
- All agents must extend `BaseAgent` from GenericAgents.Core
- Use `IAIService` for consistent AI model interactions
- Implement proper cancellation token handling
- Always validate AI responses before processing

### Security Considerations
- All user inputs must be sanitized
- AI prompts should not contain sensitive user data
- Implement rate limiting for AI API calls
- Use secure authentication patterns throughout

### Performance Optimization  
- Implement caching for expensive AI operations
- Use background services for non-critical processing
- Optimize database queries with proper indexing
- Monitor and alert on high latency operations

## Recent Changes & Context
- Solution structure created with all GenericAgents packages
- Phased implementation plan documented
- Build pipeline configured
- Ready to begin Phase 1 implementation

---

**Next Steps**: Begin implementing UserRegistrationAgent with TDD approach, starting with failing tests for user profile creation and AI-powered profile analysis.