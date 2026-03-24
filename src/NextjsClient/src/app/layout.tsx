import type { Metadata } from "next";
import { SessionProvider } from "next-auth/react";

export const metadata: Metadata = {
  title: "SSO Client App",
  description: "Next.js client with custom SSO",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body
        style={{ fontFamily: "system-ui, sans-serif", margin: 0, padding: 0 }}
      >
        <SessionProvider>{children}</SessionProvider>
      </body>
    </html>
  );
}
