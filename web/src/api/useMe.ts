import { useQuery } from "@tanstack/react-query";
import { useApi } from "./useApi";

type Me = { sub: string; username: string; roles: string[] };

export function useMe() {
  const api = useApi();
  const { data } = useQuery({
    queryKey: ["me"],
    queryFn: () => api<Me>("/me"),
  });
  const isStaff =
    !!data && (data.roles.includes("support") || data.roles.includes("admin"));
  return { me: data, isStaff };
}
