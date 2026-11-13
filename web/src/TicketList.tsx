import { useQuery } from "@tanstack/react-query";
import { useAuth } from "react-oidc-context";

type Ticket = { id: string; title: string; status: string; priority: string };
type Paged<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export function TicketList() {
  const auth = useAuth();

  const { data, isPending, error } = useQuery({
    queryKey: ["tickets"],
    queryFn: async (): Promise<Paged<Ticket>> => {
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
