<script setup lang="ts">
import { type ScopeUpdateRequest } from '@/api/generated'
import { getScopeById, updateScope } from '@/api/scopeApi'
import ResourceServerAutocomplete from '@/components/ResourceServerAutocomplete.vue'
import { useForm } from '@/composables/useForm'
import { RouteNames } from '@/router'
import { normalizeRoleName } from '@/utils/normalizers'
import { useRouter } from 'vue-router'

const props = defineProps<{ id: string }>()
const router = useRouter()
const breadcrumbs = [{ title: 'Scopet', to: { name: RouteNames.ScopeList }, disabled: false }]

const form = useForm<ScopeUpdateRequest>({
  initialValues: { id: '', name: '', displayName: '', resourceServerId: '' },
  save: updateScope,
  load: async () => {
    const scope = await getScopeById(props.id)
    if (!scope) return null
    return {
      id: scope.id,
      name: scope.name,
      displayName: scope.displayName,
      resourceServerId: scope.resourceServerId,
    }
  },
  onSuccess: () => router.replace({ name: RouteNames.RoleDetails, params: { id: props.id } }),
})

function onNameChange(e: Event) {
  const input = e.target as HTMLInputElement
  form.values.name = normalizeRoleName(input.value)
  input.value = form.values.name
}
</script>

<template>
  <v-breadcrumbs :items="breadcrumbs"></v-breadcrumbs>
  <h1>Scope <span class="text-tertiary">|</span> Muokkaa</h1>
  <v-form
    v-if="!form.loading && !form.notFound"
    class="stacked"
    style="max-width: 600px"
    @submit.prevent="form.submit"
  >
    <v-alert v-if="form.generalErrors.length" type="error" variant="tonal">
      <div v-for="error in form.generalErrors" :key="error">{{ error }}</div>
    </v-alert>

    <v-text-field
      label="Nimi"
      :model-value="form.values.name"
      :disabled="form.submitting"
      :error-messages="form.messagesFor('name')"
      @input="onNameChange"
      hint="Vain pienet kirjaimet, numerot ja väliviivat"
      autocomplete="off"
      :rules="[(v) => !!v || 'Pakollinen kenttä']"
    ></v-text-field>

    <v-text-field
      label="Näyttönimi"
      :model="form.values.displayName"
      :disabled="form.submitting"
      :error-messages="form.messagesFor('displayName')"
      autocomplete="off"
      :rules="[(v) => !!v || 'Pakollinen kenttä']"
    ></v-text-field>

    <resource-server-autocomplete
      :selected-id="form.values.resourceServerId"
      @change="(server) => (form.values.resourceServerId = server?.id ?? null)"
      :error-messages="form.messagesFor('resourceServerId')"
      :hint="'Jätä tyhjäksi jos globaali'"
    />

    <v-btn type="submit" color="primary" variant="flat" :loading="form.submitting">Luo</v-btn>
  </v-form>
  <v-alert class="not-found" v-if="form.notFound" type="warning" variant="tonal">
    Scopea ei löytynyt.
  </v-alert>
</template>
