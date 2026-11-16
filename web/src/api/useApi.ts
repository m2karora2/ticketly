import { useAuth } from "react-oidc-context";

const API_URL = "http://localhost:5057";

export function useApi() {
  const auth = useAuth();
  return async function api<T>(
    path: string,
    init: RequestInit = {},
  ): Promise<T> {
    const res = await fetch(`${API_URL}${path}`, {
      ...init,
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${auth.user?.access_token}`,
        ...init.headers,
      },
    });
    if (!res.ok) throw new Error(`API returned ${res.status}`);
    return res.json() as Promise<T>;
  };
}
