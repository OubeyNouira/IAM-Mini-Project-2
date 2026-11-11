# PolyCorp IAM Mini-Project 2
## Passwordless Web Authentication Using Active Directory, Kerberos, Keycloak and SAML

## 1. Project Overview

**PolyCorp IAM Mini-Project 2** demonstrates how an organization can implement Single Sign-On (SSO) to provide seamless access to web applications using existing Windows Active Directory credentials.

The project connects Windows domain authentication with a centralized Identity and Access Management (IAM) solution. Users authenticate once through their Windows domain account and can subsequently access a protected web application without manually entering their credentials again.

The solution combines Kerberos authentication, LDAP federation, Keycloak and SAML 2.0.

### Main Objective

Implement a passwordless web authentication flow in which a domain user logged into a Windows workstation can access a web application through SSO.

## 2. Architecture

The laboratory environment consists of three main components:

| Component | Role |
|---|---|
| DC01 | Windows Server Domain Controller, Active Directory, DNS and Kerberos KDC |
| CL-01 | Windows 10 domain-joined workstation |
| Windows 11 Host | Runs Keycloak using Docker and hosts the ASP.NET Core SAML application |

### Authentication Architecture

```text
       Windows Domain User
          POLYCORP\bdupont
                 |
                 v
       Windows 10 - CL-01
                 |
                 v
       Active Directory / KDC
                 |
          Kerberos Ticket
                 |
                 v
              Keycloak
          LDAP Federation
          Kerberos / SPNEGO
                 |
           SAML Assertion
                 |
                 v
       PolyCorp.SamlSP
          ASP.NET Core
                 |
                 v
       Protected User Profile
          Welcome bdupont!
```

## 3. Technologies Used

### Identity and Infrastructure
- Microsoft Active Directory Domain Services
- Windows Server
- Windows 10
- Kerberos authentication
- LDAP
- DNS
- VMware Workstation

### Identity and Access Management
- Keycloak 26.8.2
- Docker Desktop
- SAML 2.0
- SPNEGO
- Kerberos keytab

### Application Development
- ASP.NET Core 8
- C#
- Sustainsys.Saml2 2.11.0
- Cookie Authentication
- HTTPS

## 4. Laboratory Environment

The Active Directory domain is:

`polycorp.test`

Important domain components:

| Component | Configuration |
|---|---|
| Domain | polycorp.test |
| Domain Controller | DC01 |
| Domain Client | CL-01 |
| Domain User | bdupont |
| Service Account | svc_keycloak |
| Kerberos Service Principal | HTTP/sso.polycorp.test |
| Keycloak URL | http://sso.polycorp.test:8180 |
| SAML Service Provider | https://sso.polycorp.test:5001 |

## 5. Implementation

### 5.1 Active Directory Configuration

Active Directory was configured with the `polycorp.test` domain.

A dedicated service account, `svc_keycloak`, was created for Keycloak's Kerberos integration.

The HTTP Service Principal Name (SPN) was registered:

`HTTP/sso.polycorp.test`

A Kerberos keytab was generated and configured for the Keycloak service.

AES encryption support was configured for the service account to ensure compatibility between the Kerberos ticket and the keytab.

### 5.2 Keycloak Deployment

Keycloak 26.8.2 was deployed using Docker Compose.

The instance provides centralized authentication and acts as the Identity Provider (IdP).

Persistent local storage was configured for the Keycloak database.

### 5.3 LDAP Federation

Keycloak was connected to Active Directory through LDAP.

The integration allows Keycloak to retrieve and synchronize domain users.

The LDAP configuration uses:
- Active Directory as the LDAP vendor.
- `sAMAccountName` as the username attribute.
- Read-only synchronization.
- Subtree user search.
- Kerberos integration enabled.

### 5.4 Kerberos SSO Integration

A dedicated browser authentication flow was configured in Keycloak.

The Kerberos execution was added as an alternative authentication mechanism.

The Windows client was configured to allow integrated authentication to the Keycloak hostname.

