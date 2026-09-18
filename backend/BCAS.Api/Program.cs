using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCAS.Api.Helpers;
using BCAS.Api.Models;
using BCAS.Api.Options;
using BCAS.Api.Repositories;
using BCAS.Api.Repositories.Utilities;
using BCAS.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration -----------------------------------------------------------
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<LoginOptions>(builder.Configuration.GetSection(LoginOptions.SectionName));
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.SectionName));
builder.Services.Configure<PasswordResetOptions>(builder.Configuration.GetSection(PasswordResetOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("The 'Jwt' configuration section is missing.");

if (string.IsNullOrWhiteSpace(jwt.SigningKey))
{
    throw new InvalidOperationException(
        "Jwt:SigningKey is not configured. Run 'dotnet user-secrets set \"Jwt:SigningKey\" \"<key>\"' " +
        "for local development, or set it in the deployment secret store.");
}

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
var emailEnabled = builder.Configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>()?.Enabled ?? false;

// --- Services ----------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddProblemDetails();

builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IActivityLogger, ActivityLogger>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
builder.Services.AddScoped<ITokenRevocationStore, TokenRevocationStore>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IAccountService, AccountService>();

// Without SMTP configured, reset emails go to the application log so the link
// can still be followed during development.
if (emailEnabled)
{
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
}
else
{
    builder.Services.AddSingleton<IEmailSender, LoggingEmailSender>();
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep the JWT's own claim names ("sub", "role", "jti") instead of the
        // legacy SOAP URIs, so what the token carries is what the code reads.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = "role",
        };

        options.Events = new JwtBearerEvents
        {
            // BW-11 and BW-15: a token that has been logged out, or whose account
            // has since been deactivated, is rejected on the next request rather
            // than staying good until it expires.
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                var jti = principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
                var rawUserId = principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);

                if (string.IsNullOrEmpty(jti)
                    || !int.TryParse(rawUserId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId))
                {
                    context.Fail("Token is missing the claims needed to validate it.");
                    return;
                }

                var store = context.HttpContext.RequestServices.GetRequiredService<ITokenRevocationStore>();
                var status = await store
                    .GetStatusAsync(jti, userId, context.HttpContext.RequestAborted)
                    .ConfigureAwait(false);

                if (status != AccessTokenStatus.Accepted)
                {
                    context.Fail(status == AccessTokenStatus.Revoked
                        ? "Token has been revoked."
                        : "The account behind this token is deactivated.");
                }
            },
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "BCAS Web API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the access token returned by POST /api/auth/login.",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            Array.Empty<string>()
        },
    });
});

var app = builder.Build();

// --- Pipeline ----------------------------------------------------------------
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors();

// BW-11: keep API responses out of the browser's back/forward cache, so pressing
// Back after signing out cannot redisplay admin data from a cached response.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Headers[HeaderNames.CacheControl] = "no-store, no-cache, must-revalidate";
        context.Response.Headers[HeaderNames.Pragma] = "no-cache";
    }

    await next(context).ConfigureAwait(false);
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.Run();
