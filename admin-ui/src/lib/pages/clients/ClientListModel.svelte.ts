import { getClientPagedList } from '#lib/api/clientApi.ts';
import type { PagedListDto } from '#lib/api/generated/index.ts';
import { getResourceServerMapByIds } from '#lib/api/resourceServerApi.ts';
import { getTenantMapByIds } from '#lib/api/tenantApi.ts';
import { clientTypeLabel } from '#lib/domain/clientTypes.ts';
import { PagedListBase } from '#lib/models/PagedList.svelte.ts';

export interface ClientScope {
	scopeId: string;
	scopeName: string;
}
export interface ClientListItem {
	id: string;
	displayName: string;
	typeLabel: string;
	resourceServerId: string;
	resourceServerName: string;
	tenantId: string | null;
	tenantName: string | null;
}

export class ClientListModel extends PagedListBase<ClientListItem> {
	protected async fetch(): Promise<PagedListDto<ClientListItem>> {
		const page = await getClientPagedList({
			pageNumber: this.pageNumber,
			pageSize: this.pageSize,
			searchTerm: this.searchTerm || null
		});

		const resourceServerIds = page.items.map((c) => c.resourceServerId);
		const resourceServerMap = await getResourceServerMapByIds(resourceServerIds);

		const tenantIds = page.items.filter((c) => c.tenantId !== null).map((c) => c.tenantId!);
		const tenantMap = await getTenantMapByIds(tenantIds);

		const items: ClientListItem[] = page.items.map((dto) => ({
			id: dto.id,
			displayName: dto.displayName,
			typeLabel: clientTypeLabel(dto.type),
			resourceServerId: dto.resourceServerId,
			resourceServerName: resourceServerMap.get(dto.resourceServerId)?.displayName ?? '',
			tenantId: dto.tenantId,
			tenantName: dto.tenantId ? (tenantMap.get(dto.tenantId)?.name ?? null) : null
		}));
		return { ...page, items };
	}
}
