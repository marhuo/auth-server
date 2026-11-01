<script setup lang="ts">
import { type ResourceServerUpdateRequest } from '@/api/generated'
import { getResourceServerById, updateResourceServer } from '@/api/resourceServerApi'
import { useForm } from '@/composables/useForm'
import { RouteNames } from '@/router'
import { computed } from 'vue'
import { useRouter, type RouteLocationRaw } from 'vue-router'

const props = defineProps<{ id: string }>()
const router = useRouter()
const form = useForm<ResourceServerUpdateRequest>({
  initialValues: { id: '', audience: '', displayName: '' },
  load: async () => {
    const resourceServer = await getResourceServerById(props.id)
    if (!resourceServer) return null
    return {
      id: resourceServer.id,
      audience: resourceServer.audience,
      displayName: resourceServer.displayName,
    }
  },
  save: updateResourceServer,
  onSuccess: () => router.replace({ name: RouteNames.RoleDetails, params: { id: props.id } }),
})

const breadcrumbs = computed(() => {
  const items: { title: string; to?: RouteLocationRaw; disabled?: boolean }[] = [
    { title: 'Resurssipalvelimet', to: { name: RouteNames.ResourceServerList }, disabled: false },
  ]
  if (form.values.id) {
    items.push({
      title: form.values.audience,
      to: { name: RouteNames.ResourceServerDetails, params: { id: props.id } },
      disabled: false,
    })
  }
  return items
})
</script>

<template>
  <v-breadcrumbs :items="breadcrumbs"></v-breadcrumbs>
  <h1>Resurssipalvelin <span class="text-tertiary">|</span> Muokkaa</h1>

  <v-form
    v-if="!form.loading && !form.notFound"
    style="max-width: 500px"
    class="stacked"
    @submit.prevent="form.submit"
  >
    <v-alert v-if="form.generalErrors.length" type="error" variant="tonal">
      <div v-for="error in form.generalErrors" :key="error">{{ error }}</div>
    </v-alert>

    <v-text-field
      label="Nimi"
      v-model="form.values.displayName"
      :disabled="form.submitting"
      :error-messages="form.messagesFor('displayName')"
      :rules="[(v) => !!v || 'Pakollinen kenttä']"
      autocomplete="off"
    ></v-text-field>

    <v-text-field
      label="Tunniste (audience)"
      v-model="form.values.audience"
      :disabled="form.submitting"
      :error-messages="form.messagesFor('audience')"
      :rules="[(v) => !!v || 'Pakollinen kenttä']"
      autocomplete="off"
    ></v-text-field>

    <v-btn type="submit" color="primary" variant="flat" :loading="form.submitting">Tallenna</v-btn>
  </v-form>

  <v-alert class="not-found" v-if="form.notFound" type="warning" variant="tonal">
    Resurssipalvelinta ei löytynyt.
  </v-alert>
</template>
