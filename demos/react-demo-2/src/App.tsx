import { useEffect, useState } from "react";
import { auth } from "./auth";

export default function App() {
  const [message, setMessage] = useState("Autentikoidaan...");

  useEffect(() => {
    async function authenticate() {
      try {
        if (window.location.pathname === "/callback") {
          await auth.signinRedirectCallback();
          setMessage("Autentikointi onnistui!");
        } else {
          await auth.signinRedirect();
        }
      } catch (error) {
        setMessage(`Autentikointi epäonnistui: ${error}`);
      }
    }

    authenticate();
  }, []);

  return <h1>{message}</h1>;
}