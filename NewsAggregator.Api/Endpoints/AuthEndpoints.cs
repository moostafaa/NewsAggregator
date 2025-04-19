using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using NewsAggregator.Domain.Auth.Services;
using NewsAggregator.Domain.Auth.Models;

namespace NewsAggregator.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .WithOpenApi();

        group.MapGet("/login/{provider}", (string provider, IAuthService authService) =>
        {
            var properties = authService.ConfigureExternalAuthenticationProperties(provider, "/api/auth/callback");
            return Results.Challenge(new AuthenticationProperties(properties), new[] { provider });
        })
        .WithName("ExternalLogin")
        .AllowAnonymous();

        group.MapGet("/callback", async Task<Results<Ok<AuthResult>, BadRequest<ErrorResponse>>> (
            IAuthService authService) =>
        {
            try
            {
                var result = await authService.HandleExternalLoginCallback();
                return TypedResults.Ok(result);
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest(new ErrorResponse(ex.Message));
            }
        })
        .WithName("ExternalLoginCallback")
        .AllowAnonymous();

        group.MapPost("/logout", async Task<Results<Ok<MessageResponse>, BadRequest<ErrorResponse>>> (
            HttpContext context) =>
        {
            try
            {
                await context.SignOutAsync();
                return TypedResults.Ok(new MessageResponse("Successfully logged out"));
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest(new ErrorResponse(ex.Message));
            }
        })
        .WithName("Logout")
        .RequireAuthorization();

        group.MapGet("/me", async Task<Results<Ok<UserInfo>, BadRequest<ErrorResponse>>> (
            IAuthService authService) =>
        {
            try
            {
                var user = await authService.GetCurrentUserInfo();
                return TypedResults.Ok(user);
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest(new ErrorResponse(ex.Message));
            }
        })
        .WithName("GetCurrentUser")
        .RequireAuthorization();

        group.MapPost("/roles/{userId}", async Task<Results<Ok<MessageResponse>, BadRequest<ErrorResponse>>> (
            IAuthService authService,
            string userId,
            RoleAssignmentModel model) =>
        {
            try
            {
                await authService.AssignRoleToUser(userId, model.Role);
                return TypedResults.Ok(new MessageResponse("Role assigned successfully"));
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest(new ErrorResponse(ex.Message));
            }
        })
        .WithName("AssignRole")
        .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

        return app;
    }
}

public record MessageResponse(string Message); 