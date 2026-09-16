namespace Astriology.Application.DTOs.Auth;

/// <summary>
/// Changing a password always requires the current one. There is no reset-by-email
/// path; that is out of scope (roadmap section 5.8).
/// </summary>
public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);
