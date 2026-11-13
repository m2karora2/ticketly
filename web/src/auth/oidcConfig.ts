export const oidcConfig = {
  authority: "http://localhost:8080/realms/ticketly",
  client_id: "ticketly-web",
  redirect_uri: window.location.origin + "/",
  scope: "openid profile email",
  onSigninCallback: () => {
    // after login, remove ?code=...&state=... from the address bar
    window.history.replaceState({}, document.title, window.location.pathname);
  },
};