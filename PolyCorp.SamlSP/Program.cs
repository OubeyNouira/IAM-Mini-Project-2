
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Sustainsys.Saml2;
using Sustainsys.Saml2.AspNetCore2;
using Sustainsys.Saml2.Metadata;

var builder = WebApplication.CreateBuilder(args);

// Authentication configuration
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme =
        CookieAuthenticationDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        Saml2Defaults.Scheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = "PolyCorp.SamlSP.Auth";
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.LoginPath = "/login";
})
.AddSaml2(options =>
{
    // Our application is the SAML Service Provider.
    options.SPOptions.EntityId =
        new EntityId("https://sso.polycorp.test:5001/Saml2");

    options.SPOptions.AuthenticateRequestSigningBehavior =
        Sustainsys.Saml2.Configuration.SigningBehavior.Never;

    options.SPOptions.ModulePath = "/Saml2";

    options.SPOptions.PublicOrigin =
        new Uri("https://sso.polycorp.test:5001");

    // Keycloak is our SAML Identity Provider.
    options.IdentityProviders.Add(
        new IdentityProvider(
            new EntityId("http://sso.polycorp.test:8180/realms/master"),
            options.SPOptions)
        {
            MetadataLocation =
                "http://sso.polycorp.test:8180/realms/master/protocol/saml/descriptor"
        });
});

// Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// Middleware
app.UseAuthentication();
app.UseAuthorization();

// Home page
app.MapGet("/", (HttpContext context) =>
{
    var authenticated =
        context.User.Identity?.IsAuthenticated == true;

    var status = authenticated
        ? "Authenticated"
        : "Not authenticated";

    var username = context.User.FindFirst(
        ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

    var safeUsername = WebUtility.HtmlEncode(username);

    return Results.Content($"""
        <h1>PolyCorp SAML Application</h1>
        <p>Status: {status}</p>
        <p>Username: {safeUsername}</p>
        <p><a href="/login">Sign in with Keycloak</a></p>
        <p><a href="/profile">View protected profile</a></p>
        """, "text/html");
});

// Login endpoint
app.MapGet("/login", async (HttpContext context) =>
{
    // Avoid starting another SAML login if already authenticated.
    if (context.User.Identity?.IsAuthenticated == true)
    {
        context.Response.Redirect("/profile");
        return;
    }

    // Start SAML authentication with Keycloak.
    await context.ChallengeAsync(
        Saml2Defaults.Scheme,
        new AuthenticationProperties
        {
            RedirectUri = "/profile"
        });
});

// Protected profile
app.MapGet("/profile", (HttpContext context) =>
{
    // Read the username from the SAML NameIdentifier claim.
    var username = context.User.FindFirst(
        ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

    var safeUsername = WebUtility.HtmlEncode(username);

    // Display all claims for troubleshooting.
    var claims = context.User.Claims.Select(c =>
        $"{WebUtility.HtmlEncode(c.Type)} = " +
        $"{WebUtility.HtmlEncode(c.Value)}");

    var output = string.Join("<br>", claims);

    return Results.Content($"""
        <h1>PolyCorp User Profile</h1>

        <p>Authenticated: {context.User.Identity?.IsAuthenticated}</p>

        <h2>Welcome {safeUsername}!</h2>

        <p>SAML authentication succeeded.</p>
        <p><a href="/">Home</a></p>
        """, "text/html");
})
.RequireAuthorization();

app.Run();
