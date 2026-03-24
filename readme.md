# Central Auth Server

A production-ready **Single Sign-On (SSO) and Authorization System** built on OAuth 2.0 / OpenID Connect (OIDC) standards. It consists of three components: a central **Auth Server**, a **Resource API**, and a **Next.js Client** application.

---

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Technology Stack](#technology-stack)
- [Prerequisites](#prerequisites)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
  - [1. AuthServer Setup](#1-authserver-setup)
  - [2. ResourceApi Setup](#2-resourceapi-setup)
  - [3. NextjsClient Setup](#3-nextjsclient-setup)
- [Running the Project](#running-the-project)
- [Default Seed Credentials](#default-seed-credentials)
- [Configuration Reference](#configuration-reference)
- [API Endpoints](#api-endpoints)
- [Authentication Flows](#authentication-flows)
- [Security Features](#security-features)

---

## Overview

**Central Auth Server** is a centralized authentication and authorization hub that allows multiple client applications to delegate identity management to a single trusted provider.

Key capabilities:
- OAuth 2.0 Authorization Code Flow with PKCE
- Client Credentials Flow for machine-to-machine (M2M) communication
- Refresh Token support
- OpenID Connect (OIDC) compliant UserInfo and token endpoints
- Role-based access control (RBAC) with custom claims
- User registration, login, logout, and password management
- JWT access tokens validated by downstream APIs
- Multi-client support (web apps, SPAs, M2M services)

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        Clients                              │
│                                                             │
│   ┌──────────────────┐        ┌──────────────────────┐     │
│   │  NextjsClient    │        │  Postman / Other     │     │
│   │  (Next.js 15)    │        │  OIDC Clients        │     │
│   │  :3000           │        │                      │     │
│   └────────┬─────────┘        └──────────┬───────────┘     │
└────────────┼──────────────────────────────┼─────────────────┘
             │ OIDC / Auth Code + PKCE      │ OAuth 2.0
             ▼                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      AuthServer                             │
│              (ASP.NET Core 10 + OpenIddict)                 │
│                      :5001 / :5000                          │
│                                                             │
│  /connect/authorize   /connect/token   /connect/userinfo    │
│  /account/login       /account/register  /account/logout    │
└──────────────────────────────┬──────────────────────────────┘
                               │ JWT Bearer Token
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                      ResourceApi                            │
│              (ASP.NET Core 10 + JWT Bearer)                 │
│                      :5002 / :5003                          │
│                                                             │
│                    GET /api/user/me                         │
└─────────────────────────────────────────────────────────────┘
```

---

## Technology Stack

| Component      | Technology                                                              |
|----------------|-------------------------------------------------------------------------|
| AuthServer     | ASP.NET Core 10, OpenIddict 7.4, ASP.NET Identity, EF Core 10, Serilog |
| ResourceApi    | ASP.NET Core 10, JWT Bearer Authentication                              |
| NextjsClient   | Next.js 15, React 19, NextAuth.js v5 (Auth.js), TypeScript             |
| Database       | SQL Server / LocalDB (development)                                      |

---

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server) or SQL Server LocalDB (included with Visual Studio)
- [Node.js 18+](https://nodejs.org/) and npm
- [OpenSSL](https://www.openssl.org/) (for generating secrets)

---

## Project Structure

```
central-auth-server/
├── readme.md
├── CentralAuthServer.slnx          # .NET solution file
├── .gitignore
└── src/
    ├── AuthServer/                 # OIDC / OAuth 2.0 provider
    │   ├── Controllers/
    │   │   ├── AuthorizationController.cs  # /connect/* endpoints
    │   │   ├── AccountController.cs        # Login, register, logout
    │   │   └── HomeController.cs
    │   ├── Data/
    │   │   ├── ApplicationDbContext.cs
    │   │   └── SeedData.cs                 # Seeds test users and clients
    │   ├── Entities/
    │   │   ├── ApplicationUser.cs          # Extended Identity user
    │   │   └── ApplicationRole.cs
    │   ├── Models/                         # View models
    │   ├── Services/
    │   │   └── ClaimsService.cs
    │   ├── Extensions/
    │   │   └── ServiceExtensions.cs        # DI / service registration
    │   ├── Views/                          # Razor views
    │   ├── appsettings.json
    │   └── appsettings.Development.json
    │
    ├── ResourceApi/                # Protected downstream API
    │   ├── Controllers/
    │   │   └── UserController.cs           # GET /api/user/me
    │   ├── appsettings.json
    │   └── appsettings.Development.json
    │
    └── NextjsClient/               # Next.js web application
        ├── src/
        │   ├── auth.ts                     # NextAuth.js / OIDC config
        │   ├── middleware.ts               # Route protection
        │   ├── lib/api.ts                  # API client helper
        │   └── app/
        │       ├── page.tsx                # Home page
        │       ├── login/page.tsx          # Login page
        │       ├── dashboard/page.tsx      # Protected dashboard
        │       └── api/auth/[...nextauth]/ # NextAuth.js API routes
        ├── .env.local.example
        └── package.json
```

---

## Getting Started

### 1. AuthServer Setup

```bash
cd src/AuthServer
dotnet restore
dotnet build
```

Create or update `appsettings.Development.json` with your seed credentials:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CentralAuthServer;Trusted_Connection=True;"
  },
  "SeedData": {
    "AdminPassword": "Admin@12345",
    "UserPassword": "User@12345",
    "WebClientSecret": "<your-web-client-secret>",
    "M2MClientSecret": "<your-m2m-client-secret>"
  },
  "Urls": "https://localhost:5001;http://localhost:5000"
}
```

The database is created and migrated automatically on first run.

---

### 2. ResourceApi Setup

```bash
cd src/ResourceApi
dotnet restore
dotnet build
```

Verify `appsettings.json` matches your AuthServer URL:

```json
{
  "Auth": {
    "Authority": "https://localhost:5001",
    "Audience": "api"
  },
  "Cors": {
    "AllowedOrigins": "http://localhost:3000,https://localhost:3000"
  },
  "Urls": "https://localhost:5002;http://localhost:5003"
}
```

---

### 3. NextjsClient Setup

```bash
cd src/NextjsClient
npm install
```

Copy the example environment file and fill in your values:

```bash
cp .env.local.example .env.local
```

`.env.local` variables:

```env
# Generate with: openssl rand -base64 32
AUTH_SECRET=<your-secret>

AUTH_ISSUER=https://localhost:5001
AUTH_CLIENT_ID=nextjs-client

# ResourceApi base URL
NEXT_PUBLIC_API_URL=https://localhost:5002

NEXTAUTH_URL=http://localhost:3000
```

---

## Running the Project

Start all three services (each in a separate terminal):

**Terminal 1 – AuthServer**
```bash
cd src/AuthServer
dotnet run
# Available at https://localhost:5001
```

**Terminal 2 – ResourceApi**
```bash
cd src/ResourceApi
dotnet run
# Available at https://localhost:5002
```

**Terminal 3 – NextjsClient**
```bash
cd src/NextjsClient
npm run dev
# Available at http://localhost:3000
```

Open your browser at [http://localhost:3000](http://localhost:3000) and log in with the seed credentials below.

---

## Default Seed Credentials

These users are automatically seeded into the database on first run.

| Role  | Username   | Email                    | Default Password |
|-------|------------|--------------------------|------------------|
| Admin | `admin`    | `admin@sso.local`        | `Admin@12345`    |
| User  | `testuser` | `user@authserver.local`  | `User@12345`     |

> **Note:** Change these passwords in production via `appsettings.Development.json` and the `SeedData` configuration section.

### Pre-registered OAuth Clients

| Client ID        | Type          | Grant Types                       | Redirect URI                                         |
|------------------|---------------|-----------------------------------|------------------------------------------------------|
| `web-client`     | Confidential  | Authorization Code, Refresh Token | `https://localhost:5001/signin-oidc`                 |
| `m2m-client`     | Confidential  | Client Credentials                | —                                                    |
| `spa-client`     | Public (PKCE) | Authorization Code, Refresh Token | `https://localhost:3000/callback`                    |
| `nextjs-client`  | Public (PKCE) | Authorization Code, Refresh Token | `http://localhost:3000/api/auth/callback/custom-sso` |

---

## Configuration Reference

### AuthServer — appsettings

| Key | Description |
|-----|-------------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `SeedData:AdminPassword` | Password for the seeded admin user |
| `SeedData:UserPassword` | Password for the seeded regular user |
| `SeedData:WebClientSecret` | Secret for the `web-client` OAuth client |
| `SeedData:M2MClientSecret` | Secret for the `m2m-client` OAuth client |

### ResourceApi — appsettings

| Key | Description |
|-----|-------------|
| `Auth:Authority` | AuthServer base URL for token validation |
| `Auth:Audience` | Expected audience claim in JWT (e.g. `api`) |
| `Cors:AllowedOrigins` | Comma-separated list of allowed CORS origins |

### NextjsClient — .env.local

| Variable | Description |
|----------|-------------|
| `AUTH_SECRET` | Secret key for NextAuth.js session encryption |
| `AUTH_ISSUER` | AuthServer OIDC issuer URL |
| `AUTH_CLIENT_ID` | OAuth client ID registered in AuthServer |
| `NEXT_PUBLIC_API_URL` | Base URL of ResourceApi |
| `NEXTAUTH_URL` | Public URL of the Next.js client |

---

## API Endpoints

### AuthServer – OIDC Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/connect/authorize` | GET, POST | Authorization endpoint (start OAuth flow) |
| `/connect/token` | POST | Token endpoint (exchange code for tokens) |
| `/connect/userinfo` | GET, POST | UserInfo endpoint (returns user claims) |
| `/connect/logout` | GET, POST | End session / logout |
| `/connect/introspect` | POST | Token introspection |

### AuthServer – Account Endpoints

| Endpoint | Method | Auth Required | Description |
|----------|--------|---------------|-------------|
| `/account/login` | GET, POST | No | User login form |
| `/account/register` | GET, POST | No | User registration |
| `/account/logout` | GET, POST | Yes | Sign out |
| `/account/change-password` | GET, POST | Yes | Change password |
| `/account/access-denied` | GET | No | Access denied page |

### ResourceApi – Protected Endpoints

| Endpoint | Method | Auth Required | Description |
|----------|--------|---------------|-------------|
| `/api/user/me` | GET | Yes (Bearer JWT) | Returns current user's info and claims |

### NextjsClient – Application Routes

| Route | Auth Required | Description |
|-------|---------------|-------------|
| `/` | No | Home page |
| `/login` | No | Login page (redirects to dashboard if already authenticated) |
| `/dashboard` | Yes | Protected dashboard (redirects to login if unauthenticated) |
| `/api/auth/[...nextauth]` | — | NextAuth.js dynamic API routes |

---

## Authentication Flows

### Authorization Code Flow with PKCE (SPAs / Next.js)

```
Browser         NextjsClient          AuthServer            ResourceApi
  │                  │                     │                     │
  │─ GET /dashboard ─►│                     │                     │
  │                  │─ redirect ──────────►│                     │
  │◄─ GET /account/login ──────────────────│                     │
  │─ POST credentials ────────────────────►│                     │
  │◄─ redirect with auth code ─────────────│                     │
  │─ GET /api/auth/callback ───────────────►│                     │
  │                  │─ POST /connect/token ►│                     │
  │                  │◄─ access + id token ──│                     │
  │◄─ session cookie ─│                     │                     │
  │─ GET /api/user/me ──────────────────────────────────────────►│
  │◄─ user info ────────────────────────────────────────────────│
```

### Client Credentials Flow (M2M)

```
Backend Service         AuthServer
       │                     │
       │─ POST /connect/token ►│
       │  (client_id + secret) │
       │◄─ access token ───────│
       │─ GET /api/... ─────────────────────► ResourceApi
```

### Token Lifetimes

| Token | Lifetime |
|-------|----------|
| Access Token | 60 minutes |
| ID Token | 60 minutes |
| Refresh Token | 14 days |
| Session Cookie | 8 hours (sliding) |

### Supported Scopes

`openid` · `profile` · `email` · `offline_access` · `roles` · `api` · `phone`

---

## Security Features

- **PKCE** (Proof Key for Code Exchange) enforced for all public clients
- **JWT signing and encryption** for all tokens
- **Password policy**: minimum 8 characters, requires uppercase, lowercase, digit, and special character
- **Account lockout**: 5 failed login attempts triggers a 15-minute lockout
- **CSRF protection** via ASP.NET Core antiforgery tokens
- **HTTPS redirection** enforced in production
- **Audience and issuer validation** on all JWT tokens with a 30-second clock skew tolerance
- **Structured logging** via Serilog (console + rolling file output)
