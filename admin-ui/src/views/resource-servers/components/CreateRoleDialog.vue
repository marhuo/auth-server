<script setup lang="ts">
import type { ResourceServerDto, RoleCreateRequest } from '@/api/generated'
import { createRole } from '@/api/roleApi'
import { useForm } from '@/composables/useForm'
import { normalizeRoleName } from '@/utils/normalizers'
import { watch } from 'vue'

const props = defineProps<{
  open: boolean
  resourceServer: ResourceServerDto
  onCancel: () => void
  onCreated: () => void
}>()
const form = useForm<RoleCreateRequest, string>({
  initialValues: { name: '', description: '', resourceServerId: props.resourceServer.id },
  save: createRole,
  onSuccess: () => props.onCreated(),
})

watch(
  () => props.open,
  () => {
    form.values.name = ''
    form.values.description = ''
  },
)

function onNameChange(e: Event) {
  const input = e.target as HTMLInputElement
  form.values.name = normalizeRoleName(input.value)
  input.value = form.values.name
}
</script>
<template>
  <v-dialog :model-value="props.open" width="500" persistent>
    <v-form @submit.prevent="form.submit">
      <v-card title="Luo uusi rooli" :loading="form.submitting">
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
            hint="Vain pienet kirjaimet, numerot ja väliviivat"
            autocomplete="off"
            :rules="[(v) => !!v || 'Pakollinen kenttä']"
          ></v-text-field>

          <v-textarea
            label="Kuvaus"
            v-model="form.values.description"
            :disabled="form.submitting"
            :error-messages="form.messagesFor('description')"
            :rules="[(v) => !!v || 'Pakollinen kenttä']"
            class="mt-4"
          ></v-textarea>
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
