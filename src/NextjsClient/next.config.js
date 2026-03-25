// Allow self-signed certificates issued by the .NET dev HTTPS tool.
// This ONLY applies in development — never set this in production.
if (process.env.NODE_ENV === "development") {
  process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0";
}

/** @type {import('next').NextConfig} */
const nextConfig = {
  experimental: {},
};

export default nextConfig;
