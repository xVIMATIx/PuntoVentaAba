using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
namespace PuntoAba.Api.Filters;

public class ErroresApiFilter(ILogger<ErroresApiFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var ex = context.Exception;
        var status = ex switch
        {
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            ArgumentException or ValidationException => StatusCodes.Status400BadRequest,
            DbUpdateConcurrencyException or DbUpdateException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
        if (status == 500) logger.LogError(ex, "Error en API {Ruta}", context.HttpContext.Request.Path);
        var problem = new ProblemDetails
        {
            Status = status,
            Title = status switch { 400 => "Datos inválidos", 403 => "Operación no permitida", 409 => "Conflicto de datos", _ => "No se pudo completar la operación" },
            Detail = status is 400 or 403 ? ex.Message : status == 409 ? "Los datos cambiaron o ya existe el registro. Actualiza y revisa antes de reintentar." : "Inténtalo nuevamente.",
            Instance = context.HttpContext.Request.Path
        };
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        context.Result = new ObjectResult(problem) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
