using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FunSend.Web.Controllers;

/// <summary>
/// The personalised error pages the task asks for. UseStatusCodePagesWithReExecute
/// sends every failing status code here, and UseExceptionHandler sends
/// unhandled exceptions to /error/500.
/// </summary>
[Route("error")]
public class ErrorController : Controller
{
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(ILogger<ErrorController> logger)
    {
        _logger = logger;
    }

    [Route("{code:int}")]
    public IActionResult Status(int code)
    {
        var originalPath = HttpContext.Features
            .Get<IStatusCodeReExecuteFeature>()?.OriginalPath;

        return code switch
        {
            404 => View("NotFound", originalPath),
            403 => View("Forbidden", originalPath),
            500 => ServerError(),
            _ => View("Generic", code)
        };
    }

    private IActionResult ServerError()
    {
        var exception = HttpContext.Features
            .Get<IExceptionHandlerPathFeature>();

        if (exception is not null)
        {
            _logger.LogError(
                exception.Error,
                "Unhandled exception on {Path}.",
                exception.Path);
        }

        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View("ServerError");
    }
}
