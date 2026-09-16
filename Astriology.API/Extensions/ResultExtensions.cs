using Astriology.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Astriology.API.Extensions;

/// <summary>
/// Maps service results onto HTTP responses, so no controller decides status codes
/// on its own and the mapping stays consistent across the API.
/// </summary>
public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess
            ? new NoContentResult()
            : Problem(result);
    }

    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess
            ? new OkObjectResult(result.Value)
            : Problem(result);
    }

    /// <summary>Success returns 201 with a Location header pointing at the new resource.</summary>
    public static IActionResult ToCreatedResult<T>(this Result<T> result, string location)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess
            ? new CreatedResult(location, result.Value)
            : Problem(result);
    }

    private static IActionResult Problem(Result result)
    {
        var statusCode = result.ErrorKind switch
        {
            ResultError.Validation => StatusCodes.Status400BadRequest,
            ResultError.Unauthorized => StatusCodes.Status401Unauthorized,
            ResultError.Forbidden => StatusCodes.Status403Forbidden,
            ResultError.NotFound => StatusCodes.Status404NotFound,
            ResultError.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = TitleFor(result.ErrorKind),
            Detail = result.Error,
        };

        // Machine-readable, so a client branches on this rather than on the Turkish
        // text in Detail.
        problem.Extensions["errorCode"] = result.ErrorKind.ToString();

        return new ObjectResult(problem) { StatusCode = statusCode };
    }

    private static string TitleFor(ResultError kind) => kind switch
    {
        ResultError.Validation => "Geçersiz istek",
        ResultError.Unauthorized => "Kimlik doğrulanamadı",
        ResultError.Forbidden => "Bu işlem için yetkiniz yok",
        ResultError.NotFound => "Kayıt bulunamadı",
        ResultError.Conflict => "İşlem mevcut durumla çelişiyor",
        _ => "Beklenmeyen hata",
    };
}
