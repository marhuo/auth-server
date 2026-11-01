<script setup lang="ts">
import { getRoleById, updateRole } from '@/api/roleApi'
import ResourceServerAutocomplete from '@/components/ResourceServerAutocomplete.vue'
import { useForm } from '@/composables/useForm'
import { RouteNames } from '@/router'
import { normalizeRoleName } from '@/utils/normalizers'
import { useRouter } from 'vue-router'

const props = defineProps<{ id: string }>()
const router = useRouter()
const breadcrumbs = [{ title: 'Roolit', to: { name: RouteNames.RoleList }, disabled: false }]
const form = useForm({
  initialValues: { id: '', name: '', description: '', resourceServerId: '' },
  load: async () => {
    const role = await getRoleById(props.id)
    if (!role) return null
    return {
      id: role.id,
      name: role.name,
      description: role.description,
      resourceServerId: role.resourceServerId,
    }
  },
  save: updateRole,
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
  <h1>Rooli <span class="text-tertiary">|</span> Muokkaa</h1>

  <v-progress-linear v-if="form.loading" indeterminate />

  <v-form v-if="!form.notFound && !form.loading" class="stacked" @submit.prevent="form.submit">
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

    <resource-server-autocomplete
      :selected-id="form.values.resourceServerId"
      @change="(server) => (form.values.resourceServerId = server?.id ?? '')"
      :error-messages="form.messagesFor('resourceServerId')"
      :disabled="form.submitting"
      required
    />

    <v-textarea
      label="Kuvaus"
      v-model="form.values.description"
      :disabled="form.submitting"
      :error-messages="form.messagesFor('description')"
      :rules="[(v) => !!v || 'Pakollinen kenttä']"
      autocomplete="off"
    ></v-textarea>

    <v-btn type="submit" color="primary" variant="flat" :loading="form.submitting">Tallenna</v-btn>
  </v-form>

  <v-alert v-if="form.notFound" class="not-found" type="warning" variant="tonal">
    Roolia ei löytynyt.
  </v-alert>
</template>
