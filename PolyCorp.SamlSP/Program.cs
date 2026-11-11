
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Sustainsys.Saml2;
using Sustainsys.Saml2.AspNetCore2;
using Sustainsys.Saml2.Metadata;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// AUTHENTICATION CONFIGURATION
// ======================================================

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
    // Public Service Provider identity.
    // This must match the Keycloak client's Client ID.
    var spEntityId =
        builder.Configuration["Saml:SpEntityId"]
        ?? "https://sso.polycorp.test:5001/Saml2";

    // Public URL used by the browser to access the application.
    var publicOrigin =
        builder.Configuration["Saml:PublicOrigin"]
        ?? "https://sso.polycorp.test:5001";

    // Public Keycloak identity.
    // This must match the entity ID in Keycloak's SAML metadata.
    var idpEntityId =
        builder.Configuration["Saml:IdpEntityId"]
        ?? "http://sso.polycorp.test:8180/realms/master";

    // Internal Docker address for metadata retrieval.
    var idpMetadataUrl =
        builder.Configuration["Saml:IdpMetadataUrl"]
        ?? "http://sso.polycorp.test:8180/realms/master/protocol/saml/descriptor";

    options.SPOptions.EntityId =
        new EntityId(spEntityId);

    options.SPOptions.AuthenticateRequestSigningBehavior =
        Sustainsys.Saml2.Configuration.SigningBehavior.Never;

    options.SPOptions.ModulePath = "/Saml2";

    options.SPOptions.PublicOrigin =
        new Uri(publicOrigin);

    options.IdentityProviders.Add(
        new IdentityProvider(
            new EntityId(idpEntityId),
            options.SPOptions)
        {
            MetadataLocation = idpMetadataUrl
        });
});


builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// ======================================================
// HELPER: HTML ENCODING
// ======================================================

string Safe(string? value)
{
    return WebUtility.HtmlEncode(value ?? "Not provided");
}

// ======================================================
// SHARED WEBSITE DESIGN
// ======================================================

