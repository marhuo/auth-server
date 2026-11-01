import { auth } from "./auth";

export default function SignOutButton() {
  return <div><button onClick={() => auth.signoutRedirect()}>Kirjaudu ulos</button></div>;
}
