using api.Interfaces;
using api.Models;
using api.Validation;
using Carter;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace api.Modules
{
    public class AccountModule : CarterModule
    {
        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/account/register", async ([FromBody] RegisterModel model, UserManager<ApplicationUser> userManager) =>
            {
                try
                {
                    var validationErrors = ModelValidation.ValidateRegisterModel(model);
                    if (validationErrors.Count > 0)
                    {
                        return Results.BadRequest(new { errors = validationErrors });
                    }

                    var user = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        FullName = model.FullName
                    };

                    var result = await userManager.CreateAsync(user, model.Password);

                    if (result.Succeeded)
                    {
                        return Results.Ok(new { message = "Registration successful" });
                    }

                    return Results.BadRequest(new { errors = result.Errors });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        detail: "An error occurred during registration.",
                        statusCode: 500);
                }
            })
            .WithName("Register")
            .WithTags("Account");

            app.MapPost("/account/login", async ([FromBody] LoginModel model, 
                UserManager<ApplicationUser> userManager,
                ITokenService tokenService) =>
            {
                try
                {
                    var validationErrors = ModelValidation.ValidateLoginModel(model);
                    if (validationErrors.Count > 0)
                    {
                        return Results.BadRequest(new { errors = validationErrors });
                    }

                    var user = await userManager.FindByEmailAsync(model.Email);
                    if (user == null)
                    {
                        return Results.BadRequest(new { message = "Invalid login attempt" });
                    }

                    var isPasswordValid = await userManager.CheckPasswordAsync(user, model.Password);
                    if (!isPasswordValid)
                    {
                        return Results.BadRequest(new { message = "Invalid login attempt" });
                    }

                    if (await userManager.IsLockedOutAsync(user))
                    {
                        return Results.BadRequest(new { message = "User account locked out" });
                    }

                    var tokens = await tokenService.GenerateTokensAsync(user);
                    
                    return Results.Ok(tokens);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        detail: "An error occurred during login.",
                        statusCode: 500);
                }
            })
            .WithName("Login")
            .WithTags("Account");

            app.MapPost("/account/refresh-token", async ([FromBody] RefreshTokenRequest model, ITokenService tokenService) =>
            {
                try
                {
                    var tokens = await tokenService.RefreshTokenAsync(model.RefreshToken);
                    return Results.Ok(tokens);
                }
                catch (SecurityTokenException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        detail: "An error occurred while refreshing the token.",
                        statusCode: 500);
                }
            })
            .WithName("RefreshToken")
            .WithTags("Account");

            app.MapPost("/account/revoke-token", async ([FromBody] RevokeTokenRequest model, ITokenService tokenService) =>
            {
                try
                {
                    await tokenService.RevokeRefreshTokenAsync(model.RefreshToken);
                    return Results.Ok(new { message = "Token revoked successfully" });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        detail: "An error occurred while revoking the token.",
                        statusCode: 500);
                }
            })
            .WithName("RevokeToken")
            .WithTags("Account")
            .RequireAuthorization();
        }
    }
}
