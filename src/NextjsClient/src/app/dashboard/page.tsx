import { auth, signOut } from "@/auth";
import { redirect } from "next/navigation";
import { fetchCurrentUser } from "@/lib/api";

export default async function DashboardPage() {
  const session = await auth();

  if (!session) {
    redirect("/login");
  }

  let apiUser: Record<string, unknown> | null = null;
  let apiError: string | null = null;

  if (session.accessToken) {
    try {
      apiUser = await fetchCurrentUser(session.accessToken);
    } catch (e) {
      apiError =
        e instanceof Error ? e.message : "Failed to fetch user from API";
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
          <pre
            style={{
              background: "#f4f4f4",
              padding: "1rem",
              borderRadius: "4px",
              overflow: "auto",
            }}
          >
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

      <form
        action={async () => {
          "use server";
          await signOut({ redirectTo: "/" });
        }}
      >
        <button type="submit" style={btnStyleDanger}>
          Sign Out
        </button>
      </form>
    </main>
  );
}

const cardStyle: React.CSSProperties = {
  border: "1px solid #ddd",
  borderRadius: "8px",
  padding: "1.5rem",
  marginBottom: "1.5rem",
};

const tdLabelStyle: React.CSSProperties = {
  fontWeight: "bold",
  paddingRight: "1rem",
  paddingBottom: "0.5rem",
  width: "120px",
};

const btnStyleDanger: React.CSSProperties = {
  padding: "0.75rem 1.5rem",
  background: "#e00",
  color: "white",
  border: "none",
  borderRadius: "4px",
  cursor: "pointer",
  fontSize: "1rem",
};
