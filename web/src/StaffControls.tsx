import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useApi } from "./api/useApi";
import { useMe } from "./api/useMe";

const STATUSES = ["Open", "InProgress", "Resolved"];

export function StaffControls({
  ticketId,
  status,
}: {
  ticketId: string;
  status: string;
}) {
  const api = useApi();
  const queryClient = useQueryClient();
  const { isStaff } = useMe();

  const update = useMutation({
    mutationFn: (body: { status?: string; assignToMe?: boolean }) =>
      api(`/tickets/${ticketId}`, {
        method: "PATCH",
        body: JSON.stringify(body),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["ticket", ticketId] });
      queryClient.invalidateQueries({ queryKey: ["tickets"] });
    },
  });

  if (!isStaff) return null;

  return (
    <div className="staff-controls">
      <select
        value={status}
        onChange={(e) => update.mutate({ status: e.target.value })}
        disabled={update.isPending}
      >
        {STATUSES.map((s) => (
          <option key={s}>{s}</option>
        ))}
      </select>
      <button
        className="secondary"
        onClick={() => update.mutate({ assignToMe: true })}
        disabled={update.isPending}
      >
        Assign to me
      </button>
      {update.error && <p>Error: {update.error.message}</p>}
    </div>
  );
}
