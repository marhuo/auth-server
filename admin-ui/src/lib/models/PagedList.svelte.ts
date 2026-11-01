import type { PagedListDto, PagedListRequest } from '#lib/api/generated/index.js';

export interface Pageable {
	readonly pageNumber: number;
	readonly pageSize: number;
	readonly totalCount: number;
	readonly totalPages: number;
	readonly hasPrevious: boolean;
	readonly hasNext: boolean;
	get loading(): boolean;
	setPageNumber(pageNumber: number): void;
	setPageSize(pageSize: number): void;
	setSearchTerm(term: string): void;
}

export abstract class PagedListBase<TItem> implements Pageable {
	private _items = $state<TItem[]>([]);
	get items() {
		return this._items;
	}

	private _pageNumber = $state(1);
	get pageNumber() {
		return this._pageNumber;
	}

	private _totalPages = $state(0);
	get totalPages() {
		return this._totalPages;
	}

	private _totalCount = $state(0);
	get totalCount() {
		return this._totalCount;
	}

	private _pageSize = $state(10);
	get pageSize() {
		return this._pageSize;
	}

	private _hasPrevious = $state(false);
	get hasPrevious() {
		return this._hasPrevious;
	}

	private _hasNext = $state(false);
	get hasNext() {
		return this._hasNext;
	}

	private _loading = $state(false);
	get loading() {
		return this._loading;
	}

	private _searchTerm = $state('');
	get searchTerm() {
		return this._searchTerm;
	}

	constructor(pageSize?: number) {
		if (pageSize) this._pageSize = pageSize;
	}

	private _requestId = 0;
	private _searchTimer: ReturnType<typeof setTimeout> | undefined;

	load = async () => {
		const requestId = ++this._requestId;
		this._loading = true;
		try {
			const pagedListDto = await this.fetch();
			if (requestId !== this._requestId) return;
			this._items = pagedListDto.items;
			this._pageNumber = pagedListDto.pageNumber;
			this._pageSize = pagedListDto.pageSize;
			this._totalCount = pagedListDto.totalCount;
			this._totalPages = pagedListDto.totalPages;
			this._hasPrevious = pagedListDto.hasPrevious;
			this._hasNext = pagedListDto.hasNext;
		} finally {
			if (requestId === this._requestId) this._loading = false;
		}
	};

	protected abstract fetch(): Promise<PagedListDto<TItem>>;

	setPageNumber = (pageNumber: number) => {
		this._pageNumber = pageNumber;
		this.load();
	};

	setPageSize = (pageSize: number) => {
		this._pageSize = pageSize;
		this.load();
	};

	setSearchTerm = (term: string) => {
		this._searchTerm = term;
		this._pageNumber = 1;
		clearTimeout(this._searchTimer);
		this._searchTimer = setTimeout(() => this.load(), 300);
	};
}

type ExtraFields<TRequest> = Omit<TRequest, keyof PagedListRequest>;

export class PagedList<
	TItem,
	TRequest extends PagedListRequest = PagedListRequest
> extends PagedListBase<TItem> {
	private readonly _fetch: (request: TRequest) => Promise<PagedListDto<TItem>>;
	private readonly _initialValues: ExtraFields<TRequest>;

	constructor(
		fetch: (request: TRequest) => Promise<PagedListDto<TItem>>,
		initialValues: ExtraFields<TRequest> = {} as ExtraFields<TRequest>,
		pageSize?: number
	) {
		super(pageSize);
		this._fetch = fetch;
		this._initialValues = initialValues;
	}

	protected fetch(): Promise<PagedListDto<TItem>> {
		return this._fetch({
			...this._initialValues,
			pageNumber: this.pageNumber,
			pageSize: this.pageSize,
			searchTerm: this.searchTerm || null
		} as TRequest);
	}
}
export class InMemoryPagedList<TItem> extends PagedListBase<TItem> {
	private _all: TItem[];
	private readonly _matches: (item: TItem, term: string) => boolean;

	constructor(all: TItem[], matches: (item: TItem, term: string) => boolean, pageSize?: number) {
		super(pageSize);
		this._all = all;
		this._matches = matches;
	}

	setAll = (all: TItem[]) => {
		this._all = all;
		this.load();
	};

	protected fetch(): Promise<PagedListDto<TItem>> {
		const term = this.searchTerm.trim().toLowerCase();
		const filtered = term ? this._all.filter((item) => this._matches(item, term)) : this._all;

		const totalCount = filtered.length;
		const totalPages = Math.max(1, Math.ceil(totalCount / this.pageSize));
		const pageNumber = Math.min(this.pageNumber, totalPages);
		const start = (pageNumber - 1) * this.pageSize;

		return Promise.resolve({
			items: filtered.slice(start, start + this.pageSize),
			pageNumber,
			pageSize: this.pageSize,
			totalCount,
			totalPages,
			hasPrevious: pageNumber > 1,
			hasNext: pageNumber < totalPages
		});
	}
}