When the domain user accesses Keycloak from CL-01, the browser uses the existing Kerberos ticket instead of requesting the user's password again.

### 5.5 SAML Integration

Keycloak was configured as the SAML Identity Provider.

An ASP.NET Core application was configured as the SAML Service Provider using Sustainsys.Saml2.

The Service Provider metadata defines the following identifiers:

**Entity ID**

`https://sso.polycorp.test:5001/Saml2`

**Assertion Consumer Service (ACS)**

`https://sso.polycorp.test:5001/Saml2/Acs`

The Service Provider validates the SAML response and establishes an authenticated application session using cookie authentication.

### 5.6 ASP.NET Core Application

The application provides:

- A home page displaying authentication status.
- A login endpoint initiating SAML authentication.
- A protected user profile.
- User identity extraction from SAML claims.
- Cookie-based authentication after successful SAML validation.

The application retrieves the username from the SAML NameIdentifier claim.

## 6. Authentication Flow

The complete authentication process is:

1. The user logs into CL-01 using their Active Directory account.
2. Windows obtains the necessary Kerberos tickets.
3. The user accesses the PolyCorp web application.
4. The application redirects the browser to Keycloak for authentication.
5. Keycloak uses Kerberos/SPNEGO to authenticate the domain user.
6. Keycloak uses LDAP federation to associate the authenticated identity with the Active Directory user.
7. Keycloak issues a SAML assertion.
8. The browser sends the SAML response to the Service Provider's ACS endpoint.
9. ASP.NET Core validates the SAML response.
10. The application creates an authenticated session and displays the user's profile.

**Expected result:**

`Welcome bdupont!`

No additional username or password entry is required during the demonstrated SSO flow.

## 7. Security Considerations

The laboratory demonstrates several important IAM security practices:

- Dedicated service accounts for service authentication.
- Kerberos-based authentication instead of repeatedly entering passwords.
- AES-256 Kerberos ticket encryption.
- HTTPS communication between the workstation and the Service Provider.
- Trusted certificate configuration on the Windows client.
- LDAP read-only integration.
- Protected application endpoints.
- Cookie-based application sessions.
- Separation of public certificate material from private keys.

This project is a laboratory demonstration, not a production-ready deployment. Production environments should additionally implement trusted CA-issued certificates, secure database storage, hardened Keycloak configuration, signed SAML assertions, appropriate key management, and secure network configuration.

## 8. Testing and Validation

The following scenarios were successfully demonstrated:

| Test | Expected Result |
|---|---|
| Windows domain login | Successful authentication as bdupont |
| Kerberos ticket acquisition | HTTP service ticket obtained |
| Kerberos encryption | AES-256 |
| LDAP federation | Active Directory users synchronized |
| Keycloak browser SSO | Automatic authentication |
| SAML response processing | Successful validation |
| Application authorization | Protected profile accessible |
| User identity extraction | `bdupont` displayed |

## 9. Repository Structure

```text
PolyCorp-IAM-Mini-Project-2/
│
├── docs/
│   ├── technical-report/
│   └── screenshots/
│
├── PolyCorp.SamlSP/
│   └── ASP.NET Core Service Provider
│
├── docker-compose.yml
├── krb5.conf
├── saml-sp.cer
├── .gitignore
├── .gitattributes
└── README.md
```

Sensitive local configuration, private certificates, Kerberos keytabs, environment variables and Keycloak database files are excluded from version control.

## 10. Conclusion

This project demonstrates the integration of enterprise identity infrastructure with modern web application authentication.

By combining Active Directory, Kerberos, LDAP, Keycloak and SAML 2.0, the solution provides a centralized authentication mechanism and a passwordless SSO experience for domain users.

It illustrates how existing Windows domain identities can be reused to access web applications securely and transparently.

---

**Project:** PolyCorp IAM Mini-Project 2  
**Focus:** Identity and Access Management / Single Sign-On  
**Domain:** polycorp.test  
**Application:** PolyCorp.SamlSP  
**Status:** Core SSO authentication flow successfully demonstrated.
