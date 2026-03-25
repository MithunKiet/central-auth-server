import { signIn } from "@/auth";

export default function LoginPage() {
  return (
    <main style={{ padding: "4rem", textAlign: "center" }}>
      <h1>Sign In</h1>
      <p>Click the button below to sign in with your SSO account.</p>
      <form
        action={async () => {
          "use server";
          await signIn("custom-sso", { redirectTo: "/dashboard" });
        }}
      >
        <button type="submit" style={btnStyle}>
          Sign In with SSO
        </button>
      </form>
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
