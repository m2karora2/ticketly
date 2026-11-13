import { useQuery } from "@tanstack/react-query";
import { useAuth } from "react-oidc-context";
import type { components } from "./api/schema";

//type Ticket = components["schemas"]["TicketResponse"];
type Paged = components["schemas"]["PagedResponseOfTicketResponse"];

export function TicketList() {
  const auth = useAuth();

  const { data, isPending, error } = useQuery({
    queryKey: ["tickets"],
    queryFn: async (): Promise<Paged> => {
      const res = await fetch("http://localhost:5057/tickets", {
        headers: { Authorization: `Bearer ${auth.user?.access_token}` },
      });
      if (!res.ok) throw new Error(`API returned ${res.status}`);
      return res.json();
    },
  });

  if (isPending) return <p>Loading tickets...</p>;
  if (error) return <p>Error: {error.message}</p>;

  return (
    <ul>
      {data.items.map((t) => (
        <li key={t.id}>
          {t.title} ({t.status}, {t.priority})
        </li>
      ))}
    </ul>
  );
}