string RenderPage(string content, string username)
{
    return $$"""
    <!DOCTYPE html>
    <html lang="en">
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <meta name="theme-color" content="#080d18">

        <title>PolyCorp | Identity Platform</title>

        <link rel="preconnect" href="https://fonts.googleapis.com">
        <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
        <link href="https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;600;700&family=Space+Grotesk:wght@400;500;600;700&display=swap" rel="stylesheet">

        <style>
            :root {
                --bg: #080d18;
                --panel: rgba(18, 29, 48, .72);
                --border: rgba(148, 183, 220, .13);
                --text: #edf5ff;
                --muted: #91a4bd;
                --cyan: #51dcff;
                --blue: #668cff;
                --purple: #a78bfa;
            }

            * { box-sizing: border-box; }

            html { scroll-behavior: smooth; }

            body {
                margin: 0;
                color: var(--text);
                background:
                    radial-gradient(ellipse at 15% 5%, rgba(43, 100, 190, .19), transparent 38%),
                    radial-gradient(ellipse at 90% 35%, rgba(111, 75, 190, .13), transparent 35%),
                    var(--bg);
                font-family: 'DM Sans', sans-serif;
                overflow-x: hidden;
            }

            body::before {
                content: "";
                position: fixed;
                inset: 0;
                pointer-events: none;
                z-index: -1;
                opacity: .22;
                background-image:
                    linear-gradient(rgba(120,160,210,.055) 1px, transparent 1px),
                    linear-gradient(90deg, rgba(120,160,210,.055) 1px, transparent 1px);
                background-size: 55px 55px;
                mask-image: linear-gradient(to bottom, black, transparent 85%);
            }

            a { color: inherit; text-decoration: none; }

            .container {
                width: min(1160px, calc(100% - 44px));
                margin: auto;
            }

            .navbar {
                height: 78px;
                display: flex;
                align-items: center;
                justify-content: space-between;
                border-bottom: 1px solid var(--border);
            }

            .brand {
                display: flex;
                align-items: center;
                gap: 12px;
                font-family: 'Space Grotesk', sans-serif;
                font-size: 19px;
                font-weight: 700;
                letter-spacing: -.6px;
            }

            .brand-icon {
                width: 38px;
                height: 38px;
                display: grid;
                place-items: center;
                border-radius: 12px;
                color: #06111d;
                background: linear-gradient(135deg, var(--cyan), var(--blue));
                font-size: 20px;
                box-shadow: 0 0 28px rgba(81,220,255,.22);
            }

            .nav-links {
                display: flex;
                align-items: center;
                gap: 27px;
                color: var(--muted);
                font-size: 13px;
            }

            .nav-links a:hover { color: white; }

            .nav-user {
                border: 1px solid var(--border);
                background: rgba(255,255,255,.035);
                border-radius: 30px;
                padding: 9px 15px;
                color: #d9e8ff;
                font-size: 12px;
            }

            .status-dot {
                display: inline-block;
                width: 7px;
                height: 7px;
                background: #4ee5a5;
                border-radius: 50%;
                margin-right: 7px;
                box-shadow: 0 0 10px #4ee5a5;
            }

            .hero {
                padding: 105px 0 85px;
                position: relative;
            }

            .eyebrow {
                color: var(--cyan);
                font-size: 11px;
                letter-spacing: 3px;
                font-weight: 700;
                text-transform: uppercase;
                display: flex;
                align-items: center;
                gap: 10px;
            }

            .eyebrow::before {
                content: "";
                height: 1px;
                width: 27px;
                background: var(--cyan);
            }

            h1, h2, h3 {
                font-family: 'Space Grotesk', sans-serif;
            }

            .hero h1 {
                font-size: clamp(42px, 7vw, 76px);
                letter-spacing: -4px;
                line-height: 1.08;
                margin: 25px 0;
                max-width: 850px;
            }

            .gradient-text {
                background: linear-gradient(90deg, #fff, var(--cyan), #9b9bff);
                -webkit-background-clip: text;
                background-clip: text;
                color: transparent;
            }

            .hero-description {
                color: var(--muted);
                max-width: 620px;
                line-height: 1.9;
                font-size: 15px;
            }

            .welcome-card {
                margin-top: 38px;
                display: inline-flex;
                align-items: center;
                gap: 15px;
                padding: 16px 21px;
                background: rgba(18, 34, 57, .78);
                border: 1px solid rgba(81,220,255,.24);
                border-radius: 15px;
                box-shadow: 0 12px 50px rgba(0,0,0,.2);
            }

            .avatar {
                width: 43px;
                height: 43px;
                border-radius: 13px;
                display: grid;
                place-items: center;
                background: linear-gradient(135deg, #43c8ed, #6578ff);
                color: #071321;
                font-size: 18px;
                font-weight: 800;
            }

            .welcome-card small {
                display: block;
                color: var(--muted);
                font-size: 11px;
                margin-bottom: 5px;
            }

            .welcome-card strong { font-size: 14px; }

            .actions {
                display: flex;
                gap: 12px;
                flex-wrap: wrap;
                margin-top: 27px;
            }

            .btn {
                display: inline-flex;
                justify-content: center;
                align-items: center;
                gap: 9px;
                padding: 13px 19px;
                border-radius: 10px;
                font-weight: 700;
                font-size: 13px;
                transition: .25s;
            }

            .btn-primary {
                color: #06111d;
                background: linear-gradient(110deg, var(--cyan), #7f9aff);
                box-shadow: 0 8px 30px rgba(81,220,255,.16);
            }

            .btn-secondary {
                border: 1px solid var(--border);
                background: rgba(255,255,255,.035);
                color: var(--text);
            }

            .btn:hover {
                transform: translateY(-3px);
                box-shadow: 0 12px 30px rgba(81,220,255,.17);
            }

            .section { padding: 55px 0 85px; }

            .section-heading {
                display: flex;
                align-items: end;
                justify-content: space-between;
                gap: 20px;
                margin-bottom: 35px;
            }

            .section-heading h2 {
                font-size: clamp(28px, 4vw, 39px);
                margin: 12px 0 0;
                letter-spacing: -1.5px;
            }

            .section-heading p {
                color: var(--muted);
                max-width: 410px;
                font-size: 13px;
                line-height: 1.8;
            }

            .tech-grid {
                display: grid;
                grid-template-columns: repeat(3, 1fr);
                gap: 17px;
            }

            .tech-card {
                position: relative;
                overflow: hidden;
                min-height: 210px;
                padding: 25px;
                border-radius: 17px;
                border: 1px solid var(--border);
                background: linear-gradient(145deg, rgba(22,37,60,.82), rgba(12,20,34,.78));
                transition: transform .25s, border-color .25s, box-shadow .25s;
                transform-style: preserve-3d;
            }

            .tech-card::before {
                content: "";
                position: absolute;
                width: 160px;
                height: 160px;
                border-radius: 50%;
                background: var(--glow, rgba(81,220,255,.12));
                filter: blur(60px);
                right: -70px;
                top: -80px;
                pointer-events: none;
            }

            .tech-card:hover {
                border-color: rgba(81,220,255,.45);
                box-shadow: 0 20px 60px rgba(0,0,0,.28);
                transform: translateY(-7px);
            }

            .tech-icon {
                font-size: 28px;
                margin-bottom: 21px;
            }

            .tech-card h3 {
                font-size: 17px;
                margin: 0 0 9px;
            }

            .tech-card p {
                font-size: 12px;
                color: var(--muted);
                line-height: 1.8;
                margin: 0;
            }

            .tag {
                display: inline-block;
                margin-top: 17px;
                padding: 5px 9px;
                border-radius: 6px;
                background: rgba(81,220,255,.08);
                border: 1px solid rgba(81,220,255,.14);
                color: #8ce8ff;
                font-size: 10px;
                letter-spacing: .5px;
            }

            .architecture {
                padding: 30px;
                border: 1px solid var(--border);
                border-radius: 19px;
                background: rgba(14,25,42,.77);
            }

            .flow {
                display: flex;
                align-items: center;
                justify-content: center;
                gap: 10px;
                flex-wrap: wrap;
                padding: 30px 0 10px;
            }

            .flow-node {
                min-width: 130px;
                text-align: center;
                padding: 18px 13px;
                border: 1px solid rgba(81,220,255,.2);
                background: rgba(81,220,255,.055);
                border-radius: 12px;
            }

            .flow-node strong {
                display: block;
                font-size: 12px;
                margin-top: 8px;
            }

            .flow-node span {
                font-size: 10px;
                color: var(--muted);
            }

            .flow-arrow {
                color: var(--cyan);
                font-size: 20px;
            }

            .stats {
                display: grid;
                grid-template-columns: repeat(3, 1fr);
                gap: 15px;
                margin-top: 22px;
            }

            .stat {
                padding: 20px;
                background: rgba(255,255,255,.025);
                border: 1px solid var(--border);
                border-radius: 12px;
            }

            .stat strong {
                font-family: 'Space Grotesk', sans-serif;
                font-size: 25px;
                color: var(--cyan);
            }

            .stat p {
                font-size: 11px;
                color: var(--muted);
                margin-bottom: 0;
            }

            .profile-panel {
                border: 1px solid rgba(81,220,255,.22);
                background: linear-gradient(135deg, rgba(20,43,68,.85), rgba(18,22,44,.85));
                border-radius: 19px;
                padding: 28px;
                margin-top: 30px;
            }

            .profile-panel h3 { margin-top: 0; }

            .claims-grid {
                display: grid;
                grid-template-columns: repeat(2, 1fr);
                gap: 11px;
                margin-top: 20px;
            }

            .claim {
                min-width: 0;
                padding: 15px;
                border: 1px solid var(--border);
                background: rgba(0,0,0,.14);
                border-radius: 10px;
            }

            .claim small {
                display: block;
                color: var(--cyan);
                font-size: 10px;
                margin-bottom: 8px;
                overflow-wrap: anywhere;
            }

            .claim strong {
                font-size: 12px;
                overflow-wrap: anywhere;
            }

            .footer {
                border-top: 1px solid var(--border);
                padding: 28px 0;
                color: var(--muted);
                font-size: 11px;
                display: flex;
                justify-content: space-between;
                gap: 15px;
                flex-wrap: wrap;
            }
            .cursor-glow {
                position: fixed;
                width: 7px;
                height: 7px;
                border-radius: 50%;
                background: #72e8ff;
                box-shadow: 0 0 10px 3px rgba(81,220,255,.3);
                pointer-events: none;
                z-index: 9999;
                animation: trailFade .38s ease-out forwards;
            }

            @keyframes trailFade {
                from {
                    opacity: .8;
                    transform: scale(1);
                }
                to {
                    opacity: 0;
                    transform: scale(0);
                }
            }

            .reveal {
                opacity: 0;
                transform: translateY(22px);
                transition: opacity .7s ease, transform .7s ease;
            }

            .reveal.visible {
                opacity: 1;
                transform: translateY(0);
            }

            .profile-intro {
                color: var(--muted);
                font-size: 13px;
                line-height: 1.8;
            }

            @@media(max-width: 760px) {
                .nav-links { display: none; }
                .hero { padding: 75px 0 55px; }
                .hero h1 { letter-spacing: -2px; }
                .tech-grid { grid-template-columns: repeat(2, 1fr); }
                .section-heading { display: block; }
                .stats { grid-template-columns: 1fr; }
                .flow { flex-direction: column; }
                .flow-arrow { transform: rotate(90deg); }
            }

            @@media(max-width: 480px) {
                .tech-grid, .claims-grid { grid-template-columns: 1fr; }
                .container { width: calc(100% - 30px); }
                .hero h1 { font-size: 43px; }
            }

            @@media(prefers-reduced-motion: reduce) {
                *, *::before, *::after {
                    animation-duration: .01ms !important;
                    transition-duration: .01ms !important;
                    scroll-behavior: auto !important;
                }
            }
        </style>
    </head>

    <body>
        <div class="container">

            <nav class="navbar">
                <a class="brand" href="/">
                    <span class="brand-icon">P</span>
                    POLYCORP
                </a>

                <div class="nav-links">
                    <a href="/#overview">Overview</a>
                    <a href="/#architecture">Architecture</a>
                    <a href="/#technologies">Technologies</a>
                    <a href="/profile">Identity</a>
                </div>

                <div class="nav-user">
                    <span class="status-dot"></span>
                    {{Safe(username)}}
                </div>
            </nav>

            {{content}}

            <footer class="footer">
                <span>© 2026 PolyCorp Identity Platform</span>
                <span>Enterprise Identity & Access Management · IAM Lab</span>
            </footer>

        </div>

        <script>
                    // Smooth, short-lived cursor trail.
            let lastTrail = 0;

            document.addEventListener("mousemove", function(e) {
                const now = Date.now();

                if (now - lastTrail < 12) return;
                lastTrail = now;

                const dot = document.createElement("span");
                dot.className = "cursor-glow";
                dot.style.left = e.clientX + "px";
                dot.style.top = e.clientY + "px";

                document.body.appendChild(dot);

                setTimeout(() => dot.remove(), 400);
            });

            // Reveal cards as they enter the viewport.
            const observer = new IntersectionObserver(entries => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        entry.target.classList.add("visible");
                        observer.unobserve(entry.target);
                    }
                });
            }, { threshold: 0.12 });

            document.querySelectorAll(".reveal").forEach(el => {
                observer.observe(el);
            });
        </script>
    </body>
    </html>
    """;
}

