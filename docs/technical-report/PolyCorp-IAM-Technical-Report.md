# POLYCORP IAM MINI-PROJECT 2

## Technical Report
### From Windows Domain Authentication to Passwordless Web Access

**Project:** Identity and Access Management — Single Sign-On  
**Environment:** PolyCorp — `polycorp.test`  
**Application:** PolyCorp.SamlSP  
**Technologies:** Active Directory, Kerberos, LDAP, Keycloak, SAML 2.0, ASP.NET Core 8  
**Author:** Oubey Nouira  
**Year:** 2026

---

# Table of Contents

1. Introduction
2. Project Objectives
3. Technical Environment
4. System Architecture
5. Active Directory Configuration
6. Kerberos Integration
7. Keycloak Deployment
8. LDAP Federation
9. Kerberos Browser SSO
10. SAML Integration
11. ASP.NET Core Service Provider
12. Authentication Workflow
13. Testing and Validation
14. Security Considerations
15. Limitations and Future Improvements
16. Conclusion

---

# 1. Introduction

Identity and Access Management (IAM) is an essential component of modern enterprise information systems. Organizations need centralized mechanisms to manage user identities, control access to applications, and reduce the risks associated with repeated password authentication.

This project, entitled **"From Windows Domain Authentication to Passwordless Web Access"**, demonstrates the integration of Microsoft Active Directory with Keycloak to provide Single Sign-On (SSO) for a web application.

The solution allows a user already authenticated on a Windows domain workstation to access a protected web application without manually entering their credentials again.

The implementation combines Kerberos authentication, LDAP federation, Keycloak and SAML 2.0.

The project is developed as a laboratory environment using virtual machines and a local Docker-based deployment.

# 2. Project Objectives

The main objective is to establish an authentication chain connecting Windows domain authentication to a modern web application.

The specific objectives are:

- Configure an Active Directory domain and domain-joined workstation.
- Create a dedicated service account for Keycloak.
- Configure Kerberos authentication using a Service Principal Name (SPN) and keytab.
- Deploy Keycloak as a centralized Identity Provider.
- Integrate Active Directory users through LDAP federation.
- Configure Kerberos-based browser authentication using SPNEGO.
- Integrate a SAML Service Provider developed with ASP.NET Core.
- Validate the complete authentication flow using a domain user.

The expected result is transparent authentication: the user logs into Windows and can access the protected application using the existing domain identity.

# 3. Technical Environment

## 3.1 Infrastructure

The laboratory uses VMware Workstation to host the Windows domain environment. Keycloak and the ASP.NET Core application run on the Windows 11 host.

| Component | Operating system / technology | Responsibility |
|---|---|---|
| DC01 | Windows Server | Active Directory, DNS and Kerberos KDC |
| CL-01 | Windows 10 | Domain-joined client workstation |
| Host machine | Windows 11 | Docker and ASP.NET Core hosting |
| Keycloak | Version 26.8.2 | Identity Provider |
| PolyCorp.SamlSP | ASP.NET Core 8 | SAML Service Provider |

## 3.2 Network Configuration

The laboratory uses two network interfaces on the virtual machines:

- An isolated LAN segment for communication between the domain controller and client.
- A VMware Host-only network (VMnet1) to allow communication between the virtual machines and the Windows host.

The relevant network configuration is:

| Machine | Network | IP address |
|---|---|---|
| DC01 | Original laboratory LAN | 192.168.50.10 |
| CL-01 | Original laboratory LAN | 192.168.50.20 |
| DC01 | VMnet1 | 192.168.6.128 |
| CL-01 | VMnet1 | 192.168.6.129 |
| Windows 11 Host | VMnet1 | 192.168.6.1 |

The domain DNS configuration remains associated with the original domain controller network.

The hostname `sso.polycorp.test` is resolved to the host address `192.168.6.1` by the domain DNS server for the laboratory environment.

The services are accessed using the following URLs:

