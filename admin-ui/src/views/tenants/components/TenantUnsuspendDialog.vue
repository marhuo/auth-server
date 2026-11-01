<script setup lang="ts">
import type { ErrorDto, TenantDto } from '@/api/generated'
import { ValidationError } from '@/api/httpClient'
import { unsuspendTenant } from '@/api/tenantApi'
import { ref, watch } from 'vue'

const props = defineProps<{
  tenant: TenantDto
  open: boolean
  onUnsuspended: () => void
  onCancel: () => void
}>()

const errors = ref<ErrorDto[]>([])
const submitting = ref(false)

watch(
  () => props.open,
  (open) => {
    if (open) errors.value = []
  },
)

async function submit() {
  submitting.value = true
  try {
    await unsuspendTenant(props.tenant.id)
    props.onUnsuspended()
  } catch (e) {
    if (e instanceof ValidationError) {
      errors.value = e.errors
      return
    }
    throw e
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <v-dialog :model-value="open" max-width="400" persistent>
    <v-card title="Poista lukitus">
      <template #append>
        <v-btn icon="mdi-close" variant="text" :disabled="submitting" @click="onCancel" />
      </template>
      <v-card-text>
        <v-alert v-if="errors.length" type="error" variant="tonal" class="mb-4">
          <div v-for="error in errors" :key="error.key">{{ error.messageFi }}</div>
        </v-alert>
        Haluatko varmasti poistaa asiakkaan {{ tenant.name }} lukituksen?
      </v-card-text>
      <v-card-actions class="px-6 pb-4">
        <v-spacer />
        <v-btn color="secondary" variant="flat" :disabled="submitting" @click="onCancel">
          Peruuta
        </v-btn>
        <v-btn color="primary" variant="flat" :loading="submitting" @click="submit">
          Poista lukitus
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
