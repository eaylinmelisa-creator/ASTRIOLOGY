using System.Globalization;
using System.Security.Claims;
using Astriology.Infrastructure.Security;

namespace Astriology.API.Extensions;

/// <summary>
/// Reads identity from the validated token.
/// </summary>
/// <remarks>
/// Every ownership decision must start here, never from an id in the request body or
/// query string: the token is signed, the request body is whatever the caller typed.
/// </remarks>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The authenticated user's id.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the claim is missing or unparsable. That cannot happen behind
    /// [Authorize] with a validated token, so it signals a configuration mistake -
    /// most often inbound claim mapping being left on.
    /// </exception>
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var value = principal.FindFirstValue(TokenClaimNames.Subject);

        if (!int.TryParse(value, CultureInfo.InvariantCulture, out var userId))
        {
            throw new InvalidOperationException(
                $"The '{TokenClaimNames.Subject}' claim is missing or not an integer. "
                + "Check that MapInboundClaims is disabled on the JWT bearer options.");
        }

        return userId;
    }
}
