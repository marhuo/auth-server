// src/composables/useForm.ts
import type { ErrorDto } from '@/api/generated'
import { ValidationError } from '@/api/httpClient'
import { computed, reactive, ref } from 'vue'

interface FormOptions<TModel, TResponse> {
  initialValues: TModel
  save: (model: TModel) => Promise<TResponse>
  onSuccess?: (response: TResponse) => void
  load?: () => Promise<TModel | null>
}
export interface Form<TModel> {
  values: TModel
  loading: boolean
  notFound: boolean
  submitting: boolean
  submit: (event?: Promise<{ valid: boolean }>) => Promise<void>
  messagesFor: (key: keyof TModel & string) => string[]
  generalErrors: string[]
}

export function useForm<TModel, TResponse = void>(
  options: FormOptions<TModel, TResponse>,
): Form<TModel> {
  const form = ref(options.initialValues)
  const loading = ref(false)
  const submitting = ref(false)
  const errors = ref<ErrorDto[]>([])
  const notFound = ref(false)

  async function load() {
    if (!options.load) return
    loading.value = true
    try {
      const values = await options.load()
      if (values === null) {
        notFound.value = true
        return
      }
      form.value = values
    } finally {
      loading.value = false
    }
  }

  function isValidationEvent(e: unknown): e is PromiseLike<{ valid: boolean }> {
    return typeof (e as PromiseLike<unknown> | undefined)?.then === 'function'
  }

  async function submit(event?: unknown) {
    if (isValidationEvent(event)) {
      const { valid } = await event
      if (!valid) return
    }

    submitting.value = true
    errors.value = []
    try {
      const response = await options.save(form.value)
      options.onSuccess?.(response)
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

  function messagesFor(key: keyof TModel & string) {
    return errors.value.filter((e) => e.key === key).map((e) => e.messageFi)
  }

  const generalErrors = computed(() =>
    errors.value.filter((e) => e.key === '' || e.key === 'id').map((e) => e.messageFi),
  )

  load()

  return reactive({
    values: form,
    loading,
    notFound,
    submitting,
    submit,
    messagesFor,
    generalErrors,
  }) as Form<TModel>
}
