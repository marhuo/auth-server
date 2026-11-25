namespace AuthServer.Domain.Users;

// Invited: created by an invitation, has not signed in yet.
// Active: has signed in with a code or an external account. Never goes back to Invited.
// Locking is separate (User.Locked): an invited or an active user can be locked.
public enum UserStatuses { Invited, Active }
