import NextAuth from "next-auth";
import type { NextAuthConfig } from "next-auth";

export const config: NextAuthConfig = {
  providers: [
    {
      id: "custom-sso",
      name: "Custom SSO",
      type: "oidc",
      issuer: process.env.AUTH_ISSUER ?? "https://localhost:5001",
      clientId: process.env.AUTH_CLIENT_ID ?? "nextjs-client",
      // Public client - no client secret (uses PKCE)
      clientSecret: process.env.AUTH_CLIENT_SECRET,
      authorization: {
        params: {
          scope: "openid profile email offline_access roles api",
          response_type: "code",
        },
      },
      checks: ["pkce", "state"],
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
  session: {
    strategy: "jwt",
  },
  callbacks: {
    async jwt({ token, account, profile }) {
      if (account) {
        token.accessToken = account.access_token;
        token.refreshToken = account.refresh_token;
        token.idToken = account.id_token;
        token.expiresAt = account.expires_at;
      }
      if (profile) {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const p = profile as any;
        token.roles = p.role ?? [];
        token.department = p.department;
        token.userId = p.sub;
      }
      return token;
    },
    async session({ session, token }) {
      session.accessToken = token.accessToken as string | undefined;
      session.refreshToken = token.refreshToken as string | undefined;
      session.user.roles = token.roles as string[] | undefined;
      session.user.department = token.department as string | undefined;
      session.user.userId = token.userId as string | undefined;
      return session;
    },
  },
  pages: {
    signIn: "/login",
  },
  trustHost: true,
};

export const { handlers, signIn, signOut, auth } = NextAuth(config);
