<script setup lang="ts">
import type { ErrorDto, ResourceServerDto } from '@/api/generated'
import { ValidationError } from '@/api/httpClient'
import { getResourceServerById } from '@/api/resourceServerApi'
import { RouteNames } from '@/router'
import { computed, ref, watch } from 'vue'

const props = defineProps<{
  label: string
  selectedId: string | null
  hint?: string
  editable: boolean
  required?: boolean
  onSave: (value: ResourceServerDto | null) => Promise<void>
}>()

const editMode = ref(false)
const resourceServer = ref<ResourceServerDto | null>(null)
const editorResourceServer = ref<ResourceServerDto | null>(null)
const loading = ref(true)
const errors = ref<ErrorDto[]>([])
const submitting = ref(false)

watch(
  () => props.selectedId,
  async (id) => {
    if (id === resourceServer.value?.id) return
    resourceServer.value = id ? await getResourceServerById(id) : null
    editorResourceServer.value = resourceServer.value
    loading.value = false
  },
  { immediate: true },
)

function onEditClick() {
  editorResourceServer.value = resourceServer.value
  errors.value = []
  editMode.value = true
}

async function onSubmit() {
  submitting.value = true
  try {
    await props.onSave(editorResourceServer.value)
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

function onClose() {
  editMode.value = false
  editorResourceServer.value = resourceServer.value
}

const hasChanges = computed(() => editorResourceServer.value?.id !== resourceServer.value?.id)
</script>

<template>
  <v-list-item>
    <v-list-item-subtitle>{{ label }}</v-list-item-subtitle>
    <v-list-item-title>
      <router-link
        v-if="resourceServer"
        :to="{ name: RouteNames.ResourceServerDetails, params: { id: resourceServer.id } }"
        >{{ resourceServer.displayName }}</router-link
      >
      <slot v-else name="empty" />

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
            @click="onClose"
          />
        </template>

        <v-card-text class="pb-0">
          <resource-server-autocomplete
            :selectedId="resourceServer?.id"
            :required="required"
            @change="(newValue) => (editorResourceServer = newValue)"
          />
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
