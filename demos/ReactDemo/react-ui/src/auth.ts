//auth.ts
import { UserManager } from "oidc-client-ts";

export const auth = new UserManager({
  authority: "http://localhost:5028",
  client_id: "react-demo-ui",
  redirect_uri: "https://localhost:4010/callback",
  post_logout_redirect_uri: "https://localhost:4010/",
  response_type: "code",
  scope: "openid profile offline_access react-demo.api",
  loadUserInfo: true,
});