import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useApi } from "./api/useApi";
import { useMe } from "./api/useMe";

export function CommentForm({ ticketId }: { ticketId: string }) {
  const api = useApi();
  const queryClient = useQueryClient();
  const { isStaff } = useMe();
  const [body, setBody] = useState("");
  const [isInternal, setIsInternal] = useState(false);

  const addComment = useMutation({
    mutationFn: () =>
      api(`/tickets/${ticketId}/comments`, {
        method: "POST",
        body: JSON.stringify({ body, isInternal }),
      }),
    onSuccess: () => {
      setBody("");
      setIsInternal(false);
      queryClient.invalidateQueries({ queryKey: ["ticket", ticketId] });
    },
  });

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        addComment.mutate();
      }}
    >
      <textarea
        value={body}
        onChange={(e) => setBody(e.target.value)}
        placeholder="Write a comment..."
        required
        maxLength={5000}
      />
      {isStaff && (
        <label>
          <input
            type="checkbox"
            checked={isInternal}
            onChange={(e) => setIsInternal(e.target.checked)}
          />
          Internal note (hidden from the customer)
        </label>
      )}
      <button type="submit" disabled={addComment.isPending}>
        Add comment
      </button>
      {addComment.error && <p>Error: {addComment.error.message}</p>}
    </form>
  );
}
