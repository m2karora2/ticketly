import { useState } from "react";
import { useAuth } from "react-oidc-context";

function App() {
  const auth = useAuth();
  const [apiResult, setApiResult] = useState("");

  async function callApi() {
    const res = await fetch("http://localhost:5057/me", {
      headers: { Authorization: `Bearer ${auth.user?.access_token}` },
    });
    setApiResult(`${res.status} ${await res.text()}`);
  }

  if (auth.isLoading) return <p>Loading...</p>;
  if (auth.error) return <p>Auth error: {auth.error.message}</p>;

  if (!auth.isAuthenticated) {
    return (
      <div>
        <h1>Ticketly</h1>
        <button onClick={() => auth.signinRedirect()}>Log in</button>
      </div>
    );
  }

  return (
    <div>
      <h1>Ticketly</h1>
      <p>Logged in as {auth.user?.profile.preferred_username}</p>
      <button onClick={() => auth.removeUser()}>Log out</button>
      <button onClick={callApi}>Call API /me</button>
      <pre>{apiResult}</pre>
    </div>
  );
}

export default App;
