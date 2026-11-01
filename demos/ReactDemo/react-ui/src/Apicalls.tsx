import { useEffect, useState } from "react";
import { auth } from "./auth";

type CallResult = { ok: boolean | null; text: string };

const pending: CallResult = { ok: null, text: "..." };

async function callApi(url: string, token: string): Promise<CallResult> {
  const response = await fetch(url, { headers: { Authorization: `Bearer ${token}` } });
  if (response.ok) return { ok: true, text: JSON.stringify(await response.json()) };
  return { ok: false, text: `HTTP ${response.status}` };
}

const mark = (r: CallResult) => (r.ok === null ? "…" : r.ok ? "✔" : "✖");

export default function ApiCalls() {
  const [api, setApi] = useState<CallResult>(pending);
  const [manager, setManager] = useState<CallResult>(pending);
  const [worker, setWorker] = useState<CallResult>(pending);
  const [error, setError] = useState("");

  useEffect(() => {
    async function load() {
      try {
        const user = await auth.getUser();
        if (!user) throw new Error("Not signed in");

        setApi(await callApi("/hello-api", user.access_token));
        setManager(await callApi("/hello-manager", user.access_token));
        setWorker(await callApi("/hello-worker", user.access_token));
      } catch (e) {
        setError(String(e));
      }
    }
    load();
  }, []);

  return (
    <div>
      <h2>API-kutsut</h2>
      {error && <p>Virhe: {error}</p>}
      <ul>
        <li>{mark(api)} /hello-api: {api.text}</li>
        <li>{mark(manager)} /hello-manager: {manager.text}</li>
        <li>{mark(worker)} /hello-worker: {worker.text}</li>
      </ul>
    </div>
  );
}
