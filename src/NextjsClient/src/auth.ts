import NextAuth from "next-auth";
import type { NextAuthConfig, DefaultSession } from "next-auth";

// 1. TypeScript Module Augmentation for custom attributes
declare module "next-auth" {
  interface Session {
    error?: string;
    accessToken?: string;
    refreshToken?: string;
    user: {
      userId?: string;
      roles?: string[];
      department?: string;
    } & DefaultSession["user"];
  }
}

declare module "next-auth/jwt" {
  interface JWT {
    accessToken?: string;
    refreshToken?: string;
    idToken?: string;
    expiresAt?: number;
    roles?: string[];
    department?: string;
    userId?: string;
    error?: string;
  }
}

export const config: NextAuthConfig = {
  providers: [
    {
      id: "custom-sso",
      name: "Custom SSO",
      type: "oidc",
      issuer: process.env.AUTH_ISSUER ?? "https://localhost:5001",
      clientId: process.env.AUTH_CLIENT_ID ?? "nextjs-client",
      client: {
        // CRITICAL: Tells oauth4webapi this is a public client requiring no client secret
        token_endpoint_auth_method: "none", 
      },
      authorization: {
        params: {
          scope: "openid profile email offline_access roles api",
          response_type: "code",
        },
      },
      checks: ["pkce"], // Explicitly use PKCE only via oauth4webapi
      profile(profile) {
        return {
          id: profile.sub,
          name:
            profile.name ??
            `${profile.given_name ?? ""} ${profile.family_name ?? ""}`.trim(),
          email: profile.email,
          image: null,
        };
      },
    },
  ],
  session: { strategy: "jwt" },
  callbacks: {
    async redirect({ url, baseUrl }) {
      // Allows redirecting to the AuthServer for federated logout
      const issuer = process.env.AUTH_ISSUER ?? "https://localhost:5001";
      if (url.startsWith(issuer)) {
        return url;
      }
      // Default behavior
      if (url.startsWith("/")) return new URL(url, baseUrl).toString();
      else if (new URL(url).origin === baseUrl) return url;
      return baseUrl;
    },
    async jwt({ token, account, profile }) {
      // 1. Initial sign-in
      if (account && profile) {
        // Handle ASP.NET Core Single vs Array Claim serialization issue
        const rolesClaim = (profile as any).role;
        const normalizedRoles = Array.isArray(rolesClaim) ? rolesClaim : (rolesClaim ? [rolesClaim] : []);

        return {
          ...token,
          accessToken: account.access_token,
          refreshToken: account.refresh_token,
          idToken: account.id_token,
          expiresAt: account.expires_at, // Provided as Unix Timestamp by ASP.NET Core
          roles: normalizedRoles,
          department: (profile as any).department,
          userId: profile.sub,
        };
      }

      // 2. Return previous token if the access token has not expired yet
      // (Add 60 seconds buffer before actual expiration)
      if (token.expiresAt && Date.now() < (token.expiresAt as number) * 1000 - 60000) {
        return token;
      }

      // 3. Access token has expired, refresh it using OpenIddict Token Endpoint
      try {
        const response = await fetch(`${process.env.AUTH_ISSUER ?? "https://localhost:5001"}/connect/token`, {
          headers: { "Content-Type": "application/x-www-form-urlencoded" },
          body: new URLSearchParams({
            client_id: process.env.AUTH_CLIENT_ID ?? "nextjs-client",
            grant_type: "refresh_token",
            refresh_token: token.refreshToken as string,
          }),
          method: "POST",
        });

        const refreshTokens = await response.json();

        if (!response.ok) throw refreshTokens;

        return {
          ...token,
          accessToken: refreshTokens.access_token,
          expiresAt: Math.floor(Date.now() / 1000) + refreshTokens.expires_in,
          // Fall back to old refresh token if OpenIddict does not cycle it
          refreshToken: refreshTokens.refresh_token ?? token.refreshToken,
        };
      } catch (error) {
        console.error("Error refreshing access token", error);
        return { ...token, error: "RefreshAccessTokenError" as const };
      }
    },
    async session({ session, token }) {
      if (token.error) {
        session.error = token.error as string;
      }
      session.accessToken = token.accessToken as string | undefined;
      session.refreshToken = token.refreshToken as string | undefined;
      session.idToken = token.idToken as string | undefined;
      session.user.roles = token.roles as string[] | undefined;
      session.user.department = token.department as string | undefined;
      session.user.userId = token.userId as string | undefined;
      return session;
    },
  },
  pages: { signIn: "/login" },
  trustHost: true,
};

export const { handlers, signIn, signOut, auth } = NextAuth(config);
