import { auth } from "@/auth";
import { NextResponse } from "next/server";

export default auth((req) => {
  const { nextUrl } = req;
  const session = req.auth;
  const isLoggedIn = !!session;
  const isOnDashboard = nextUrl.pathname.startsWith("/dashboard");

  // If the user hits a protected route and either isn't logged in OR their token couldn't be refreshed
  if (isOnDashboard) {
    if (!isLoggedIn || session?.error === "RefreshAccessTokenError") {
      return NextResponse.redirect(new URL("/login", nextUrl));
    }
  }

  return NextResponse.next();
});

export const config = {
  matcher: ["/((?!api|_next/static|_next/image|favicon.ico).*)"],
};