// ======================================================
// HOME PAGE
// ======================================================

app.MapGet("/", (HttpContext context) =>
{
    bool authenticated =
        context.User.Identity?.IsAuthenticated == true;

    string username = authenticated
        ? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.User.Identity?.Name
            ?? "Authenticated User"
        : "Guest";

    string safeUsername = Safe(username);

    string welcome = authenticated
        ? $"Welcome back, {safeUsername}."
        : "One identity. Every application.";

    string description = authenticated
        ? "Your identity has been verified through the PolyCorp authentication infrastructure. You are now accessing the application with your authenticated domain identity."
        : "A modern identity and access management demonstration connecting Windows Active Directory, Kerberos, Keycloak and SAML 2.0 into a unified authentication experience.";

    string authCard = authenticated
   
    ? $$"""
      <div class="welcome-card">
          <div class="avatar">✓</div>
          <div>
              <small>AUTHENTICATED IDENTITY</small>
              <strong>{{safeUsername}}</strong>
              <div style="color:#54e6a8;font-size:11px;margin-top:5px">
                  <span class="status-dot"></span>Verified session
              </div>
          </div>
      </div>
      """
    : """
      <div class="welcome-card">
          <div class="avatar">P</div>
          <div>
              <small>YOUR IDENTITY STARTS HERE</small>
              <strong>Secure enterprise authentication</strong>
          </div>
      </div>
      """;

    string primaryAction = authenticated
        ? """<a class="btn btn-primary" href="/profile">Explore my identity →</a>"""
        : """<a class="btn btn-primary" href="/login">Authenticate with Keycloak →</a>""";

    string content = $$"""
    <main>

        <section class="hero" id="overview">
            <div class="eyebrow">POLYCORP · IDENTITY PLATFORM</div>

            <h1>
                {{welcome}}
                <br>
                <span class="gradient-text">Identity without boundaries.</span>
            </h1>

            <p class="hero-description">{{description}}</p>

            {{authCard}}

            <div class="actions">
                {{primaryAction}}
                <a class="btn btn-secondary" href="#architecture">
                    Explore the architecture ↓
                </a>
            </div>
        </section>

        <section class="section" id="technologies">
            <div class="section-heading">
                <div>
                    <div class="eyebrow">THE TECHNOLOGY STACK</div>
                    <h2>Six layers. One identity.</h2>
                </div>
                <p>
                    Each component has a specific responsibility in the
                    authentication process, working together to establish
                    trusted access to enterprise applications.
                </p>
            </div>

            <div class="tech-grid">

                <article class="tech-card reveal" style="--glow:rgba(75,130,255,.2)">
                    <div class="tech-icon">🖥️</div>
                    <h3>Active Directory</h3>
                    <p>
                        Centralized directory services manage domain users,
                        computers, groups and enterprise identities.
                    </p>
                    <span class="tag">IDENTITY SOURCE</span>
                </article>

                <article class="tech-card reveal" style="--glow:rgba(81,220,255,.2)">
                    <div class="tech-icon">🔐</div>
                    <h3>Kerberos</h3>
                    <p>
                        Ticket-based authentication enables Windows users
                        to access trusted services without repeatedly
                        entering their passwords.
                    </p>
                    <span class="tag">SINGLE SIGN-ON</span>
                </article>

                <article class="tech-card reveal" style="--glow:rgba(170,130,255,.2)">
                    <div class="tech-icon">🛡️</div>
                    <h3>Keycloak</h3>
                    <p>
                        Identity and access management broker that connects
                        enterprise authentication with web applications.
                    </p>
                    <span class="tag">IDENTITY PROVIDER</span>
                </article>

                <article class="tech-card reveal" style="--glow:rgba(70,210,180,.2)">
                    <div class="tech-icon">🔗</div>
                    <h3>LDAP Federation</h3>
                    <p>
                        Integrates Keycloak with Active Directory so that
                        domain identities can be synchronized and managed.
                    </p>
                    <span class="tag">DIRECTORY INTEGRATION</span>
                </article>

                <article class="tech-card reveal" style="--glow:rgba(255,180,100,.2)">
                    <div class="tech-icon">📜</div>
                    <h3>SAML 2.0</h3>
                    <p>
                        XML-based federation protocol transfers trusted
                        authentication assertions from Keycloak to the
                        service provider.
                    </p>
                    <span class="tag">FEDERATED AUTHENTICATION</span>
                </article>

                <article class="tech-card reveal" style="--glow:rgba(110,140,255,.2)">
                    <div class="tech-icon">⚙️</div>
                    <h3>ASP.NET Core</h3>
                    <p>
                        The protected service provider validates the
                        authentication response and establishes the
                        application session.
                    </p>
                    <span class="tag">SERVICE PROVIDER</span>
                </article>

            </div>
        </section>

        <section class="section" id="architecture">
            <div class="section-heading">
                <div>
                    <div class="eyebrow">HOW IT WORKS</div>
                    <h2>Authentication journey.</h2>
                </div>
                <p>
                    From a Windows domain session to a protected web
                    application, each step contributes to a trusted
                    authentication chain.
                </p>
            </div>

            <div class="architecture reveal">

                <div class="flow">

                    <div class="flow-node">
                        <div>🖥️</div>
                        <strong>Windows Login</strong>
                        <span>Domain identity</span>
                    </div>

                    <div class="flow-arrow">→</div>

                    <div class="flow-node">
                        <div>🔐</div>
                        <strong>Kerberos</strong>
                        <span>SPNEGO ticket</span>
                    </div>

                    <div class="flow-arrow">→</div>

                    <div class="flow-node">
                        <div>🛡️</div>
                        <strong>Keycloak</strong>
                        <span>Identity provider</span>
                    </div>

                    <div class="flow-arrow">→</div>

                    <div class="flow-node">
                        <div>📜</div>
                        <strong>SAML 2.0</strong>
                        <span>Signed response</span>
                    </div>

                    <div class="flow-arrow">→</div>

                    <div class="flow-node">
                        <div>⚙️</div>
                        <strong>ASP.NET Core</strong>
                        <span>Protected application</span>
                    </div>

                </div>

                <div class="stats">
                    <div class="stat">
                        <strong>01</strong>
                        <p>Centralized identity source</p>
                    </div>
                    <div class="stat">
                        <strong>02</strong>
                        <p>Integrated authentication</p>
                    </div>
                    <div class="stat">
                        <strong>03</strong>
                        <p>Federated application access</p>
                    </div>
                </div>

            </div>
        </section>

        <section class="section">
            <div class="section-heading">
                <div>
                    <div class="eyebrow">PROJECT OBJECTIVE</div>
                    <h2>From workstation to web.</h2>
                </div>
            </div>

            <div class="architecture reveal">
                <h3 style="margin-top:0">A passwordless application access experience</h3>

                <p class="profile-intro">
                    This project demonstrates how an organization can
                    combine an existing Windows domain environment with
                    modern identity federation. Users authenticate to
                    their domain workstation, Kerberos provides integrated
                    authentication to Keycloak, and SAML 2.0 communicates
                    the resulting identity to the ASP.NET Core application.
                </p>

                <p class="profile-intro">
                    The application then validates the SAML response,
                    establishes an authenticated session and makes
                    identity claims available to protected resources.
                </p>
            </div>
        </section>

    </main>
    """;

    return Results.Content(
        RenderPage(content, username),
        "text/html; charset=utf-8");
});

