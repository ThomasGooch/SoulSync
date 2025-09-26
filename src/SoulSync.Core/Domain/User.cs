using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SoulSync.Core.Domain;

public class User
{
    private string _email = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private DateOnly _dateOfBirth;

    public Guid Id { get; init; } = Guid.NewGuid();

    public string Email
    {
        get => _email;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Email cannot be empty", nameof(Email));

            if (!IsValidEmail(value))
                throw new ArgumentException("Email format is invalid", nameof(Email));

            _email = value;
        }
    }

    public string FirstName
    {
        get => _firstName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("First name cannot be empty", nameof(FirstName));

            _firstName = value;
            LastModifiedAt = DateTime.UtcNow;
        }
    }

    public string LastName
    {
        get => _lastName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Last name cannot be empty", nameof(LastName));

            _lastName = value;
            LastModifiedAt = DateTime.UtcNow;
        }
    }

    public DateOnly DateOfBirth
    {
        get => _dateOfBirth;
        set
        {
            if (value > DateOnly.FromDateTime(DateTime.Now))
                throw new ArgumentException("Date of birth cannot be in the future", nameof(DateOfBirth));

            var age = DateTime.Now.Year - value.Year;
            if (DateTime.Now < value.ToDateTime(TimeOnly.MinValue).AddYears(age))
                age--;

            if (age < 18)
                throw new ArgumentException("User must be at least 18 years old", nameof(DateOfBirth));

            _dateOfBirth = value;
        }
    }

    public string? Bio { get; set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime LastModifiedAt { get; private set; } = DateTime.UtcNow;

    // Computed properties
    public string FullName => $"{FirstName} {LastName}";
    
    public int Age
    {
        get
        {
            var age = DateTime.Now.Year - DateOfBirth.Year;
            if (DateTime.Now < DateOfBirth.ToDateTime(TimeOnly.MinValue).AddYears(age))
                age--;
            return age;
        }
    }

    // Navigation properties
    public UserProfile? Profile { get; set; }

    // Methods
    public void UpdateProfile(string firstName, string lastName, string? bio = null)
    {
        FirstName = firstName;
        LastName = lastName;
        
        if (bio != null)
            Bio = bio;
            
        LastModifiedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        LastModifiedAt = DateTime.UtcNow;
    }

    private static bool IsValidEmail(string email)
    {
        var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, emailPattern);
    }
}