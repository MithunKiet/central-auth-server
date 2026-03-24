import Link from "next/link";
import { auth } from "@/auth";

export default async function HomePage() {
  const session = await auth();

  return (
    <main style={{ padding: "2rem", textAlign: "center" }}>
      <h1>Welcome to SSO Demo</h1>
      {session ? (
        <div>
          <p>Hello, {session.user?.name}!</p>
          <Link href="/dashboard">
            <button style={btnStyle}>Go to Dashboard</button>
          </Link>
        </div>
      ) : (
        <Link href="/login">
          <button style={btnStyle}>Login with SSO</button>
        </Link>
      )}
    </main>
  );
}

const btnStyle: React.CSSProperties = {
  padding: "0.75rem 1.5rem",
  background: "#0070f3",
  color: "white",
  border: "none",
  borderRadius: "4px",
  cursor: "pointer",
  fontSize: "1rem",
};