| Service | Address |
|---|---|
| Keycloak | http://sso.polycorp.test:8180 |
| SAML Service Provider | https://sso.polycorp.test:5001 |

# 4. System Architecture

The architecture is based on the integration of three identity and authentication components.

**Active Directory:** Maintains domain identities and provides Kerberos authentication.

**Keycloak:** Acts as the centralized Identity Provider. It connects to Active Directory through LDAP and authenticates users through Kerberos/SPNEGO.

**PolyCorp.SamlSP:** Acts as the SAML Service Provider. It receives and validates SAML responses issued by Keycloak.

## 4.1 Authentication Architecture

```text
+-----------------------------+
|       Windows 10 CL-01      |
|                             |
|   Domain User: bdupont      |
+--------------+--------------+
               |
               | Kerberos / SPNEGO
               |
               v
+-----------------------------+
|       Active Directory      |
|                             |
|   Domain: polycorp.test     |
|   DC01 / Kerberos KDC       |
+--------------+--------------+
               |
               | LDAP Federation
               |
               v
+-----------------------------+
|          Keycloak           |
|                             |
|   Identity Provider (IdP)   |
|   Kerberos Authentication   |
|   LDAP User Federation      |
+--------------+--------------+
               |
               | SAML 2.0 Assertion
               |
               v
+-----------------------------+
|       PolyCorp.SamlSP       |
|                             |
|   ASP.NET Core 8            |
|   SAML Service Provider     |
+--------------+--------------+
               |
               v
+-----------------------------+
|      Protected Profile      |
|                             |
|       Welcome bdupont!      |
+-----------------------------+
```

# 5. Active Directory Configuration

## 5.1 Domain Creation

An Active Directory domain named `polycorp.test` was configured on the Windows Server virtual machine DC01.

The domain controller provides the following services:

- Active Directory Domain Services (AD DS).
- Domain Name System (DNS).
- Kerberos Key Distribution Center (KDC).
- Netlogon.

The Windows 10 workstation CL-01 was joined to the domain.

## 5.2 Domain User

A domain user named `bdupont` was used to validate the authentication process.

The account is used to demonstrate how an existing Windows domain identity can be reused to access the web application.

## 5.3 Keycloak Service Account

A dedicated Active Directory service account named `svc_keycloak` was created.

This account is used by Keycloak to authenticate its Kerberos service identity.

The following Service Principal Name was registered:

`HTTP/sso.polycorp.test`

The service account was configured to support AES encryption.

The supported encryption types were configured using the Active Directory attribute:

`msDS-SupportedEncryptionTypes = 24`

This enables AES128 and AES256 encryption support.

# 6. Kerberos Integration

## 6.1 Kerberos Overview

Kerberos is a network authentication protocol based on tickets. It allows users and services to authenticate without transmitting passwords directly during each authentication request.

In this project, Kerberos is used to authenticate the Windows domain user to Keycloak through the browser.

The domain controller acts as the Key Distribution Center (KDC).

## 6.2 Service Principal Name

The HTTP SPN identifies the Keycloak service within Active Directory:

`HTTP/sso.polycorp.test`

The SPN allows Active Directory to associate Kerberos service tickets with the correct service account.

## 6.3 Keytab Generation

A Kerberos keytab was generated on DC01 using the `ktpass` utility.

The keytab contains the cryptographic key associated with the service principal.

It was transferred to the Windows host and mounted into the Keycloak container.

The file is accessed inside the container through:

`/opt/keycloak/conf/http.keytab`

The private Kerberos keytab is excluded from the public Git repository.

## 6.4 Kerberos Configuration

A `krb5.conf` file was created to configure the Kerberos realm and KDC.

The configured realm is:

`POLYCORP.TEST`

The configuration identifies the domain controller as the Kerberos server.

The configuration file is mounted inside the Keycloak container.

## 6.5 Kerberos Ticket Validation

On CL-01, the following commands were used to request and inspect the HTTP service ticket:

