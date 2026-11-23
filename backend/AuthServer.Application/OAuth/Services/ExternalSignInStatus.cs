namespace AuthServer.Application.OAuth.Services;

// Locked: locked by an admin or after failed logins. EmailNotVerified: the service has not verified the
// email, so the user is not created or linked.
public enum ExternalSignInStatus { Success, Locked, EmailNotVerified }
