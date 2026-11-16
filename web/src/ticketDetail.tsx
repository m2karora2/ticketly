import { useQuery } from "@tanstack/react-query";
import type { components } from "./api/schema";
import { useApi } from "./api/useApi";
import { CommentForm } from "./CommentForm";
import { StaffControls } from "./StaffControls";

type TicketDetailDto = components["schemas"]["TicketDetailResponse"];

export function TicketDetail({ id }: { id: string }) {
  const api = useApi();
  const { data, isPending, error } = useQuery({
    queryKey: ["ticket", id],
    queryFn: () => api<TicketDetailDto>(`/tickets/${id}`),
  });

  if (isPending) return <p className="muted">Loading ticket...</p>;
  if (error) return <p>Error: {error.message}</p>;

  return (
    <div>
      <h2>{data.title}</h2>
      <div className="badges">
        <span className={`badge status-${data.status}`}>{data.status}</span>
        <span className={`badge priority-${data.priority}`}>
          {data.priority}
        </span>
      </div>
      <p>{data.description}</p>
      <p className="muted">Assignee: {data.assigneeName ?? "unassigned"}</p>

      <h2>Comments</h2>
      {data.comments.length === 0 && <p className="muted">No comments yet.</p>}
      <ul className="comments">
        {data.comments.map((c) => (
          <li key={c.id} className={c.isInternal ? "internal" : ""}>
            <div className="muted">
              {c.authorName || c.authorId} ·{" "}
              {new Date(c.createdAt).toLocaleString()}
              {c.isInternal && " · internal note"}
            </div>
            <div>{c.body}</div>
          </li>
        ))}
      </ul>
      <StaffControls ticketId={id} status={data.status} />
      <CommentForm ticketId={id} />
    </div>
  );
}
