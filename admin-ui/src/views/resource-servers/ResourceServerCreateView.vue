<script setup lang="ts">
import { type ResourceServerCreateRequest } from '@/api/generated'
import { createResourceServer } from '@/api/resourceServerApi'
import { useForm } from '@/composables/useForm'
import router, { RouteNames } from '@/router'
const breadcrumbs = [
  { title: 'Resurssi palveliment', to: { name: RouteNames.ResourceServerList }, disabled: false },
]
const form = useForm<ResourceServerCreateRequest, string>({
  initialValues: { audience: '', displayName: '' },
  save: createResourceServer,
  onSuccess: (id) => router.replace({ name: RouteNames.ResourceServerDetails, params: { id } }),
})
</script>

<template>
  <v-breadcrumbs :items="breadcrumbs"></v-breadcrumbs>
  <h1>Resurssipalvelimet <span class="text-tertiary">|</span> Luo uusi</h1>
  <v-form class="stacked" @submit.prevent="form.submit" style="max-width: 600px">
    <v-alert v-if="form.generalErrors.length" type="error" variant="tonal">
      <div v-for="error in form.generalErrors" generalErrors :key="error">{{ error }}</div>
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

    <v-btn type="submit" color="primary" variant="flat" :loading="form.submitting">Luo</v-btn>
  </v-form>
</template>
