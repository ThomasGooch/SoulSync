# 💕 SoulSync Dating App - Phase 4 Subphase: Payment Processing Integration

## Overview

This subphase document outlines the implementation of secure payment processing and subscription billing for the SoulSync Dating App. This is a subphase of Phase 4 that requires dedicated implementation due to its complexity, security requirements, and compliance considerations.

## 🎯 Objectives

- **Payment Gateway Integration**: Stripe or similar payment processor
- **Secure Payment Flow**: PCI-DSS compliant payment handling
- **Subscription Billing**: Automated recurring billing
- **Payment Methods**: Credit cards, digital wallets, and alternative payments
- **Invoice Generation**: Automated invoicing and receipts
- **Refund Processing**: Support for refunds and cancellations
- **Webhook Handling**: Process payment gateway webhooks
- **Fraud Prevention**: Payment fraud detection and prevention

---

## 🤖 Planned AI Agents

### 1. PaymentProcessingAgent

**Purpose**: Handle payment transactions securely and efficiently.

**Key Features**:
- Payment intent creation
- Payment method validation
- Transaction processing
- Payment status tracking
- Error handling and retry logic
- Payment reconciliation

**GenericAgents Integration**:
- Extends `BaseAgent` from GenericAgents.Core
- Uses `GenericAgents.Security` for payment data encryption
- Leverages secure token handling

### 2. SubscriptionBillingAgent

**Purpose**: Manage recurring subscription billing and lifecycle.

**Key Features**:
- Subscription creation and activation
- Billing cycle management
- Automatic payment collection
- Failed payment handling
- Subscription upgrades/downgrades
- Grace period management
- Cancellation processing

### 3. InvoiceGenerationAgent

**Purpose**: Generate and deliver invoices and receipts.

**Key Features**:
- Invoice creation from payments
- PDF invoice generation
- Email delivery
- Tax calculation
- Invoice archival
- Receipt generation

---

## 📊 Domain Models

### Payment

```csharp
public class Payment
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid SubscriptionId { get; set; }
    
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public PaymentMethod Method { get; set; }
    
    public string? PaymentGatewayId { get; set; } // Stripe Payment Intent ID
    public string? PaymentMethodId { get; set; } // Stripe Payment Method ID
    
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public string? FailureReason { get; private set; }
    
    // Navigation properties
    public User? User { get; set; }
    public Subscription? Subscription { get; set; }
    public Invoice? Invoice { get; set; }
    
    // Methods
    public void MarkAsProcessed();
    public void MarkAsFailed(string reason);
    public void Refund(decimal amount, string reason);
}
```

### PaymentMethod (Enum)

```csharp
public enum PaymentMethod
{
    CreditCard,
    DebitCard,
    ApplePay,
    GooglePay,
    PayPal,
    BankTransfer
}
```

### PaymentStatus (Enum)

```csharp
public enum PaymentStatus
{
    Pending,
    Processing,
    Succeeded,
    Failed,
    Refunded,
    PartiallyRefunded,
    Cancelled
}
```

### Invoice

```csharp
public class Invoice
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid SubscriptionId { get; set; }
    
    public string InvoiceNumber { get; set; } // Auto-generated
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? IssuedAt { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime DueDate { get; set; }
    
    public string? PdfUrl { get; set; }
    public string? PaymentGatewayInvoiceId { get; set; }
    
    // Navigation properties
    public User? User { get; set; }
    public Payment? Payment { get; set; }
    public Subscription? Subscription { get; set; }
    
    // Methods
    public void Issue();
    public void MarkAsPaid();
    public void Cancel();
    public string GenerateInvoiceNumber();
}
```

### InvoiceStatus (Enum)

```csharp
public enum InvoiceStatus
{
    Draft,
    Issued,
    Paid,
    Overdue,
    Cancelled,
    Refunded
}
```

### PaymentWebhookEvent

```csharp
public class PaymentWebhookEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string EventType { get; set; } // payment_intent.succeeded, etc.
    public string PaymentGatewayEventId { get; set; }
    public Dictionary<string, object> EventData { get; set; } = new();
    public bool Processed { get; private set; }
    public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; private set; }
    public string? ProcessingError { get; private set; }
    
    public void MarkAsProcessed();
    public void MarkAsFailed(string error);
}
```

---

## 🏗️ Repository Interfaces

### IPaymentRepository

```csharp
public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Payment> CreateAsync(Payment payment, CancellationToken cancellationToken = default);
    Task<Payment> UpdateAsync(Payment payment, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetPaymentsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetPaymentsBySubscriptionIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task<Payment?> GetByPaymentGatewayIdAsync(string paymentGatewayId, CancellationToken cancellationToken = default);
}
```

