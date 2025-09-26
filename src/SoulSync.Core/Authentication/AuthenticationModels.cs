using SoulSync.Core.Domain;
using System.Security.Claims;

namespace SoulSync.Core.Authentication;

public class AuthenticationRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthenticationResult
{
    public bool IsSuccess { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public User? User { get; set; }
    public string? ErrorMessage { get; set; }

    public static AuthenticationResult Success(string token, string refreshToken, DateTime expiresAt, User user)
    {
        return new AuthenticationResult
        {
            IsSuccess = true,
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = user
        };
    }

    public static AuthenticationResult Failure(string errorMessage)
    {
        return new AuthenticationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}

public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public Guid UserId { get; set; }
    public string? Email { get; set; }
    public List<Claim> Claims { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public static TokenValidationResult Valid(Guid userId, string email, List<Claim> claims)
    {
        return new TokenValidationResult
        {
            IsValid = true,
            UserId = userId,
            Email = email,
            Claims = claims
        };
    }

    public static TokenValidationResult Invalid(string errorMessage)
    {
        return new TokenValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage
        };
    }
}

public class RefreshToken
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}