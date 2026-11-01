import type { ErrorDto } from '#lib/api/generated/index.ts';
import { ValidationError } from '#lib/api/httpClient.ts';

export abstract class FormModelBase {
	private _fields: string[] = [];

	private _submitting = $state(false);
	get submitting() {
		return this._submitting;
	}

	private _errors = $state<ErrorDto[]>([]);

	get errors() {
		return this._errors.map((e) => e.messageFi);
	}

	errorsFor(key: string) {
		return this._errors.filter((e) => e.key === key).map((e) => e.messageFi);
	}

	get generalErrors() {
		return this._errors
			.filter((e) => e.key === 'id' || !this._fields.includes(e.key))
			.map((e) => e.messageFi);
	}

	protected async run<TRequest extends object, TResult>(
		request: TRequest,
		send: (request: TRequest) => Promise<TResult>
	): Promise<TResult | null> {
		if (this._submitting) return null;

		this._fields = Object.keys(request);
		this._submitting = true;
		this._errors = [];
		try {
			return await send(request);
		} catch (e) {
			if (e instanceof ValidationError) {
				this._errors = e.errors;
				return null;
			}
			throw e;
		} finally {
			this._submitting = false;
		}
	}
}
