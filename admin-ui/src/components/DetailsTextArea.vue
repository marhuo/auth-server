<script setup lang="ts">
import type { ErrorDto } from '@/api/generated'
import { ValidationError } from '@/api/httpClient'
import { computed, ref } from 'vue'

const props = defineProps<{
  label: string
  value: string | null
  hint?: string
  editable: boolean
  required?: boolean
  onSave: (value: string) => Promise<void>
}>()

const editMode = ref(false)
const inputValue = ref('')
const errors = ref<ErrorDto[]>([])
const submitting = ref(false)

function onEditClick() {
  inputValue.value = props.value ?? ''
  errors.value = []
  editMode.value = true
}

async function onSubmit() {
  submitting.value = true
  try {
    await props.onSave(inputValue.value)
    editMode.value = false
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

const hasChanges = computed(() => inputValue.value !== (props.value ?? ''))
</script>

<template>
  <v-list-item>
    <v-list-item-subtitle>{{ label }}</v-list-item-subtitle>
    <v-list-item-title>
      {{ value }}
      <v-icon-btn
        v-if="editable"
        icon="mdi-pencil"
        variant="plain"
        color="primary"
        title="Muokkaa"
        aria-label="Muokkaa"
        size="small"
        class="ml-2"
        @click="onEditClick"
      />
    </v-list-item-title>
  </v-list-item>

  <v-dialog v-model="editMode" max-width="500">
    <v-form @submit.prevent="onSubmit">
      <v-card :title="label">
        <template #append>
          <v-btn
            icon="mdi-close"
            variant="text"
            size="small"
            :disabled="submitting"
            @click="editMode = false"
          />
        </template>

        <v-card-text class="pb-0">
          <v-textarea
            v-model="inputValue"
            :hint="hint"
            persistent-hint
            :disabled="submitting"
            :error-messages="errors.map((e) => e.messageFi)"
            :rules="props.required ? [(v) => !!v || 'Pakollinen kenttä'] : []"
          ></v-textarea>
        </v-card-text>

        <v-card-actions class="px-6 pt-0 pb-6">
          <v-spacer />
          <v-btn
            type="submit"
            color="primary"
            variant="flat"
            :loading="submitting"
            :disabled="!hasChanges"
          >
            Tallenna
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-form>
  </v-dialog>
</template>
