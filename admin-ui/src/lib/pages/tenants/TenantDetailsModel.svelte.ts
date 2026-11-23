import type { TenantDomainDto } from '#lib/api/generated/tenantDomainDto.ts';
import {
	createTenantDomain,
	deleteTenant,
	deleteTenantDomain,
	getTenantById,
	getTenantDomains,
	updateTenant,
	verifyTenantDomain
} from '#lib/api/tenantApi.ts';

export class TenantDetailsModel {
	readonly id: string;

	constructor(id: string) {
		this.id = id;
	}

	private _loading = $state(true);
	get loading() {
		return this._loading;
	}

	private _notFound = $state(false);
	get notFound() {
		return this._notFound;
	}

	private _name = $state('');
	get name() {
		return this._name;
	}

	private _slug = $state('');
	get slug() {
		return this._slug;
	}

	private _suspendedAt = $state<string | null>(null);
	get suspendedAt() {
		return this._suspendedAt;
	}

	private _suspensionReason = $state<string | null>(null);
	get suspensionReason() {
		return this._suspensionReason;
	}

	private _createdAt = $state('');
	get createdAt() {
		return this._createdAt;
	}

	get isSuspended() {
		return this._suspendedAt !== null;
	}

	// Verified by the tenant through its product (DNS TXT record).
	private _domains = $state<TenantDomainDto[]>([]);
	get domains() {
		return this._domains;
	}

	load = async () => {
		this._loading = true;
		try {
			const [tenant, domains] = await Promise.all([
				getTenantById(this.id),
				getTenantDomains(this.id)
			]);
			if (!tenant) {
				this._notFound = true;
				return;
			}
			this._domains = domains;
			this._name = tenant.name;
			this._slug = tenant.slug;
			this._suspendedAt = tenant.suspendedAt;
			this._suspensionReason = tenant.suspensionReason;
			this._createdAt = tenant.createdAt;
			this._notFound = false;
		} finally {
			this._loading = false;
		}
	};

	changeName = async (name: string) => {
		await updateTenant({ id: this.id, name, slug: this._slug });
		this._name = name;
	};

	changeSlug = async (slug: string) => {
		await updateTenant({ id: this.id, name: this._name, slug });
		this._slug = slug;
	};

	delete = () => deleteTenant(this.id);

	addDomain = async (name: string) => {
		const domain = await createTenantDomain({ tenantId: this.id, name });
		this._domains = [...this._domains, domain].sort((a, b) => a.name.localeCompare(b.name));
	};

	verifyDomain = async (domainId: string) => {
		const verified = await verifyTenantDomain(this.id, domainId);
		this._domains = this._domains.map((d) => (d.id === domainId ? verified : d));
	};

	deleteDomain = async (domainId: string) => {
		await deleteTenantDomain(this.id, domainId);
		this._domains = this._domains.filter((d) => d.id !== domainId);
	};
}
