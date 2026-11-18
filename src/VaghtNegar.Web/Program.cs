using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using VaghtNegar.Web.Components;
using VaghtNegar.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var keycloakAuthority = builder.Configuration["Keycloak:Authority"]
    ?? "http://localhost:8080/realms/vaghtnegar";
var keycloakClientId = builder.Configuration["Keycloak:ClientId"] ?? "vaghtnegar-web";
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7180/";

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("VaghtNegarApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<VaghtNegarApiClient>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "__Host-VaghtNegar.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.LoginPath = "/account/login";
        options.AccessDeniedPath = "/access-denied";
    })
    .AddOpenIdConnect(options =>
    {
        options.Authority = keycloakAuthority;
        options.ClientId = keycloakClientId;
        options.ResponseType = "code";
        options.UsePkce = true;
        // Keycloak local development can reject PAR requests before processing the
        // registered redirect URI. Keep PKCE enabled and bypass PAR in Development.
        options.PushedAuthorizationBehavior = builder.Environment.IsDevelopment()
            ? PushedAuthorizationBehavior.Disable
            : PushedAuthorizationBehavior.UseIfAvailable;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.CallbackPath = "/signin-oidc";
        options.SignedOutCallbackPath = "/signout-callback-oidc";
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "preferred_username",
            RoleClaimType = ClaimTypes.Role,
            ValidateIssuer = true
        };
        options.Events = new OpenIdConnectEvents
        {
            OnTokenValidated = context =>
            {
                if (context.Principal?.Identity is not ClaimsIdentity identity)
                    return Task.CompletedTask;

                var realmAccess = context.Principal.FindFirst("realm_access")?.Value;
                if (string.IsNullOrWhiteSpace(realmAccess))
                    return Task.CompletedTask;

                try
                {
                    using var document = JsonDocument.Parse(realmAccess);
                    if (document.RootElement.TryGetProperty("roles", out var roles) &&
                        roles.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var role in roles.EnumerateArray())
                        {
                            if (role.ValueKind != JsonValueKind.String) continue;
                            var value = role.GetString();
                            if (!string.IsNullOrWhiteSpace(value) &&
                                !identity.HasClaim(ClaimTypes.Role, value))
                            {
                                identity.AddClaim(new Claim(ClaimTypes.Role, value));
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    context.Fail("Keycloak realm roles claim is invalid.");
                }

                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/account/login", (string? returnUrl) =>
    Results.Challenge(
        new AuthenticationProperties { RedirectUri = IsLocalUrl(returnUrl) ? returnUrl! : "/" },
        [OpenIdConnectDefaults.AuthenticationScheme]))
    .AllowAnonymous();

app.MapGet("/account/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.SignOut(
        new AuthenticationProperties { RedirectUri = "/" },
        [OpenIdConnectDefaults.AuthenticationScheme]);
}).RequireAuthorization();

// A fixed-origin backend-for-frontend proxy. The destination host is configured by the server,
// never supplied by the browser. API authorization remains enforced by the API itself.
app.MapMethods("/bff/{**path}", ["GET", "POST", "PUT", "PATCH"], async (
    HttpContext context,
    IHttpClientFactory clients,
    string? path,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("api/v1/", StringComparison.OrdinalIgnoreCase))
        return Results.NotFound();

    var accessToken = await context.GetTokenAsync("access_token");
    if (string.IsNullOrWhiteSpace(accessToken))
        return Results.Unauthorized();

    var target = new Uri(new Uri(apiBaseUrl), path + context.Request.QueryString);
    using var outgoing = new HttpRequestMessage(new HttpMethod(context.Request.Method), target);
    outgoing.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

    if (context.Request.ContentLength is > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
    {
        outgoing.Content = new StreamContent(context.Request.Body);
        if (!string.IsNullOrWhiteSpace(context.Request.ContentType))
            outgoing.Content.Headers.TryAddWithoutValidation("Content-Type", context.Request.ContentType);
    }

    using var response = await clients.CreateClient("VaghtNegarApi")
        .SendAsync(outgoing, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
    return Results.Content(payload, contentType, System.Text.Encoding.UTF8, (int)response.StatusCode);
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static bool IsLocalUrl(string? value) =>
    !string.IsNullOrWhiteSpace(value) &&
    value.StartsWith('/') &&
    !value.StartsWith("//", StringComparison.Ordinal) &&
    !value.StartsWith("/\\", StringComparison.Ordinal);
