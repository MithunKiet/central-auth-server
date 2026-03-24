export async function fetchCurrentUser(accessToken: string) {
  const apiBaseUrl =
    process.env.NEXT_PUBLIC_API_URL ?? "https://localhost:5002";
  const response = await fetch(`${apiBaseUrl}/api/user/me`, {
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
    cache: "no-store",
  });

  if (!response.ok) {
    throw new Error(`API error: ${response.status}`);
  }

  return response.json();
}
