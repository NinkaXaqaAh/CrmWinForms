using CrmApp.Core.Abstractions;
using CrmApp.Core.Enums;

namespace CrmApp.Core.Models;

// Учётная запись пользователя приложения.
// PasswordHash — формат "<iterations>.<base64salt>.<base64hash>" (см. AuthService).
public sealed class User : IEntity, IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Login { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Viewer;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
