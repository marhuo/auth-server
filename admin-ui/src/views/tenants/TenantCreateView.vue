<script setup lang="ts">
import { type TenantCreateRequest } from '@/api/generated'
import { createTenant } from '@/api/tenantApi'
import { useForm } from '@/composables/useForm'
import router, { RouteNames } from '@/router'
import { normalizeSlug } from '@/utils/normalizers'
const breadcrumbs = [{ title: 'Asiakkaat', to: { name: RouteNames.TenantList }, disabled: false }]

const form = useForm<TenantCreateRequest, string>({
  initialValues: { name: '', slug: '' },
  save: createTenant,
  onSuccess: (id) => router.replace({ name: RouteNames.TenantDetails, params: { id } }),
})

function onSlugChange(e: Event) {
  const input = e.target as HTMLInputElement
  form.values.slug = normalizeSlug(input.value)
  input.value = form.values.slug
}
</script>

<template>
  <v-breadcrumbs :items="breadcrumbs"></v-breadcrumbs>
  <h1>Asiakkaat <span class="text-tertiary">|</span> Luo uusi</h1>
  <v-form class="stacked" style="max-width: 600px" @submit.prevent="form.submit">
    <v-alert v-if="form.generalErrors.length" type="error" variant="tonal">
      <div v-for="error in form.generalErrors" :key="error">{{ error }}</div>
    </v-alert>

    <v-text-field
      label="Nimi"
      v-model="form.values.name"
      :disabled="form.submitting"
      :rules="[(v) => !!v || 'Pakollinen kenttä']"
      autocomplete="off"
    ></v-text-field>

    <v-text-field
      label="URL-tunniste (slug)"
      :model-value="form.values.slug"
      :disabled="form.submitting"
      @input="onSlugChange"
      hint="Vain pieniä kirjaimia, numeroita ja yhdysmerkkejä"
      hint-persistent
      :rules="[(v) => !!v || 'Pakollinen kenttä']"
      autocomplete="off"
    ></v-text-field>
    <v-btn type="submit" color="primary" variant="flat" :loading="form.submitting">Luo</v-btn>
  </v-form>
</template>
