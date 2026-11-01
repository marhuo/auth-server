//App.tsx
//App.tsx
import { useEffect, useRef, useState } from "react";
import { ErrorResponse } from "oidc-client-ts";
import { auth } from "./auth";
import TenantSelect from "./TenantSelect";
import SignOutButton from "./SignoutButton";
import Roles from "./Roles";
import ApiCalls from "./Apicalls";

type State = "authenticating" | "authenticated" | "error";

export default function App() {
  const [state, setState] = useState<State>("authenticating");
  const [message, setMessage] = useState("");
  const started = useRef(false);

  useEffect(() => {
    // Estää kaksoisajon React StrictModessa
    if (started.current) return;
    started.current = true;

    async function authenticate() {
      try {
        // Paluu auth-palvelimelta
        if (window.location.pathname === "/callback") {
          await auth.signinRedirectCallback();
          window.history.replaceState({}, document.title, "/");
          setState("authenticated");
          return;
        }

        // Onko käyttäjä jo kirjautunut?
        const user = await auth.getUser();
        if (user && !user.expired) {
          setState("authenticated");
          return;
        }

        // Ei kirjautunut, joten ohjataan auth-palvelimelle
        await auth.signinRedirect();
      } catch (error) {
        console.error(error);
        if (error instanceof ErrorResponse) {
          setMessage(`${error.error}: ${error.error_description}`);
        } else {
          setMessage(error instanceof Error ? error.message : String(error));
        }
        setState("error");
      }
    }

    authenticate();
  }, []);

  if (state === "authenticated") return (<div style={{display: "flex", flexDirection: "column", gap: "16px"}}>
        <TenantSelect />  
        <Roles />
        <ApiCalls />
        <SignOutButton />
    </div>);
  if (state === "error") {
    return (
      <>
        <h1>Autentikointi epäonnistui</h1>
        <pre>{message}</pre>
      </>
    );
  }
  return <h1>Autentikointi käynnissä...</h1>;
}