```powershell
klist purge
klist get HTTP/sso.polycorp.test
klist
```

The resulting HTTP service ticket used:

`AES-256-CTS-HMAC-SHA1-96`

This confirmed that the client obtained a ticket using the AES256 encryption type configured for the service account and keytab.

# 7. Keycloak Deployment

## 7.1 Deployment with Docker

Keycloak version 26.8.2 was deployed using Docker Desktop and Docker Compose.

The deployment uses a local container configuration with HTTP enabled for the laboratory.

Keycloak is exposed through port 8180.

The Docker Compose configuration includes:

- Keycloak container image.
- Environment variables for initial administration.
- Kerberos configuration file.
- HTTP keytab.
- Persistent local database storage.
- Automatic container restart configuration.

## 7.2 Persistent Storage

Persistent storage was configured to preserve the Keycloak database when the container is recreated.

The local directory:

`keycloak-data/`

is mounted inside the container at:

`/opt/keycloak/data`

This ensures that the laboratory configuration, including the LDAP provider and client settings, remains available across container restarts.

## 7.3 Identity Provider Role

Keycloak is responsible for:

- Managing authentication flows.
- Connecting to Active Directory.
- Authenticating users using Kerberos.
- Issuing SAML responses to registered applications.

# 8. LDAP Federation

## 8.1 Integration with Active Directory

Keycloak was configured with an LDAP User Federation provider connected to the Active Directory domain controller.

The connection uses the LDAP protocol over the laboratory network.

The service account `svc_keycloak` is used for LDAP binding.

## 8.2 LDAP Configuration

The main configuration parameters are:

| Parameter | Configuration |
|---|---|
| Vendor | Active Directory |
| Connection URL | ldap://192.168.6.128:389 |
| Bind Type | Simple |
| Bind DN | svc_keycloak@polycorp.test |
| Edit Mode | READ_ONLY |
| Users DN | DC=polycorp,DC=test |
| Username Attribute | sAMAccountName |
| RDN Attribute | cn |
| UUID Attribute | objectGUID |
| Search Scope | Subtree |
| Import Users | Enabled |
| Kerberos Integration | Enabled |

The configuration allows Keycloak to retrieve user information from Active Directory while maintaining the directory as the authoritative identity source.

## 8.3 User Synchronization

The LDAP synchronization operation was used to import and update domain user information in Keycloak.

The synchronization mechanism ensures that Keycloak can associate an authenticated domain identity with the corresponding user record.

# 9. Kerberos Browser SSO

## 9.1 Browser Authentication

The Keycloak Browser authentication flow was duplicated to create a dedicated flow named:

`Browser-Kerberos`

A Kerberos execution was added with the requirement set to `ALTERNATIVE`.

This allows Keycloak to attempt Kerberos authentication before falling back to the remaining browser authentication mechanisms.

## 9.2 Browser Configuration

Microsoft Edge on CL-01 was configured to allow integrated authentication to:

`sso.polycorp.test`

The `AuthServerAllowlist` policy was configured to authorize the hostname for integrated authentication.

The hostname was also added to the Local Intranet zone.

## 9.3 Automatic Authentication

After signing into Windows using:

`POLYCORP\bdupont`

the user accessed Keycloak from the domain workstation.

The browser automatically used the existing Kerberos credentials.

The Keycloak account page opened without requiring the user to enter their username or password again.

This validated the Kerberos-based browser SSO mechanism.

# 10. SAML Integration

## 10.1 SAML Architecture

Security Assertion Markup Language (SAML) 2.0 is an XML-based protocol used to exchange authentication and authorization information between an Identity Provider and a Service Provider.

In this project:

- Keycloak acts as the Identity Provider.
- PolyCorp.SamlSP acts as the Service Provider.

Keycloak authenticates the user and issues a SAML response to the application.

## 10.2 SAML Client Configuration

A SAML client was configured in the Keycloak master realm.

