/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import type { ClientDtoTypes } from "./clientDtoTypes";

export interface ClientCreateRequest {
    type: ClientDtoTypes;
    clientId: string;
    displayName: string;
    secret: string | null;
    resourceServerId: string;
    tenantId: string | null;
    redirectUris: string[];
    postLogoutRedirectUris: string[];
    scopeIds: string[];
}
