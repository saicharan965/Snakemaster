using System.IdentityModel.Tokens.Jwt;

public class Auth0Middleware
{
    private readonly RequestDelegate _next;

    public Auth0Middleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();

                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    var jwtToken = handler.ReadJwtToken(token);

                    var auth0Identifier = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

                    if (!string.IsNullOrEmpty(auth0Identifier))
                    {
                        context.Items["Auth0Identifier"] = auth0Identifier;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing JWT token: {ex.Message}");
        }

        await _next(context);
    }
}
