<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import CategoriasView from './components/CategoriasView.vue'
import NavbarHeader, { type TabKey } from './components/NavbarHeader.vue'
import ProdutosView from './components/ProdutosView.vue'
import SincronizacaoView from './components/SincronizacaoView.vue'
import ToastContainer from './components/ToastContainer.vue'
import { catalogoApi, vitrineApi } from './services/api'
import type {
  CatalogoHealth,
  Categoria,
  Produto,
  ProdutoVitrine,
  SyncLogsSummary,
  SyncOperationResult,
  ToastMessage,
  VitrineStatus
} from './types/catalogo'

const activeTab = ref<TabKey>('produtos')

const categorias = ref<Categoria[]>([])
const catalogoHealth = ref<CatalogoHealth | null>(null)
const vitrineStatus = ref<VitrineStatus | null>(null)
const todosProdutosCatalogo = ref<Produto[]>([])
const produtosVitrine = ref<ProdutoVitrine[]>([])
const syncSummary = ref<SyncLogsSummary | null>(null)
const carregandoSync = ref(false)

const toasts = ref<ToastMessage[]>([])
let nextToastId = 1

function addToast(payload: Omit<ToastMessage, 'id'>) {
  const id = nextToastId++
  toasts.value.push({ id, ...payload })
  window.setTimeout(() => dismissToast(id), 5500)
}

function dismissToast(id: number) {
  toasts.value = toasts.value.filter((t) => t.id !== id)
}

const divergenciasCount = computed(() => {
  if (!catalogoHealth.value || !vitrineStatus.value) return 0
  const mapA = new Map(todosProdutosCatalogo.value.map((p) => [p.produtoID, p]))
  const mapB = new Map(produtosVitrine.value.map((p) => [p.produtoOrigemId, p]))
  const ids = new Set([...mapA.keys(), ...mapB.keys()])

  let count = 0
  for (const id of ids) {
    const a = mapA.get(id)
    const b = mapB.get(id)
    if (!a || !b) {
      count++
      continue
    }
    if (
      a.nome !== b.nome ||
      Number(a.preco) !== Number(b.preco) ||
      Number(a.estoque) !== Number(b.estoque) ||
      a.categoriaNome !== b.categoriaNome
    ) {
      count++
    }
  }
  return count
})

async function carregarCategorias() {
  try {
    categorias.value = await catalogoApi.getCategorias()
  } catch {
    categorias.value = []
  }
}

async function carregarStatusESincronizacao() {
  carregandoSync.value = true

  const [healthRes, produtosRes, logsRes, vitrineStatusRes, vitrineProdutosRes] =
    await Promise.allSettled([
      catalogoApi.getHealth(),
      catalogoApi.getProdutos({ page: 1, pageSize: 100 }),
      catalogoApi.getSyncLogs(40),
      vitrineApi.getStatus(),
      vitrineApi.getProdutos()
    ])

  catalogoHealth.value = healthRes.status === 'fulfilled' ? healthRes.value : null
  todosProdutosCatalogo.value = produtosRes.status === 'fulfilled' ? produtosRes.value.items : []
  syncSummary.value = logsRes.status === 'fulfilled' ? logsRes.value : null
  vitrineStatus.value = vitrineStatusRes.status === 'fulfilled' ? vitrineStatusRes.value : null
  produtosVitrine.value =
    vitrineProdutosRes.status === 'fulfilled' ? vitrineProdutosRes.value : []

  carregandoSync.value = false
}

async function handleCatalogChanged(sync?: SyncOperationResult, acaoLabel = 'Operação concluída') {
  if (sync) {
    if (sync.sucesso) {
      addToast({
        type: 'success',
        title: `${acaoLabel} e sincronizado`,
        body: `Salvo no APICatalogo e replicado via webhook para a APIVitrine em ${sync.latenciaMs} ms.`
      })
    } else {
      addToast({
        type: 'warning',
        title: `${acaoLabel} (falha no webhook)`,
        body:
          sync.mensagemErro ||
          'Salvo no APICatalogo, mas a APIVitrine não respondeu. Ressincronize na aba Sincronização.'
      })
    }
  }

  await Promise.all([carregarCategorias(), carregarStatusESincronizacao()])
}

function handleTabChange(tab: TabKey) {
  activeTab.value = tab
  if (tab === 'sincronizacao') {
    carregarStatusESincronizacao()
  } else if (tab === 'categorias') {
    carregarCategorias()
  }
}

onMounted(async () => {
  await Promise.all([carregarCategorias(), carregarStatusESincronizacao()])
})
</script>

<template>
  <div class="min-vh-100 d-flex flex-column">
    <NavbarHeader
      :active-tab="activeTab"
      :catalogo-online="Boolean(catalogoHealth)"
      :vitrine-online="Boolean(vitrineStatus)"
      :vitrine-simulando-falha="Boolean(vitrineStatus?.simularFalha)"
      :divergencias-count="divergenciasCount"
      @change-tab="handleTabChange"
    />

    <main class="container-xl py-4 flex-grow-1">
      <ProdutosView
        v-if="activeTab === 'produtos'"
        :categorias="categorias"
        @catalog-changed="handleCatalogChanged"
        @notify="addToast"
      />

      <CategoriasView
        v-else-if="activeTab === 'categorias'"
        :categorias="categorias"
        @refresh-categories="carregarCategorias"
        @notify="addToast"
      />

      <SincronizacaoView
        v-else
        :catalogo-health="catalogoHealth"
        :vitrine-status="vitrineStatus"
        :produtos-catalogo="todosProdutosCatalogo"
        :produtos-vitrine="produtosVitrine"
        :sync-summary="syncSummary"
        :carregando="carregandoSync"
        @refresh-all="carregarStatusESincronizacao"
        @notify="addToast"
      />
    </main>

    <footer class="border-top bg-white py-3 mt-auto">
      <div class="container-xl d-flex flex-wrap justify-content-between align-items-center gap-2 small text-secondary">
        <span>APICatalogo + APIVitrine · Sistema de cadastro, consulta e sincronização por Webhook</span>
        <span>ASP.NET Core 8 · EF Core (SQLite / SQL Server) · Vue 3 + TypeScript + Bootstrap 5</span>
      </div>
    </footer>

    <ToastContainer :toasts="toasts" @dismiss="dismissToast" />
  </div>
</template>
