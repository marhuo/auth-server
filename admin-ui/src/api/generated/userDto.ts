/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import type { UserRoleDto } from "./userRoleDto";

export interface UserDto {
    id: string;
    userName: string;
    email: string;
    isActive: boolean;
    lockedUntil: string | null;
    userRoles: UserRoleDto[];
}
