import { useState } from "react";
import { useAuth } from "react-oidc-context";
import { CreateTicketForm } from "./CreateTicketForm";
import { TicketList } from "./TicketList";

function App() {
  const auth = useAuth();
  const [apiResult, setApiResult] = useState("");

  async function callApi() {
    const res = await fetch("http://localhost:5057/me", {
      headers: { Authorization: `Bearer ${auth.user?.access_token}` },
    });
    setApiResult(`${res.status} ${await res.text()}`);
  }

  if (auth.isLoading)
    return (
      <main>
        <p className="muted">Loading...</p>
      </main>
    );
  if (auth.error)
    return (
      <main>
        <p>Auth error: {auth.error.message}</p>
      </main>
    );

  if (!auth.isAuthenticated) {
    return (
      <main className="login">
        <h1>Ticketly</h1>
        <p>Support helpdesk</p>
        <button onClick={() => auth.signinRedirect()}>Log in</button>
      </main>
    );
  }

  return (
    <main>
      <header className="topbar">
        <h1>Ticketly</h1>
        <div className="user">
          <span>{auth.user?.profile.preferred_username}</span>
          <button className="secondary" onClick={() => auth.removeUser()}>
            Log out
          </button>
        </div>
      </header>

      <section className="card">
        <CreateTicketForm />
      </section>

      <section className="card">
        <h2>Tickets</h2>
        <TicketList />
      </section>

      <details className="card">
        <summary>Debug</summary>
        <button className="secondary" onClick={callApi}>
          Call API /me
        </button>
        <pre>{apiResult}</pre>
      </details>
    </main>
  );
}

export default App;
