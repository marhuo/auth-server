namespace AuthServer.Domain.Users;

// Invited: the user has been invited but has not set a password yet, so they cannot sign in.
// Active: the user has accepted the invitation by setting a password.
// Locking is separate (User.Locked, User.LockedUntil): an invited or an active user can be locked.
public enum UserStatuses { Invited, Active }
