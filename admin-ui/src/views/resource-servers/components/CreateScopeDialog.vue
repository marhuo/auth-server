<script setup lang="ts">
import type { ResourceServerDto, ScopeCreateRequest } from '@/api/generated'
import { createScope } from '@/api/scopeApi'
import { useForm } from '@/composables/useForm'
import { normalizeScopeName } from '@/utils/normalizers'
import { watch } from 'vue'

const props = defineProps<{
  open: boolean
  resourceServer: ResourceServerDto
  onCancel: () => void
  onCreated: () => void
}>()
const form = useForm<ScopeCreateRequest, string>({
  initialValues: { name: '', displayName: '', resourceServerId: props.resourceServer.id },
  save: createScope,
  onSuccess: () => props.onCreated(),
})

watch(
  () => props.open,
  () => {
    form.values.name = ''
    form.values.displayName = ''
  },
)

function onNameChange(e: Event) {
  const input = e.target as HTMLInputElement
  form.values.name = normalizeScopeName(input.value)
  input.value = form.values.name
}
</script>
<template>
  <v-dialog :model-value="props.open" width="500" persistent>
    <v-form @submit.prevent="form.submit">
      <v-card title="Luo uusi scope" :loading="form.submitting">
        <template #append>
          <v-btn
            icon="mdi-close"
            variant="text"
            :disabled="form.submitting"
            @click="props.onCancel"
          />
        </template>

        <v-alert v-if="form.generalErrors.length" type="error" variant="tonal">
          <div v-for="error in form.generalErrors" :key="error">{{ error }}</div>
        </v-alert>

        <v-card-text class="pb-0">
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
            class="mt-4"
          ></v-text-field>
        </v-card-text>

        <v-card-actions class="px-6 pb-4">
          <v-spacer />
          <v-btn type="submit" color="primary" variant="flat" :loading="form.submitting">
            Luo
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-form>
  </v-dialog>
</template>
