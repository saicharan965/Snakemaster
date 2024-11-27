using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;

public class Auth0Middleware
{
    private readonly RequestDelegate _next;

    public Auth0Middleware(RequestDelegate next)
    {
        _next = next; // RequestDelegate is passed automatically by ASP.NET Core
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var auth0Identifier = context.User?.FindFirst("sub")?.Value; // Extract "sub" (Auth0 Identifier)

        if (!string.IsNullOrEmpty(auth0Identifier))
        {
            context.Items["Auth0Identifier"] = auth0Identifier; // Store in context for later use
        }

        await _next(context); // Pass to the next middleware
    }
}
