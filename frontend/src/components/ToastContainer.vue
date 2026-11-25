<script setup lang="ts">
import type { ToastMessage } from '../types/catalogo'

defineProps<{
  toasts: ToastMessage[]
}>()

defineEmits<{
  (e: 'dismiss', id: number): void
}>()

function iconClass(type: ToastMessage['type']): string {
  switch (type) {
    case 'success':
      return 'bi-check-circle-fill text-success'
    case 'warning':
      return 'bi-exclamation-triangle-fill text-warning'
    case 'danger':
      return 'bi-x-circle-fill text-danger'
    default:
      return 'bi-info-circle-fill text-primary'
  }
}
</script>

<template>
  <div
    class="position-fixed bottom-0 end-0 p-3 d-flex flex-column gap-2"
    style="z-index: 1090; max-width: 400px; width: 100%;"
  >
    <div
      v-for="toast in toasts"
      :key="toast.id"
      class="surface-panel shadow p-3 d-flex align-items-start gap-2"
      role="status"
      aria-live="polite"
    >
      <i class="bi fs-5 mt-1" :class="iconClass(toast.type)"></i>
      <div class="flex-grow-1">
        <div class="fw-semibold" style="font-size: 0.9rem;">{{ toast.title }}</div>
        <div class="text-secondary" style="font-size: 0.82rem;">{{ toast.body }}</div>
      </div>
      <button
        type="button"
        class="btn-close btn-sm"
        aria-label="Fechar notificação"
        @click="$emit('dismiss', toast.id)"
      ></button>
    </div>
  </div>
</template>
