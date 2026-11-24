namespace AuthServer.Domain.Users;

// Invited: the user cannot sign in yet: no password and no external account.
// Active: the user has set a password or signed in with an external account. Never goes back to Invited.
// Locking is separate (User.Locked, User.LockedUntil): an invited or an active user can be locked.
public enum UserStatuses { Invited, Active }
