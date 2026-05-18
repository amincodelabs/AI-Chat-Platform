using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using PrivateAiChat.Contracts.Auth;
using PrivateAiChat.Domain.Users;
using PrivateAiChat.Infrastructure.DependencyInjection;
using PrivateAiChat.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/auth/signup", async (
    SignupRequest request,
    UserManager<User> userManager,
    SignInManager<User> signInManager) =>
{
    if (!TryValidate(request, out var validationErrors))
    {
        return Results.ValidationProblem(validationErrors);
    }

    var existingUser = await userManager.FindByEmailAsync(request.Email.Trim());
    if (existingUser is not null)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(SignupRequest.Email)] = ["Email is already registered."]
        });
    }

    var user = new User(request.Email, request.DisplayName);
    var result = await userManager.CreateAsync(user, request.Password);

    if (!result.Succeeded)
    {
        return Results.ValidationProblem(ToValidationErrors(result));
    }

    await signInManager.SignInAsync(user, isPersistent: true);

    return Results.Ok(ToAuthResponse(user));
})
.AllowAnonymous();

app.MapPost("/auth/login", async (
    LoginRequest request,
    UserManager<User> userManager,
    SignInManager<User> signInManager) =>
{
    if (!TryValidate(request, out var validationErrors))
    {
        return Results.ValidationProblem(validationErrors);
    }

    var user = await userManager.FindByEmailAsync(request.Email.Trim());
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var result = await signInManager.PasswordSignInAsync(
        user,
        request.Password,
        isPersistent: true,
        lockoutOnFailure: true);

    return result.Succeeded
        ? Results.Ok(ToAuthResponse(user))
        : Results.Unauthorized();
})
.AllowAnonymous();

app.MapPost("/auth/logout", async (SignInManager<User> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.NoContent();
})
.RequireAuthorization();

app.MapGet("/health", async (AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var databaseConnected = await dbContext.Database.CanConnectAsync(cancellationToken);

    return Results.Ok(new
    {
        status = databaseConnected ? "Healthy" : "Degraded",
        database = databaseConnected ? "Connected" : "Unavailable"
    });
});

app.Run();

static AuthResponse ToAuthResponse(User user) =>
    new(
        user.Id,
        user.Email ?? string.Empty,
        user.DisplayName);

static bool TryValidate<TRequest>(
    TRequest request,
    out Dictionary<string, string[]> validationErrors)
{
    var results = new List<ValidationResult>();
    var context = new ValidationContext(request!);

    var isValid = Validator.TryValidateObject(request!, context, results, validateAllProperties: true);
    validationErrors = results
        .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty)
            .Select(memberName => new { memberName, result.ErrorMessage }))
        .GroupBy(error => error.memberName)
        .ToDictionary(
            group => group.Key,
            group => group
                .Select(error => error.ErrorMessage ?? "The request is invalid.")
                .ToArray());

    return isValid;
}

static Dictionary<string, string[]> ToValidationErrors(IdentityResult result) =>
    result.Errors
        .GroupBy(error => error.Code)
        .ToDictionary(
            group => group.Key,
            group => group.Select(error => error.Description).ToArray());
