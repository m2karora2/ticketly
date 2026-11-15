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

  if (isPending) return <p className="muted">Loading tickets...</p>;
  if (error) return <p>Error: {error.message}</p>;
  if (data.items.length === 0) return <p className="muted">No tickets yet.</p>;

  return (
    <ul className="tickets">
      {data.items.map((t) => (
        <li key={t.id}>
          <span className="ticket-title">{t.title}</span>
          <span className="badges">
            <span className={`badge status-${t.status}`}>{t.status}</span>
            <span className={`badge priority-${t.priority}`}>{t.priority}</span>
          </span>
        </li>
      ))}
    </ul>
  );
}
