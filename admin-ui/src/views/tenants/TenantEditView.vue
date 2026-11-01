<script setup lang="ts">
import { type TenantUpdateRequest } from '@/api/generated'
import { getTenantById, updateTenant } from '@/api/tenantApi'
import { useForm } from '@/composables/useForm'
import { RouteNames } from '@/router'
import { normalizeSlug } from '@/utils/normalizers'
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import type { BreadcrumbItem } from 'vuetify/lib/components/VBreadcrumbs/VBreadcrumbs.mjs'

const props = defineProps<{ id: string }>()
const router = useRouter()
const breadcrumbs = ref<BreadcrumbItem[]>([
  { title: 'Asiakkaat', to: { name: RouteNames.TenantList } },
])

const form = useForm<TenantUpdateRequest>({
  initialValues: { id: '', name: '', slug: '' },
  save: updateTenant,
  load: async () => {
    const tenant = await getTenantById(props.id)
    if (!tenant) return null
    return {
      id: tenant.id,
      name: tenant.name,
      slug: tenant.slug,
    }
  },
  onSuccess: () => router.replace({ name: RouteNames.TenantDetails, params: { id: props.id } }),
})

function onSlugChange(e: Event) {
  const input = e.target as HTMLInputElement
  form.values.slug = normalizeSlug(input.value)
  input.value = form.values.slug
}
</script>

<template>
  <v-breadcrumbs :items="breadcrumbs"></v-breadcrumbs>
  <h1>Asiakkaat <span class="text-tertiary">|</span> Muokkaa</h1>
  <v-form class="stacked" style="max-width: 600px" @submit.prevent="form.submit">
    <v-alert v-if="form.generalErrors.length" type="error" variant="tonal">
      <div v-for="error in form.generalErrors" :key="error">{{ error }}</div>
    </v-alert>

    <v-text-field
      label="Nimi"
      v-model="form.values.name"
      :disabled="form.submitting"
      :error-messages="form.messagesFor('name')"
      autocomplete="off"
      :rules="[(v) => !!v || 'Pakollinen kenttä']"
    ></v-text-field>

    <v-text-field
      label="URL-tunniste (slug)"
      :model-value="form.values.slug"
      :disabled="form.submitting"
      @input="onSlugChange"
      :error-messages="form.messagesFor('slug')"
      hint="Vain pienet kirjaimet, numerot ja väliviivat"
      persistent-hint
      :rules="[(v) => !!v || 'Pakollinen kenttä']"
      autocomplete="off"
    ></v-text-field>

    <v-btn type="submit" color="primary" variant="flat" :loading="form.submitting">Tallenna</v-btn>
  </v-form>
  <v-alert class="not-found" v-if="form.notFound" type="warning" variant="tonal">
    Asiakasta ei löytynyt.
  </v-alert>
</template>
