/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import type { ClientDtoTypes } from "./clientDtoTypes";

export interface ClientDto {
    id: string;
    type: ClientDtoTypes;
    clientId: string;
    displayName: string;
    isActive: boolean;
    resourceServerId: string;
    createdAt: string;
    tenantId: string | null;
    redirectUris: string[];
    postLogoutRedirectUris: string[];
    scopeIds: string[];
    allowedGrantTypes: string[];
}
