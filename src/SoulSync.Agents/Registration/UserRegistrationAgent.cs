using Microsoft.Extensions.Logging;
using SoulSync.Core.Agents;
using SoulSync.Core.Domain;
using SoulSync.Core.Enums;
using SoulSync.Core.Interfaces;
using System.Globalization;

namespace SoulSync.Agents.Registration;

public class UserRegistrationAgent : BaseAgent
{
    private readonly IAIService _aiService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserRegistrationAgent> _logger;

    public UserRegistrationAgent(
        IAIService aiService,
        IUserRepository userRepository,
        ILogger<UserRegistrationAgent> logger)
    {
        _aiService = aiService;
        _userRepository = userRepository;
        _logger = logger;
    }

    protected override async Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting user registration process");

            // Validate and extract required parameters
            var validationResult = ValidateAndExtractParameters(request.Parameters);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed: {Error}", validationResult.ErrorMessage);
                return AgentResult.CreateError(validationResult.ErrorMessage);
            }

            var (email, firstName, lastName, dateOfBirth) = validationResult.RequiredData;

            // Check if user already exists
            if (await _userRepository.ExistsAsync(email, cancellationToken))
            {
                _logger.LogWarning("Registration failed: User with email {Email} already exists", email);
                return AgentResult.CreateError($"User with email {email} already exists");
            }

            // Create User entity
            var user = new User
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateOfBirth,
                Bio = request.Parameters.TryGetValue("bio", out var bioValue) ? bioValue?.ToString() : null
            };

            // Create UserProfile if profile data is provided
            var profileData = ExtractProfileData(request.Parameters);
            if (profileData.HasProfileData)
            {
                user.Profile = CreateUserProfile(user.Id, profileData);

                // Generate AI insights if we have meaningful content to analyze
                var aiInsights = await GenerateAIInsights(user, profileData, cancellationToken);
                if (!string.IsNullOrEmpty(aiInsights))
                {
                    user.Profile.AddAIInsights(aiInsights);
                }
            }

            // Save user to database
            var createdUser = await _userRepository.CreateAsync(user, cancellationToken);

            _logger.LogInformation("User registered successfully: {UserId} with email {Email}", 
                createdUser.Id, createdUser.Email);

            // Return success response
            return AgentResult.CreateSuccess(new Dictionary<string, object>
            {
                ["userId"] = createdUser.Id,
                ["email"] = createdUser.Email,
                ["fullName"] = createdUser.FullName,
                ["aiInsights"] = createdUser.Profile?.AIInsights ?? "Profile analysis will be completed shortly.",
                ["hasProfile"] = createdUser.Profile != null
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Validation error during user registration");
            return AgentResult.CreateError($"Validation error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during user registration");
            return AgentResult.CreateError("An unexpected error occurred during registration");
        }
    }

    private static ValidationResult ValidateAndExtractParameters(Dictionary<string, object> parameters)
    {
        // Validate required fields
        if (!parameters.TryGetValue("email", out var emailValue) || string.IsNullOrWhiteSpace(emailValue?.ToString()))
            return ValidationResult.Invalid("Email is required");

        if (!parameters.TryGetValue("firstName", out var firstNameValue) || string.IsNullOrWhiteSpace(firstNameValue?.ToString()))
            return ValidationResult.Invalid("First name is required");

        if (!parameters.TryGetValue("lastName", out var lastNameValue) || string.IsNullOrWhiteSpace(lastNameValue?.ToString()))
            return ValidationResult.Invalid("Last name is required");

        if (!parameters.TryGetValue("dateOfBirth", out var dobValue) || string.IsNullOrWhiteSpace(dobValue?.ToString()))
            return ValidationResult.Invalid("Date of birth is required");

        var email = emailValue!.ToString()!;
        var firstName = firstNameValue!.ToString()!;
        var lastName = lastNameValue!.ToString()!;

        // Parse and validate date of birth
        if (!DateOnly.TryParse(dobValue!.ToString(), CultureInfo.InvariantCulture, out var dateOfBirth))
            return ValidationResult.Invalid("Invalid date of birth format");

        try
        {
            // This will validate age constraints through the User constructor
            var testUser = new User
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateOfBirth
            };
        }
        catch (ArgumentException ex)
        {
            return ValidationResult.Invalid(ex.Message);
        }

        return ValidationResult.Valid((email, firstName, lastName, dateOfBirth));
    }

    private static ProfileData ExtractProfileData(Dictionary<string, object> parameters)
    {
        var profileData = new ProfileData();

        if (parameters.TryGetValue("genderIdentity", out var genderValue) && 
            Enum.TryParse<GenderIdentity>(genderValue?.ToString(), true, out var gender))
        {
            profileData.GenderIdentity = gender;
        }

        if (parameters.TryGetValue("interestedInGenders", out var interestedValue) && interestedValue is List<string> interestedList)
        {
            profileData.InterestedInGenders = interestedList
                .Select(g => Enum.TryParse<GenderIdentity>(g, true, out var parsed) ? parsed : (GenderIdentity?)null)
                .Where(g => g.HasValue)
                .Select(g => g!.Value)
                .ToList();
        }

        profileData.Interests = parameters.TryGetValue("interests", out var interestsValue) ? interestsValue?.ToString() : null;
        profileData.Location = parameters.TryGetValue("location", out var locationValue) ? locationValue?.ToString() : null;
        profileData.Occupation = parameters.TryGetValue("occupation", out var occupationValue) ? occupationValue?.ToString() : null;

        if (parameters.TryGetValue("minAge", out var minAgeValue) && int.TryParse(minAgeValue?.ToString(), out var minAge))
            profileData.MinAge = minAge;

        if (parameters.TryGetValue("maxAge", out var maxAgeValue) && int.TryParse(maxAgeValue?.ToString(), out var maxAge))
            profileData.MaxAge = maxAge;

        if (parameters.TryGetValue("maxDistanceKm", out var maxDistValue) && int.TryParse(maxDistValue?.ToString(), out var maxDist))
            profileData.MaxDistanceKm = maxDist;

        return profileData;
    }

    private static UserProfile CreateUserProfile(Guid userId, ProfileData profileData)
    {
        var profile = new UserProfile
        {
            UserId = userId,
            Interests = profileData.Interests,
            Location = profileData.Location,
            Occupation = profileData.Occupation,
            MinAge = profileData.MinAge,
            MaxAge = profileData.MaxAge,
            MaxDistanceKm = profileData.MaxDistanceKm
        };

        if (profileData.GenderIdentity.HasValue)
            profile.GenderIdentity = profileData.GenderIdentity.Value;

        if (profileData.InterestedInGenders.Any())
            profile.InterestedInGenders = profileData.InterestedInGenders;

        return profile;
    }

    private async Task<string?> GenerateAIInsights(User user, ProfileData profileData, CancellationToken cancellationToken)
    {
        try
        {
            // Only analyze if we have meaningful content
            var contentToAnalyze = new List<string>();
            
            if (!string.IsNullOrEmpty(user.Bio))
                contentToAnalyze.Add($"Bio: {user.Bio}");
                
            if (!string.IsNullOrEmpty(profileData.Interests))
                contentToAnalyze.Add($"Interests: {profileData.Interests}");
                
            if (!string.IsNullOrEmpty(profileData.Occupation))
                contentToAnalyze.Add($"Occupation: {profileData.Occupation}");

            if (!contentToAnalyze.Any())
                return null;

            var profileText = string.Join(". ", contentToAnalyze);
            var insights = await _aiService.AnalyzeProfileAsync(profileText, cancellationToken);
            
            _logger.LogInformation("AI insights generated for user {UserId}", user.Id);
            return insights;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate AI insights for user {UserId}", user.Id);
            return null; // Continue without AI insights
        }
    }

    private class ValidationResult
    {
        public bool IsValid { get; private set; }
        public string? ErrorMessage { get; private set; }
        public (string Email, string FirstName, string LastName, DateOnly DateOfBirth) RequiredData { get; private set; }

        public static ValidationResult Valid((string, string, string, DateOnly) data)
        {
            return new ValidationResult
            {
                IsValid = true,
                RequiredData = data
            };
        }

        public static ValidationResult Invalid(string errorMessage)
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = errorMessage
            };
        }
    }

    private class ProfileData
    {
        public GenderIdentity? GenderIdentity { get; set; }
        public List<GenderIdentity> InterestedInGenders { get; set; } = new();
        public string? Interests { get; set; }
        public string? Location { get; set; }
        public string? Occupation { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public int? MaxDistanceKm { get; set; }

        public bool HasProfileData => 
            GenderIdentity.HasValue ||
            InterestedInGenders.Any() ||
            !string.IsNullOrEmpty(Interests) ||
            !string.IsNullOrEmpty(Location) ||
            !string.IsNullOrEmpty(Occupation) ||
            MinAge.HasValue ||
            MaxAge.HasValue ||
            MaxDistanceKm.HasValue;
    }
}