import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "react-oidc-context";

export function CreateTicketForm() {
  const auth = useAuth();
  const queryClient = useQueryClient();
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [priority, setPriority] = useState("Medium");

  const createTicket = useMutation({
    mutationFn: async () => {
      const res = await fetch("http://localhost:5057/tickets", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${auth.user?.access_token}`,
        },
        body: JSON.stringify({ title, description, priority }),
      });
      if (!res.ok) throw new Error(`API returned ${res.status}`);
      return res.json();
    },
    onSuccess: () => {
      setTitle("");
      setDescription("");
      setPriority("Medium");
      queryClient.invalidateQueries({ queryKey: ["tickets"] });
    },
  });

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        createTicket.mutate();
      }}
    >
      <h2>New ticket</h2>
      <input
        value={title}
        onChange={(e) => setTitle(e.target.value)}
        placeholder="Title"
        required
        maxLength={200}
      />
      <textarea
        value={description}
        onChange={(e) => setDescription(e.target.value)}
        placeholder="Description"
        required
        maxLength={5000}
      />
      <select value={priority} onChange={(e) => setPriority(e.target.value)}>
        <option>Low</option>
        <option>Medium</option>
        <option>High</option>
        <option>Urgent</option>
      </select>
      <button type="submit" disabled={createTicket.isPending}>
        Create
      </button>
      {createTicket.error && <p>Error: {createTicket.error.message}</p>}
    </form>
  );
}
