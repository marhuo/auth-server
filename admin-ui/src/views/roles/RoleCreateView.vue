<script setup lang="ts">
import { createRole } from '@/api/roleApi'
import ResourceServerAutocomplete from '@/components/ResourceServerAutocomplete.vue'
import { useForm } from '@/composables/useForm'
import { RouteNames } from '@/router'
import { normalizeRoleName } from '@/utils/normalizers'
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import type { BreadcrumbItem } from 'vuetify/lib/components/VBreadcrumbs/VBreadcrumbs.mjs'

const props = defineProps<{ resourceServerId?: string }>()
const router = useRouter()

const form = useForm({
  initialValues: { name: '', description: '', resourceServerId: props.resourceServerId ?? '' },
  save: createRole,
  onSuccess: (id) => router.replace({ name: RouteNames.RoleDetails, params: { id } }),
})

const breadcrumbs = ref<BreadcrumbItem[]>([
  { title: 'Roolit', to: { name: RouteNames.RoleList }, disabled: false },
])

// Nimen normalisointi
function onNameChange(e: Event) {
  const input = e.target as HTMLInputElement
  form.values.name = normalizeRoleName(input.value)
  input.value = form.values.name
}
</script>

<template>
  <v-breadcrumbs :items="breadcrumbs"></v-breadcrumbs>
  <h1>Roolit <span class="text-tertiary">|</span> Luo uusi</h1>
  <v-form class="stacked" @submit.prevent="form.submit" style="max-width: 700px">
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
    ></v-textarea>

    <v-btn type="submit" color="primary" variant="flat" :loading="form.submitting">Luo</v-btn>
  </v-form>
</template>