The client represents the ASP.NET Core application.

The Service Provider Entity ID is:

`https://sso.polycorp.test:5001/Saml2`

The Assertion Consumer Service URL is:

`https://sso.polycorp.test:5001/Saml2/Acs`

The client was configured to use the HTTP-POST binding for the SAML response.

The client configuration enables document signing. Assertion signing was left disabled in the laboratory configuration.

## 10.3 Service Provider Metadata

The ASP.NET Core application exposes its SAML metadata through:

`https://sso.polycorp.test:5001/Saml2`

The metadata describes the Service Provider identity and the supported Assertion Consumer Service endpoints.

The metadata confirmed the following values:

- Entity ID: `https://sso.polycorp.test:5001/Saml2`
- HTTP-POST ACS: `https://sso.polycorp.test:5001/Saml2/Acs`
- AuthnRequestsSigned: false
- WantAssertionsSigned: false

These values were used to configure the Keycloak SAML client.

# 11. ASP.NET Core Service Provider

## 11.1 Application Creation

The Service Provider was developed using ASP.NET Core 8.

The project was created using the ASP.NET Core empty web application template.

The following NuGet package was installed:

`Sustainsys.Saml2.AspNetCore2 2.11.0`

This package provides SAML authentication integration with ASP.NET Core.

## 11.2 Authentication Configuration

The application uses two authentication schemes:

- Cookie authentication for maintaining the authenticated application session.
- SAML authentication for initiating authentication through Keycloak.

The default authentication scheme is configured as Cookie authentication.

The default challenge scheme is configured as SAML authentication.

The cookie configuration includes:

- A dedicated application cookie name.
- SameSite=None.
- SecurePolicy=Always.

These settings support the cross-site SAML POST response while requiring the application authentication cookie to be sent over HTTPS.

## 11.3 Application Endpoints

The application exposes three main routes.

| Endpoint | Purpose |
|---|---|
| `/` | Displays authentication status |
| `/login` | Starts SAML authentication |
| `/profile` | Displays the authenticated user's profile |

The profile endpoint is protected using ASP.NET Core authorization.

Users who do not have an authenticated application session are redirected into the SAML authentication process.

## 11.4 User Identity Extraction

After successful SAML response processing, the application retrieves the user's identity from the SAML claims.

The username is extracted from the NameIdentifier claim:

```csharp
var username = context.User.FindFirst(
    ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
```

During validation from CL-01, the NameIdentifier claim contained:

`bdupont`

The application therefore displays:

**Welcome bdupont!**

# 12. Authentication Workflow

The complete authentication process consists of the following stages.

### Stage 1 — Windows Authentication

The user logs into CL-01 using their Active Directory account.

Windows establishes the domain session and obtains Kerberos tickets as required.

### Stage 2 — Application Access

The user opens the PolyCorp application.

The application requires authentication before granting access to the protected profile.

### Stage 3 — Keycloak Authentication

The application initiates a SAML authentication request.

The browser is redirected to Keycloak.

### Stage 4 — Kerberos Authentication

Keycloak uses its configured Kerberos integration to authenticate the browser through SPNEGO.

The browser uses the existing Windows domain credentials.

### Stage 5 — LDAP Identity Association

Keycloak associates the authenticated domain identity with the corresponding Active Directory user through LDAP federation.

### Stage 6 — SAML Response

Keycloak issues a SAML response containing the authenticated identity.

The browser sends this response to the Service Provider's ACS endpoint.

### Stage 7 — Application Authentication

Sustainsys.Saml2 processes the SAML response.

ASP.NET Core establishes an authenticated cookie session.

### Stage 8 — Protected Resource Access

The application authorizes access to the profile and displays:

`Welcome bdupont!`

# 13. Testing and Validation

The following tests were performed during the implementation.

