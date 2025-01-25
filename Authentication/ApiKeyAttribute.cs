using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LLMWebAPI.Authentication;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAttribute : Attribute, IAuthorizationFilter
{
    private const string ApiKeyHeaderName = "X-API-KEY";
    
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var apiKey = context.HttpContext.Request.Headers[ApiKeyHeaderName].FirstOrDefault();
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var validApiKey = configuration["Authentication:ApiKey"];

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(validApiKey) || apiKey != validApiKey)
        {
            context.Result = new UnauthorizedObjectResult("Invalid API Key");
        }
    }
}
