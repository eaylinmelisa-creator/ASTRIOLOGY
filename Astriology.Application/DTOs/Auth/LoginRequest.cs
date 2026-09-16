namespace Astriology.Application.DTOs.Auth;

/// <param name="UserName">Sign-in is by user name, not email.</param>
public sealed record LoginRequest(string UserName, string Password);
