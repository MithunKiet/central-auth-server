import { auth, signOut } from "@/auth";
import { redirect } from "next/navigation";
import { fetchCurrentUser } from "@/lib/api";

export default async function DashboardPage() {
  const session = await auth();

  // Guard against expired Auth sessions
  if (!session || session.error === "RefreshAccessTokenError") {
    redirect("/login");
  }

  let apiUser: Record<string, unknown> | null = null;
  let apiError: string | null = null;

  if (session.accessToken) {
    try {
      apiUser = await fetchCurrentUser(session.accessToken);
    } catch (e) {
      apiError = e instanceof Error ? e.message : "Failed to fetch user from API";
    }
  }

  return (
    <main style={{ padding: "2rem", maxWidth: "800px", margin: "0 auto" }}>
      <h1>Dashboard</h1>
      
      <section style={cardStyle}>
        <h2>Session Info</h2>
        <table style={{ width: "100%", borderCollapse: "collapse" }}>
          <tbody>
            <tr>
              <td style={tdLabelStyle}>Name</td>
              <td>{session.user?.name}</td>
            </tr>
            <tr>
              <td style={tdLabelStyle}>Email</td>
              <td>{session.user?.email}</td>
            </tr>
            <tr>
              <td style={tdLabelStyle}>User ID</td>
              <td>{session.user?.userId}</td>
            </tr>
            <tr>
              <td style={tdLabelStyle}>Roles</td>
              <td>{(session.user?.roles ?? []).join(", ") || "—"}</td>
            </tr>
            <tr>
              <td style={tdLabelStyle}>Department</td>
              <td>{session.user?.department ?? "—"}</td>
            </tr>
          </tbody>
        </table>
      </section>

      {apiUser && (
        <section style={cardStyle}>
          <h2>Protected API Response (GET /api/user/me)</h2>
          <pre style={{ background: "#f4f4f4", padding: "1rem", borderRadius: "4px", overflow: "auto", }}>
            {JSON.stringify(apiUser, null, 2)}
          </pre>
        </section>
      )}

      {apiError && (
        <section style={{ ...cardStyle, borderColor: "#e00" }}>
          <h2>API Error</h2>
          <p style={{ color: "#e00" }}>{apiError}</p>
        </section>
      )}

      <form action={async () => { 
        "use server"; 
        const issuer = process.env.AUTH_ISSUER ?? "https://localhost:5001";
        
        // OpenIddict requires EXACT string matching for redirect URIs, including trailing slashes.
        // SeedData.cs registered: new Uri("http://localhost:3000/")
        let postLogoutUrl = process.env.AUTH_URL ?? "http://localhost:3000";
        if (!postLogoutUrl.endsWith("/")) postLogoutUrl += "/";
        
        const idToken = session?.idToken ?? "";
        
        // This clears the local NextAuth session and then issues a 302 Redirect to the specified URL
        // Which guarantees the federated logout process starts at the SSO server.
        const url = `${issuer}/connect/logout?id_token_hint=${idToken}&post_logout_redirect_uri=${encodeURIComponent(postLogoutUrl)}`;
        await signOut({ redirectTo: url }); 
      }}>  <button type="submit" style={btnStyleDanger}>Sign Out</button>
      </form>
    </main>
  );
}

const cardStyle: React.CSSProperties = { border: "1px solid #ddd", borderRadius: "8px", padding: "1.5rem", marginBottom: "1.5rem", };
const tdLabelStyle: React.CSSProperties = { fontWeight: "bold", paddingRight: "1rem", paddingBottom: "0.5rem", width: "120px", };
const btnStyleDanger: React.CSSProperties = { padding: "0.75rem 1.5rem", background: "#e00", color: "white", border: "none", borderRadius: "4px", cursor: "pointer", fontSize: "1rem", };
