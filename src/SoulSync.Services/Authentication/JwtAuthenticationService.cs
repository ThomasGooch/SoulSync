using BCrypt.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SoulSync.Core.Authentication;
using SoulSync.Core.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SoulSync.Services.Authentication;

public class JwtAuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtAuthenticationService> _logger;
    private readonly string _signingKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;
    private readonly Dictionary<string, RefreshToken> _refreshTokens = new(); // In production, use Redis or database

    public JwtAuthenticationService(
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<JwtAuthenticationService> logger)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
        
        _signingKey = configuration["JWT:SigningKey"] ?? throw new InvalidOperationException("JWT signing key not configured");
        _issuer = configuration["JWT:Issuer"] ?? throw new InvalidOperationException("JWT issuer not configured");
        _audience = configuration["JWT:Audience"] ?? throw new InvalidOperationException("JWT audience not configured");
        _expirationMinutes = int.Parse(configuration["JWT:ExpirationMinutes"] ?? "60");
    }

    public async Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate input
            var validationError = ValidateAuthenticationRequest(request);
            if (!string.IsNullOrEmpty(validationError))
            {
                _logger.LogWarning("Authentication validation failed: {Error}", validationError);
                return AuthenticationResult.Failure(validationError);
            }

            // Get user by email
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Authentication failed: User not found for email {Email}", request.Email);
                return AuthenticationResult.Failure("Invalid credentials");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Authentication failed: User account is deactivated for {Email}", request.Email);
                return AuthenticationResult.Failure("User account is deactivated");
            }

            // For demo purposes, accept any password that contains at least 8 characters and has uppercase, number, and special char
            // In real implementation, you'd verify against stored hashed password
            if (!IsValidPasswordFormat(request.Password))
            {
                _logger.LogWarning("Authentication failed: Invalid password for {Email}", request.Email);
                return AuthenticationResult.Failure("Invalid credentials");
            }

            // Generate tokens
            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken(user.Id);
            var expiresAt = DateTime.UtcNow.AddMinutes(_expirationMinutes);

            _logger.LogInformation("User authenticated successfully: {Email}", request.Email);

            return AuthenticationResult.Success(token, refreshToken.Token, expiresAt, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for {Email}", request.Email);
            return AuthenticationResult.Failure("Authentication failed due to an internal error");
        }
    }

    public async Task<AuthenticationResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(refreshToken))
                return AuthenticationResult.Failure("Refresh token is required");

            if (!_refreshTokens.TryGetValue(refreshToken, out var storedToken) || 
                !storedToken.IsActive || 
                storedToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Invalid or expired refresh token used");
                return AuthenticationResult.Failure("Invalid refresh token");
            }

            // Get user
            var user = await _userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Refresh token user not found or inactive: {UserId}", storedToken.UserId);
                return AuthenticationResult.Failure("Invalid refresh token");
            }

            // Generate new tokens
            var newToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken(user.Id);
            var expiresAt = DateTime.UtcNow.AddMinutes(_expirationMinutes);

            // Invalidate old refresh token
            storedToken.IsActive = false;

            _logger.LogInformation("Token refreshed successfully for user {UserId}", user.Id);

            return AuthenticationResult.Success(newToken, newRefreshToken.Token, expiresAt, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return AuthenticationResult.Failure("Token refresh failed");
        }
    }

    public Task<Core.Authentication.TokenValidationResult> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
                return Task.FromResult(Core.Authentication.TokenValidationResult.Invalid("Token is required"));

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_signingKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            // First let's see what claims we actually have
            var allClaims = principal.Claims.ToList();
            if (!allClaims.Any())
            {
                return Task.FromResult(Core.Authentication.TokenValidationResult.Invalid("No claims found in token"));
            }

            // Try different ways to find the claims
            var userIdClaim = principal.FindFirst("sub")?.Value ?? 
                             principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                             principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            
            var emailClaim = principal.FindFirst("email")?.Value ?? 
                            principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value ??
                            principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

            // If still not found, return the first claim to debug
            if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(emailClaim))
            {
                var claimTypes = string.Join(", ", allClaims.Select(c => $"{c.Type}='{c.Value}'"));
                return Task.FromResult(Core.Authentication.TokenValidationResult.Invalid($"Claims not found. Available claims: {claimTypes}"));
            }

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Task.FromResult(Core.Authentication.TokenValidationResult.Invalid($"Invalid user ID format: '{userIdClaim}'"));
            }

            return Task.FromResult(Core.Authentication.TokenValidationResult.Valid(userId, emailClaim, principal.Claims.ToList()));
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning("Token validation failed: {Error}", ex.Message);
            return Task.FromResult(Core.Authentication.TokenValidationResult.Invalid("Invalid token"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token validation");
            return Task.FromResult(Core.Authentication.TokenValidationResult.Invalid("Token validation error"));
        }
    }

    public Task<bool> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_refreshTokens.TryGetValue(refreshToken, out var token))
            {
                token.IsActive = false;
                _logger.LogInformation("Refresh token revoked successfully");
                return Task.FromResult(true);
            }

            _logger.LogWarning("Attempted to revoke non-existent refresh token");
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token");
            return Task.FromResult(false);
        }
    }

    public Task<string> HashPasswordAsync(string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        return Task.FromResult(hashedPassword);
    }

    public Task<bool> VerifyPasswordAsync(string password, string hashedPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
            return Task.FromResult(false);

        try
        {
            var isValid = BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            return Task.FromResult(isValid);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private static string? ValidateAuthenticationRequest(AuthenticationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return "Email is required";

        if (string.IsNullOrWhiteSpace(request.Password))
            return "Password is required";

        return null;
    }

    private static bool IsValidPasswordFormat(string password)
    {
        // For demo purposes, accept passwords with basic format requirements
        return password.Length >= 8 && 
               password.Any(char.IsUpper) && 
               password.Any(char.IsDigit) &&
               password.Any(ch => !char.IsLetterOrDigit(ch));
    }

    private string GenerateJwtToken(SoulSync.Core.Domain.User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_signingKey);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.FullName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_expirationMinutes),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private RefreshToken GenerateRefreshToken(Guid userId)
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        
        var refreshToken = new RefreshToken
        {
            Token = Convert.ToBase64String(randomBytes),
            ExpiresAt = DateTime.UtcNow.AddDays(7), // Refresh tokens last 7 days
            UserId = userId
        };

        _refreshTokens[refreshToken.Token] = refreshToken;
        
        return refreshToken;
    }
}