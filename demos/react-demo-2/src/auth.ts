import { UserManager } from "oidc-client-ts";

export const auth = new UserManager({
  authority: "https://localhost:7028",
  client_id: "react-demo-client",
  redirect_uri: "http://localhost:4001/callback",
  response_type: "code",
  scope: "openid offline_access",
});