### IInvoiceRepository

```csharp
public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Invoice> CreateAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task<Invoice> UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task<IEnumerable<Invoice>> GetInvoicesByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync(CancellationToken cancellationToken = default);
}
```

### IPaymentWebhookRepository

```csharp
public interface IPaymentWebhookRepository
{
    Task<PaymentWebhookEvent> CreateAsync(PaymentWebhookEvent webhookEvent, CancellationToken cancellationToken = default);
    Task<PaymentWebhookEvent> UpdateAsync(PaymentWebhookEvent webhookEvent, CancellationToken cancellationToken = default);
    Task<PaymentWebhookEvent?> GetByEventIdAsync(string eventId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentWebhookEvent>> GetUnprocessedEventsAsync(CancellationToken cancellationToken = default);
}
```

---

## 💳 Payment Gateway Integration (Stripe)

### Stripe Service Interface

```csharp
public interface IStripePaymentService
{
    Task<string> CreatePaymentIntentAsync(
        decimal amount, 
        string currency, 
        Guid userId, 
        CancellationToken cancellationToken = default);
        
    Task<bool> ConfirmPaymentAsync(
        string paymentIntentId, 
        string paymentMethodId, 
        CancellationToken cancellationToken = default);
        
    Task<string> CreateSubscriptionAsync(
        Guid userId, 
        string priceId, 
        string paymentMethodId, 
        CancellationToken cancellationToken = default);
        
    Task<bool> CancelSubscriptionAsync(
        string subscriptionId, 
        CancellationToken cancellationToken = default);
        
    Task<string> CreateRefundAsync(
        string paymentIntentId, 
        decimal amount, 
        string reason, 
        CancellationToken cancellationToken = default);
        
    Task<bool> UpdatePaymentMethodAsync(
        string customerId, 
        string paymentMethodId, 
        CancellationToken cancellationToken = default);
}
```

### Webhook Handler

```csharp
public class StripeWebhookHandler
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentWebhookRepository _webhookRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ILogger<StripeWebhookHandler> _logger;
    
    public async Task<bool> HandleWebhookAsync(
        string eventJson, 
        string signature, 
        CancellationToken cancellationToken = default)
    {
        // 1. Verify webhook signature
        var stripeEvent = EventUtility.ConstructEvent(
            eventJson, 
            signature, 
            _webhookSecret);
        
        // 2. Log webhook event
        var webhookEvent = new PaymentWebhookEvent
        {
            EventType = stripeEvent.Type,
            PaymentGatewayEventId = stripeEvent.Id,
            EventData = stripeEvent.Data.Object as Dictionary<string, object>
        };
        await _webhookRepository.CreateAsync(webhookEvent, cancellationToken);
        
        // 3. Process event based on type
        var processed = stripeEvent.Type switch
        {
            "payment_intent.succeeded" => await HandlePaymentSucceededAsync(stripeEvent, cancellationToken),
            "payment_intent.payment_failed" => await HandlePaymentFailedAsync(stripeEvent, cancellationToken),
            "invoice.paid" => await HandleInvoicePaidAsync(stripeEvent, cancellationToken),
            "customer.subscription.created" => await HandleSubscriptionCreatedAsync(stripeEvent, cancellationToken),
            "customer.subscription.deleted" => await HandleSubscriptionDeletedAsync(stripeEvent, cancellationToken),
            _ => true // Unknown event type, log and continue
        };
        
        // 4. Mark as processed
        if (processed)
        {
            webhookEvent.MarkAsProcessed();
            await _webhookRepository.UpdateAsync(webhookEvent, cancellationToken);
        }
        
        return processed;
    }
}
```

---

## 🔒 Security Considerations

### PCI-DSS Compliance

- **Never store** credit card numbers, CVV, or full card details
- Use payment gateway tokens for recurring payments
- Implement TLS/SSL for all payment communications
- Use Stripe.js or similar to avoid PCI scope

### Payment Data Encryption

```csharp
public class SecurePaymentService
{
    // Only store payment method tokens, never card details
    public async Task<string> CreateSecurePaymentMethodAsync(
        string paymentMethodToken,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Store only the token from Stripe, not card details
        var paymentMethod = new StoredPaymentMethod
        {
            UserId = userId,
            PaymentGatewayMethodId = paymentMethodToken, // Token only
            Last4Digits = GetLast4Digits(paymentMethodToken), // For display
            ExpiryMonth = GetExpiryMonth(paymentMethodToken),
            ExpiryYear = GetExpiryYear(paymentMethodToken),
            CardBrand = GetCardBrand(paymentMethodToken)
        };
        
        return await _repository.CreateAsync(paymentMethod, cancellationToken);
    }
}
```

