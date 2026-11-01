<script setup lang="ts">
import type { TenantDto, TenantSuspendRequest } from '@/api/generated'
import { suspendTenant } from '@/api/tenantApi'
import { useForm } from '@/composables/useForm'
import { watch } from 'vue'
const props = defineProps<{
  tenant: TenantDto
  open: boolean
  onSuspended: () => void
  onCancel: () => void
}>()

const form = useForm<TenantSuspendRequest>({
  initialValues: { id: props.tenant.id, suspensionReason: '' },
  save: suspendTenant,
  onSuccess: async () => {
    props.onSuspended()
  },
})

watch(
  () => props.tenant,
  async () => {
    form.values.id = props.tenant.id
    form.values.suspensionReason = ''
  },
  { immediate: true },
)
</script>
<template>
  <v-dialog :model-value="props.open" width="500" persistent>
    <v-form @submit.prevent="form.submit">
      <v-card title="Lukitse asiakas">
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

        <v-card-text>
          <v-textarea
            v-model="form.values.suspensionReason"
            label="Syy"
            rows="3"
            :disabled="form.submitting"
            :error-messages="form.messagesFor('suspensionReason')"
            :rules="[(v) => !!v || 'Pakollinen kenttä']"
            :hint="'Pakollinen kenttä'"
            persistent-hint
          />
        </v-card-text>

        <v-card-actions class="px-6 pb-4">
          <v-spacer />
          <v-btn
            color="secondary"
            variant="flat"
            :disabled="form.submitting"
            @click="props.onCancel"
          >
            Peruuta
          </v-btn>
          <v-btn type="submit" color="error" variant="flat" :loading="form.submitting">
            Lukitse
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-form>
  </v-dialog>
</template>
