<script lang="ts">
	import { changeUserPassword } from '#lib/api/userApi.ts';
	import Dialog from '#lib/components/Dialog.svelte';
	import FormField from '#lib/components/FormField.svelte';
	import FormLabel from '#lib/components/FormLabel.svelte';
	import GeneralErrors from '#lib/components/GeneralErrors.svelte';
	import PasswordInput from '#lib/components/PasswordInput.svelte';
	import { FormModelBase } from '#lib/models/FormModelBase.svelte.ts';
	import Icon from '@iconify/svelte';

	interface Props {
		userId: string;
		username: string;
		onChanged: () => void;
		onCancel: () => void;
	}

	let { userId, username, onChanged, onCancel }: Props = $props();

	class ChangePasswordModel extends FormModelBase {
		newPassword = $state('');
		confirmPassword = $state('');

		get mismatch() {
			return this.confirmPassword !== '' && this.confirmPassword !== this.newPassword;
		}

		get canSubmit() {
			return (
				!this.submitting && this.newPassword !== '' && this.confirmPassword === this.newPassword
			);
		}

		submit = (id: string) => this.run({ id, newPassword: this.newPassword }, changeUserPassword);
	}

	const model = new ChangePasswordModel();

	async function onSubmit(e: SubmitEvent) {
		e.preventDefault();
		const result = await model.submit(userId);
		if (result !== null) onChanged();
	}
</script>

<Dialog open={true}>
	<form class="modal-box flex flex-col gap-6" onsubmit={onSubmit}>
		<div class="flex items-center justify-between">
			<h3 class="text-lg font-semibold">Vaihda salasana: {username}</h3>
			<button
				type="button"
				class="btn btn-square btn-ghost btn-sm"
				aria-label="Sulje"
				disabled={model.submitting}
				onclick={onCancel}
			>
				<Icon icon="ic:baseline-close" width="20" />
			</button>
		</div>

		<GeneralErrors messages={model.generalErrors} />

		<fieldset class="fieldset gap-6" disabled={model.submitting}>
			<FormField>
				<FormLabel>Uusi salasana</FormLabel>
				<PasswordInput
					bind:value={model.newPassword}
					errors={model.errorsFor('newPassword')}
					required
				/>
			</FormField>

			<FormField>
				<FormLabel>Vahvista salasana</FormLabel>
				<PasswordInput
					bind:value={model.confirmPassword}
					errors={model.mismatch ? ['Salasanat eivät täsmää'] : []}
					required
				/>
			</FormField>
		</fieldset>

		<div class="modal-action">
			<button type="submit" class="btn btn-primary" disabled={!model.canSubmit}>
				{#if model.submitting}<span class="loading loading-sm loading-spinner"></span>{/if}
				Vaihda salasana
			</button>
		</div>
	</form>
</Dialog>
