<script setup lang="ts">
export type TabKey = 'produtos' | 'categorias' | 'sincronizacao'

defineProps<{
  activeTab: TabKey
  catalogoOnline: boolean
  vitrineOnline: boolean
  vitrineSimulandoFalha: boolean
  divergenciasCount: number
}>()

defineEmits<{
  (e: 'change-tab', tab: TabKey): void
}>()
</script>

<template>
  <header class="topbar-dark py-2 sticky-top">
    <div class="container-xl d-flex flex-wrap align-items-center justify-content-between gap-3">
      <div class="d-flex align-items-center gap-3">
        <div class="d-flex align-items-center gap-2 text-white">
          <span
            class="d-inline-flex align-items-center justify-content-center rounded-2"
            style="width: 34px; height: 34px; background: #1d4ed8;"
          >
            <i class="bi bi-box-seam fs-6"></i>
          </span>
          <div>
            <div class="fw-bold lh-sm" style="font-size: 0.98rem;">Catálogo Master</div>
            <div class="text-secondary" style="font-size: 0.72rem; color: #94a3b8 !important;">
              ASP.NET Core + Vue 3
            </div>
          </div>
        </div>

        <nav class="d-flex align-items-center gap-1 ms-md-3" aria-label="Navegação principal">
          <button
            type="button"
            class="nav-tab-btn"
            :class="{ active: activeTab === 'produtos' }"
            @click="$emit('change-tab', 'produtos')"
          >
            <i class="bi bi-grid me-1"></i>
            Produtos
          </button>
          <button
            type="button"
            class="nav-tab-btn"
            :class="{ active: activeTab === 'categorias' }"
            @click="$emit('change-tab', 'categorias')"
          >
            <i class="bi bi-tags me-1"></i>
            Categorias
          </button>
          <button
            type="button"
            class="nav-tab-btn d-inline-flex align-items-center gap-1"
            :class="{ active: activeTab === 'sincronizacao' }"
            @click="$emit('change-tab', 'sincronizacao')"
          >
            <i class="bi bi-arrow-left-right me-1"></i>
            Sincronização
            <span
              v-if="divergenciasCount > 0"
              class="badge rounded-pill text-bg-warning text-dark ms-1"
              style="font-size: 0.7rem;"
            >
              {{ divergenciasCount }}
            </span>
          </button>
        </nav>
      </div>

      <div class="d-flex align-items-center gap-2">
        <span class="status-pill" title="API Principal (APICatalogo)">
          <span class="status-dot" :class="catalogoOnline ? 'online' : 'offline'"></span>
          Catálogo :5070
        </span>
        <span class="status-pill" title="API Secundária (APIVitrine)">
          <span
            class="status-dot"
            :class="
              !vitrineOnline ? 'offline' : vitrineSimulandoFalha ? 'degraded' : 'online'
            "
          ></span>
          Vitrine :5071
          <span v-if="vitrineOnline && vitrineSimulandoFalha" class="text-warning">(503)</span>
        </span>
      </div>
    </div>
  </header>
</template>
