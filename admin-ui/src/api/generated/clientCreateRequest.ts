/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import type { ClientDtoTypes } from "./clientDtoTypes";

export interface ClientCreateRequest {
    type: ClientDtoTypes;
    clientId: string;
    displayName: string;
    resourceServerId: string;
    createdAt: string;
    tenantId: string | null;
    redirectUris: string[];
    postLogoutRedirectUris: string[];
    scopes: string[];
}
