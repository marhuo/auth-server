/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import type { UserDtoStatuses } from "./userDtoStatuses";
import type { UserRoleDto } from "./userRoleDto";

export interface UserDto {
    id: string;
    email: string;
    createdAt: string;
    locked: boolean;
    status: UserDtoStatuses;
    userRoles: UserRoleDto[];
}
