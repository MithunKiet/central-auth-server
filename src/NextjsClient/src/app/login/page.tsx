"use client";

import { signIn } from "next-auth/react";

export default function LoginPage() {
  return (
    <main style={{ padding: "4rem", textAlign: "center" }}>
      <h1>Sign In</h1>
      <p>Click the button below to sign in with your SSO account.</p>
      <button
        onClick={() => signIn("custom-sso", { callbackUrl: "/dashboard" })}
        style={btnStyle}
      >
        Sign In with SSO
      </button>
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
