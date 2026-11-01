import { useEffect, useState } from "react";
import { auth } from "./auth";
import { decodeJwt } from "./jwt";

export default function Roles() {
  const [roles, setRoles] = useState<string[]>([]);
  const [error, setError] = useState("");

  useEffect(() => {
    auth
      .getUser()
      .then((user) => {
        if (!user) throw new Error("Not signed in");
        const claims = decodeJwt(user.access_token);
        setRoles([claims.role ?? []].flat());
      })
      .catch((e) => setError(String(e)));
  }, []);

  return (
    <div>
      <h2>Roolit tässä yrityksessä</h2>
      {error ? <p>Virhe: {error}</p> : <p>{roles.join(", ") || "(ei rooleja)"}</p>}
    </div>
  );
}
