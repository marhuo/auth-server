<script setup lang="ts">
import { createScope } from '@/api/scopeApi'
import ResourceServerAutocomplete from '@/components/ResourceServerAutocomplete.vue'
import { useForm } from '@/composables/useForm'
import { RouteNames } from '@/router'
import { normalizeScopeName } from '@/utils/normalizers'
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import type { BreadcrumbItem } from 'vuetify/lib/components/VBreadcrumbs/VBreadcrumbs.mjs'

const props = defineProps<{ resourceServerId?: string }>()
const router = useRouter()

const form = useForm({
  initialValues: { name: '', displayName: '', resourceServerId: props.resourceServerId ?? null },
  save: createScope,
  onSuccess: (id) => router.replace({ name: RouteNames.ScopeDetails, params: { id } }),
})

const breadcrumbs = ref<BreadcrumbItem[]>([
  { title: 'Scopet', to: { name: RouteNames.ScopeList }, disabled: false },
])

// Nimen normalisointi
function onNameChange(e: Event) {
  const input = e.target as HTMLInputElement
  form.values.name = normalizeScopeName(input.value)
  input.value = form.values.name
}
</script>

<template>
  <v-breadcrumbs :items="breadcrumbs"></v-breadcrumbs>
  <h1>Scopet <span class="text-tertiary">|</span> Luo uusi</h1>
  <v-form class="stacked" @submit.prevent="form.submit">
    <v-alert v-if="form.generalErrors.length" type="error" variant="tonal">
      <div v-for="error in form.generalErrors" :key="error">{{ error }}</div>
    </v-alert>

    <v-text-field
      label="Nimi"
      :model-value="form.values.name"
      :disabled="form.submitting"
      :error-messages="form.messagesFor('name')"
      @input="onNameChange"
      hint="Pienet kirjaimet, numerot sekä erottimet - : . (esim. orders:read)"
      autocomplete="off"
      :rules="[(v) => !!v || 'Pakollinen kenttä']"
    ></v-text-field>

    <v-text-field
      label="Näytettävä nimi"
      v-model="form.values.displayName"
      :disabled="form.submitting"
      :error-messages="form.messagesFor('displayName')"
      autocomplete="off"
      :rules="[(v) => !!v || 'Pakollinen kenttä']"
    ></v-text-field>

    <resource-server-autocomplete
      :selected-id="form.values.resourceServerId"
      @change="(server) => (form.values.resourceServerId = server?.id ?? '')"
      :error-messages="form.messagesFor('resourceServerId')"
      :disabled="form.submitting"
      :hint="'Jätä tyhjäksi jos globaali'"
    />

    <v-btn type="submit" color="primary" variant="flat" :loading="form.submitting">Luo</v-btn>
  </v-form>
</template>