// ======================================================
// LOGIN ENDPOINT
// ======================================================

app.MapGet("/login", async (HttpContext context) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        context.Response.Redirect("/profile");
        return;
    }

    await context.ChallengeAsync(
        Saml2Defaults.Scheme,
        new AuthenticationProperties
        {
            RedirectUri = "/profile"
        });
});

// ======================================================
// PROTECTED USER PROFILE
// ======================================================

app.MapGet("/profile", (HttpContext context) =>
{
    string username =
        context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? context.User.Identity?.Name
        ?? "Unknown";

    string safeUsername = Safe(username);

    var claims = context.User.Claims.ToList();

    string claimsHtml = string.Join(
        "",
        claims.Select(claim => $$"""
            <div class="claim">
                <small>{{Safe(claim.Type)}}</small>
                <strong>{{Safe(claim.Value)}}</strong>
            </div>
            """));

    string email =
        context.User.FindFirst(ClaimTypes.Email)?.Value
        ?? "Not provided";

    string name =
        context.User.FindFirst(ClaimTypes.Name)?.Value
        ?? username;

    string content = $$"""
    <main>

        <section class="hero">
            <div class="eyebrow">IDENTITY CENTER · PROTECTED AREA</div>

            <h1>
                Welcome,
                <br>
                <span class="gradient-text">{{safeUsername}}</span>
            </h1>

            <p class="hero-description">
                Your identity has been successfully authenticated.
                This profile is protected by the PolyCorp SAML
                authentication infrastructure.
            </p>

            <div class="welcome-card">
                <div class="avatar">✓</div>
                <div>
                    <small>AUTHENTICATION STATUS</small>
                    <strong style="color:#54e6a8">Successfully authenticated</strong>
                </div>
            </div>
        </section>

        <section class="section">
            <div class="section-heading">
                <div>
                    <div class="eyebrow">YOUR DIGITAL IDENTITY</div>
                    <h2>Identity overview.</h2>
                </div>
            </div>

            <div class="tech-grid">

                <div class="tech-card reveal">
                    <div class="tech-icon">👤</div>
                    <h3>Username</h3>
                    <p>{{safeUsername}}</p>
                    <span class="tag">NAME IDENTIFIER</span>
                </div>

                <div class="tech-card reveal">
                    <div class="tech-icon">✉️</div>
                    <h3>Email</h3>
                    <p>{{Safe(email)}}</p>
                    <span class="tag">IDENTITY ATTRIBUTE</span>
                </div>

                <div class="tech-card reveal">
                    <div class="tech-icon">🔐</div>
                    <h3>Authentication</h3>
                    <p>SAML 2.0 via Keycloak</p>
                    <span class="tag">FEDERATED SESSION</span>
                </div>

            </div>

            <div class="profile-panel reveal">
                <div class="eyebrow">SAML ASSERTION DATA</div>
                <h3>Claims received by the application</h3>

                <p class="profile-intro">
                    The following attributes are available in the
                    authenticated ASP.NET Core ClaimsPrincipal.
                    Values are taken from the current authentication
                    session, not hardcoded into the page.
                </p>

                <div class="claims-grid">
                    {{claimsHtml}}
                </div>

                <p class="profile-intro" style="margin-top:25px">
                    Total claims received: <strong>{{claims.Count}}</strong>
                </p>
            </div>

            <div class="actions">
                <a class="btn btn-primary" href="/">← Back to presentation</a>
            </div>

        </section>

    </main>
    """;

    return Results.Content(
        RenderPage(content, username),
        "text/html; charset=utf-8");
})
.RequireAuthorization();

app.Run();