### Fraud Prevention

```csharp
public class FraudDetectionService
{
    public async Task<FraudRiskLevel> AssessFraudRiskAsync(
        Guid userId,
        decimal amount,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var riskScore = 0;
        
        // Check for unusual transaction amount
        var avgAmount = await _paymentRepository.GetAveragePaymentAmountAsync(userId);
        if (amount > avgAmount * 3)
            riskScore += 20;
        
        // Check for rapid successive payments
        var recentPayments = await _paymentRepository.GetRecentPaymentsAsync(
            userId, 
            TimeSpan.FromHours(1));
        if (recentPayments.Count() > 3)
            riskScore += 30;
        
        // Check for multiple payment method changes
        var methodChanges = await _paymentRepository.GetPaymentMethodChangesAsync(
            userId, 
            TimeSpan.FromDays(7));
        if (methodChanges.Count() > 2)
            riskScore += 25;
        
        // Return risk level based on score
        return riskScore switch
        {
            < 20 => FraudRiskLevel.Low,
            < 50 => FraudRiskLevel.Medium,
            < 75 => FraudRiskLevel.High,
            _ => FraudRiskLevel.Critical
        };
    }
}
```

---

## 🚀 Implementation Phases

### Phase 1: Payment Gateway Integration
- Set up Stripe account and API keys
- Implement PaymentProcessingAgent
- Add payment intent creation
- Handle basic payment flow

### Phase 2: Subscription Billing
- Implement SubscriptionBillingAgent
- Add recurring billing logic
- Handle subscription lifecycle
- Implement failed payment handling

### Phase 3: Invoice & Receipt Generation
- Implement InvoiceGenerationAgent
- Add PDF generation
- Email delivery integration
- Tax calculation logic

### Phase 4: Advanced Features
- Webhook processing
- Fraud detection
- Refund processing
- Payment analytics

---

## 🎯 Success Criteria

- ✅ PCI-DSS compliant payment handling
- ✅ 99.9% payment processing availability
- ✅ <2 second payment confirmation time
- ✅ Automated invoice generation and delivery
- ✅ Comprehensive payment audit trail
- ✅ Fraud detection and prevention
- ✅ >90% test coverage (using test mode)
- ✅ Webhook processing with retry logic

---

## 📚 Technology Stack

- **Stripe**: Payment processing gateway
- **Stripe.NET**: .NET SDK for Stripe
- **iTextSharp/QuestPDF**: PDF invoice generation
- **SendGrid/SMTP**: Email delivery
- **GenericAgents.Security**: Payment data encryption
- **Entity Framework Core**: Payment data persistence

---

## 📋 Testing Strategy

### Test Mode Testing

```csharp
// Use Stripe test mode for all tests
public class PaymentProcessingAgentTests
{
    // Stripe test card numbers
    private const string TestCardSuccess = "4242424242424242";
    private const string TestCardDeclined = "4000000000000002";
    private const string TestCardInsufficient = "4000000000009995";
    
    [Fact]
    public async Task ProcessPayment_WithValidCard_ShouldSucceed()
    {
        // Use test mode API key
        var agent = CreateAgentWithTestMode();
        
        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["userId"] = testUserId,
                ["amount"] = 29.99m,
                ["currency"] = "USD",
                ["paymentMethod"] = TestCardSuccess
            }
        };
        
        var result = await agent.ExecuteAsync(request);
        
        result.IsSuccess.Should().BeTrue();
    }
}
```

---

## 📚 Integration with GenericAgents

This subphase will leverage:
- **GenericAgents.Security**: For secure payment data handling
- **GenericAgents.Core**: For base agent patterns
- **GenericAgents.Configuration**: For payment gateway settings
- **GenericAgents.Observability**: For payment metrics and monitoring

---

## ⚠️ Important Notes

1. **Never log sensitive payment data** (card numbers, CVV)
2. **Always use HTTPS** for payment communications
3. **Implement idempotency** for payment operations
4. **Test thoroughly in test mode** before going live
5. **Monitor payment failures** and implement retry logic
6. **Comply with data retention policies** for financial records

---

*This is a subphase document for Phase 4 of the SoulSync Dating App*  
*To be implemented after core Phase 4 features are complete*  
*Framework: GenericAgents v1.2.0 + .NET 9 + Stripe API*