| Test | Validation | Result |
|---|---|---|
| Domain workstation login | User logged in as bdupont | Successful |
| DNS resolution | sso.polycorp.test resolved to host | Successful |
| Kerberos ticket acquisition | HTTP service ticket obtained | Successful |
| Encryption verification | AES-256 ticket confirmed | Successful |
| LDAP connection | Keycloak connected to AD | Successful |
| LDAP synchronization | Domain users synchronized | Successful |
| Kerberos browser SSO | Automatic Keycloak authentication | Successful |
| HTTPS access | CL-01 accessed application without certificate warning | Successful |
| SAML metadata | Entity ID and ACS endpoint verified | Successful |
| SAML response processing | Sustainsys processed SAML response | Successful |
| Identity extraction | NameIdentifier returned bdupont | Successful |
| Protected profile | Welcome bdupont displayed | Successful |

## 13.1 Final Demonstration

The final validation was performed from the Windows 10 domain workstation.

The user was already authenticated to the domain as `POLYCORP\bdupont`.

The browser accessed the PolyCorp application and completed the authentication process without requesting the domain password again.

The protected profile displayed the authenticated user's identity.

**Final expected application output:**

```text
PolyCorp User Profile

Authenticated: True

Welcome bdupont!

SAML authentication succeeded.
```

# 14. Security Considerations

The project demonstrates several security mechanisms:

- Centralized identity management using Active Directory.
- Dedicated service account for Kerberos integration.
- AES-based Kerberos encryption.
- LDAP read-only federation.
- HTTPS communication between CL-01 and the Service Provider.
- Trusted certificate installation on the domain client.
- Protected ASP.NET Core application endpoints.
- Cookie-based session management.
- Separation of public certificates and private cryptographic material.

## 14.1 Laboratory Limitations

Some settings were selected specifically to simplify the local laboratory deployment.

Keycloak uses its development-oriented configuration and HTTP access on port 8180.

LDAP communication uses plain LDAP within the isolated laboratory network.

The ASP.NET Core SAML metadata indicates that signed authentication requests and mandatory signed assertions are not enabled in the current configuration.

The SAML client is configured to sign documents, while assertion signing is disabled.

The project should therefore be considered a functional IAM laboratory demonstration rather than a production-ready security deployment.

For production, the following improvements are recommended:

- Deploy Keycloak with a production database and hardened configuration.
- Enforce HTTPS for all identity services.
- Use LDAPS or StartTLS for LDAP communication.
- Enable and validate signed SAML assertions.
- Configure appropriate signing certificates and key rotation.
- Apply least-privilege permissions to service accounts.
- Protect and rotate all secrets.
- Review session expiration, logout and revocation behavior.
- Monitor authentication events and security logs.

# 15. Limitations and Future Improvements

The current implementation focuses on Kerberos-based browser SSO and SAML authentication.

Possible future improvements include:

1. Configure a production-ready Keycloak deployment.
2. Enable signed SAML assertions and verify signature validation.
3. Implement Single Logout (SLO).
4. Add user role mapping from Active Directory groups.
5. Integrate an additional application using OpenID Connect (OIDC).
6. Improve audit logging and authentication monitoring.
7. Automate deployment and configuration.
8. Add integration tests for authentication and authorization.

# 16. Conclusion

This project demonstrates how an enterprise can connect Windows domain authentication to modern web applications through centralized Identity and Access Management.

By integrating Active Directory, Kerberos, LDAP, Keycloak and SAML 2.0, the solution provides a working Single Sign-On authentication chain.

The implementation demonstrates how an existing Windows domain identity can be reused to authenticate users to a protected ASP.NET Core application without requiring them to manually enter their credentials again.

The final validation confirmed that the domain user `bdupont` could access the protected PolyCorp profile through the complete authentication chain.

The project provided practical experience in enterprise identity integration, Kerberos authentication, directory federation, SAML protocols, service accounts, certificate management and application-level authorization.

---

**End of Technical Report**

**Project:** PolyCorp IAM Mini-Project 2  
**Author:** Oubey Nouira  
**Year:** 2026
