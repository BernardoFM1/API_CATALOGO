<script setup lang="ts">
import { computed, ref } from 'vue'
import { catalogoApi, extrairMensagensErro, vitrineApi } from '../services/api'
import type {
  CatalogoHealth,
  Produto,
  ProdutoVitrine,
  SyncLogsSummary,
  VitrineStatus
} from '../types/catalogo'
import { formatarDataHora, formatarMoeda } from '../utils/formatters'

const props = defineProps<{
  catalogoHealth: CatalogoHealth | null
  vitrineStatus: VitrineStatus | null
  produtosCatalogo: Produto[]
  produtosVitrine: ProdutoVitrine[]
  syncSummary: SyncLogsSummary | null
  carregando: boolean
}>()

const emit = defineEmits<{
  (e: 'refresh-all'): void
  (e: 'notify', payload: { type: 'success' | 'warning' | 'danger' | 'info'; title: string; body: string }): void
}>()

const reconciliando = ref(false)
const ressincronizandoId = ref<number | null>(null)
const alternandoFalha = ref(false)
const limpandoLogs = ref(false)

interface ComparativoItem {
  id: number
  catalogo: Produto | null
  vitrine: ProdutoVitrine | null
  status: 'sincronizado' | 'divergente' | 'faltando' | 'orfao'
  detalhe: string
}

const comparativo = computed<ComparativoItem[]>(() => {
  const mapaCatalogo = new Map(props.produtosCatalogo.map((p) => [p.produtoID, p]))
  const mapaVitrine = new Map(props.produtosVitrine.map((p) => [p.produtoOrigemId, p]))
  const todosIds = Array.from(new Set([...mapaCatalogo.keys(), ...mapaVitrine.keys()])).sort(
    (a, b) => a - b
  )

  return todosIds.map((id) => {
    const cat = mapaCatalogo.get(id) ?? null
    const vit = mapaVitrine.get(id) ?? null

    if (cat && !vit) {
      return {
        id,
        catalogo: cat,
        vitrine: null,
        status: 'faltando',
        detalhe: 'Existe no Catálogo Master, ainda não replicado na APIVitrine.'
      }
    }

    if (!cat && vit) {
      return {
        id,
        catalogo: null,
        vitrine: vit,
        status: 'orfao',
        detalhe: 'Excluído no Catálogo Master, mas permanece na APIVitrine.'
      }
    }

    if (cat && vit) {
      const diferencas: string[] = []
      if (cat.nome !== vit.nome) diferencas.push('nome')
      if (Number(cat.preco) !== Number(vit.preco)) diferencas.push('preço')
      if (Number(cat.estoque) !== Number(vit.estoque)) diferencas.push('estoque')
      if (cat.categoriaNome !== vit.categoriaNome) diferencas.push('categoria')

      if (diferencas.length > 0) {
        return {
          id,
          catalogo: cat,
          vitrine: vit,
          status: 'divergente',
          detalhe: `Divergência em: ${diferencas.join(', ')}.`
        }
      }

      return {
        id,
        catalogo: cat,
        vitrine: vit,
        status: 'sincronizado',
        detalhe: 'Dados idênticos nos dois bancos.'
      }
    }

    return { id, catalogo: null, vitrine: null, status: 'sincronizado', detalhe: '' }
  })
})

const totalDivergencias = computed(() =>
  comparativo.value.filter((c) => c.status !== 'sincronizado').length
)

async function reconciliarTudo() {
  reconciliando.value = true
  try {
    const res = await catalogoApi.reconciliarCatalogo()
    if (res.sucesso) {
      emit('notify', {
        type: 'success',
        title: 'Catálogo reconciliado',
        body: `Todos os produtos foram sincronizados com a APIVitrine em ${res.latenciaMs} ms.`
      })
    } else {
      emit('notify', {
        type: 'warning',
        title: 'Falha ao reconciliar',
        body: res.mensagemErro || 'A APIVitrine não aceitou o webhook.'
      })
    }
    emit('refresh-all')
  } catch (err) {
    emit('notify', {
      type: 'danger',
      title: 'Erro na reconciliação',
      body: extrairMensagensErro(err)[0]
    })
  } finally {
    reconciliando.value = false
  }
}

async function ressincronizarProduto(id: number) {
  ressincronizandoId.value = id
  try {
    const res = await catalogoApi.ressincronizarProduto(id)
    if (res.sucesso) {
      emit('notify', {
        type: 'success',
        title: `Produto #${id} sincronizado`,
        body: `Webhook entregue à APIVitrine em ${res.latenciaMs} ms.`
      })
    } else {
      emit('notify', {
        type: 'warning',
        title: `Falha ao sincronizar produto #${id}`,
        body: res.mensagemErro || 'Erro na entrega do webhook.'
      })
    }
    emit('refresh-all')
  } catch (err) {
    emit('notify', {
      type: 'danger',
      title: 'Erro ao ressincronizar',
      body: extrairMensagensErro(err)[0]
    })
  } finally {
    ressincronizandoId.value = null
  }
}

async function alternarFalhaVitrine() {
  if (!props.vitrineStatus) return
  alternandoFalha.value = true
  try {
    const novoValor = !props.vitrineStatus.simularFalha
    const resp = await vitrineApi.alternarSimulacaoFalha(novoValor)
    emit('notify', {
      type: novoValor ? 'warning' : 'success',
      title: novoValor ? 'Falha simulada ativada (HTTP 503)' : 'APIVitrine restaurada',
      body: resp.message
    })
    emit('refresh-all')
  } catch (err) {
    emit('notify', {
      type: 'danger',
      title: 'APIVitrine inacessível',
      body: extrairMensagensErro(err)[0]
    })
  } finally {
    alternandoFalha.value = false
  }
}

async function limparLogs() {
  limpandoLogs.value = true
  try {
    await catalogoApi.limparSyncLogs()
    emit('notify', {
      type: 'info',
      title: 'Histórico limpo',
      body: 'Os registros de envio de webhooks foram removidos.'
    })
    emit('refresh-all')
  } finally {
    limpandoLogs.value = false
  }
}

function badgeStatus(status: ComparativoItem['status']): { label: string; css: string } {
  switch (status) {
    case 'sincronizado':
      return { label: 'Sincronizado', css: 'text-bg-success' }
    case 'divergente':
      return { label: 'Divergente', css: 'text-bg-warning text-dark' }
    case 'faltando':
      return { label: 'Faltando na Vitrine', css: 'text-bg-danger' }
    case 'orfao':
      return { label: 'Órfão na Vitrine', css: 'text-bg-secondary' }
  }
}
</script>

<template>
  <section>
    <div class="d-flex flex-wrap align-items-center justify-content-between gap-3 mb-4">
      <div>
        <h1 class="h4 fw-bold mb-1">Sincronização entre aplicações</h1>
        <p class="text-secondary mb-0" style="font-size: 0.9rem;">
          Integração via Webhook HTTP autenticado (<code>X-Webhook-Secret</code>) entre o
          <strong>APICatalogo (App A)</strong> e a <strong>APIVitrine (App B)</strong>, cada um com seu próprio banco SQL.
        </p>
      </div>

      <div class="d-flex flex-wrap gap-2">
        <button
          type="button"
          class="btn btn-outline-secondary"
          :disabled="carregando"
          @click="$emit('refresh-all')"
        >
          <i class="bi bi-arrow-clockwise me-1"></i>
          Atualizar
        </button>
        <button
          type="button"
          class="btn btn-primary"
          :disabled="reconciliando"
          @click="reconciliarTudo"
        >
          <span v-if="reconciliando" class="spinner-border spinner-border-sm me-2"></span>
          <i v-else class="bi bi-cloud-upload me-1"></i>
          Sincronizar catálogo inteiro
        </button>
      </div>
    </div>

    <!-- Status das 2 APIs -->
    <div class="row g-3 mb-4">
      <div class="col-lg-6">
        <div class="surface-panel p-3 h-100">
          <div class="d-flex align-items-center justify-content-between mb-2">
            <div class="fw-bold d-flex align-items-center gap-2">
              <span class="badge text-bg-primary">App A</span>
              APICatalogo (Master)
            </div>
            <span
              class="badge"
              :class="catalogoHealth ? 'text-bg-success' : 'text-bg-danger'"
            >
              {{ catalogoHealth ? 'Online :5070' : 'Offline' }}
            </span>
          </div>
          <div class="small text-secondary d-flex flex-wrap gap-3">
            <span>Banco: <strong>{{ catalogoHealth?.databaseProvider ?? '—' }}</strong></span>
            <span class="tabular-nums">
              Produtos: <strong>{{ catalogoHealth?.totalProdutos ?? 0 }}</strong>
            </span>
            <span class="tabular-nums">
              Categorias: <strong>{{ catalogoHealth?.totalCategorias ?? 0 }}</strong>
            </span>
          </div>
        </div>
      </div>

      <div class="col-lg-6">
        <div class="surface-panel p-3 h-100">
          <div class="d-flex align-items-center justify-content-between mb-2">
            <div class="fw-bold d-flex align-items-center gap-2">
              <span class="badge text-bg-dark">App B</span>
              APIVitrine (Réplica / PDV)
            </div>
            <span
              class="badge"
              :class="
                !vitrineStatus
                  ? 'text-bg-danger'
                  : vitrineStatus.simularFalha
                    ? 'text-bg-warning text-dark'
                    : 'text-bg-success'
              "
            >
              {{
                !vitrineStatus
                  ? 'Offline :5071'
                  : vitrineStatus.simularFalha
                    ? 'Simulando falha (503)'
                    : 'Online :5071'
              }}
            </span>
          </div>

          <div class="small text-secondary d-flex flex-wrap align-items-center justify-content-between gap-2">
            <div class="d-flex flex-wrap gap-3">
              <span>Banco: <strong>{{ vitrineStatus?.databaseProvider ?? '—' }}</strong></span>
              <span class="tabular-nums">
                Replicados: <strong>{{ vitrineStatus?.totalProdutos ?? 0 }}</strong>
              </span>
            </div>

            <button
              v-if="vitrineStatus"
              type="button"
              class="btn btn-sm"
              :class="vitrineStatus.simularFalha ? 'btn-success' : 'btn-outline-warning text-dark'"
              :disabled="alternandoFalha"
              @click="alternarFalhaVitrine"
            >
              <i
                class="bi me-1"
                :class="vitrineStatus.simularFalha ? 'bi-play-circle' : 'bi-lightning-charge'"
              ></i>
              {{
                vitrineStatus.simularFalha
                  ? 'Restaurar APIVitrine'
                  : 'Simular falha (HTTP 503)'
              }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Comparativo lado a lado -->
    <div class="surface-panel overflow-hidden mb-4">
      <div class="px-3 py-3 border-bottom d-flex flex-wrap align-items-center justify-content-between gap-2">
        <div>
          <h2 class="h6 fw-bold mb-0">Comparativo de bancos: Catálogo Master × Vitrine Externa</h2>
          <small class="text-secondary">
            {{
              totalDivergencias === 0
                ? 'Todos os registros estão consistentes entre as duas aplicações.'
                : `${totalDivergencias} divergência(s) detectada(s) — clique em "Ressincronizar" ou "Sincronizar catálogo inteiro".`
            }}
          </small>
        </div>
        <span
          class="badge rounded-pill"
          :class="totalDivergencias === 0 ? 'text-bg-success' : 'text-bg-warning text-dark'"
        >
          {{ totalDivergencias === 0 ? '100% consistente' : `${totalDivergencias} pendente(s)` }}
        </span>
      </div>

      <div class="table-responsive">
        <table class="table table-hover mb-0">
          <thead>
            <tr>
              <th class="ps-3">ID</th>
              <th>App A — APICatalogo (catalogo.db)</th>
              <th>App B — APIVitrine (vitrine.db)</th>
              <th>Status</th>
              <th class="text-end pe-3">Ação</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="comparativo.length === 0">
              <td colspan="5" class="text-center text-secondary py-4">
                Nenhum produto encontrado nas APIs.
              </td>
            </tr>
            <tr v-for="item in comparativo" :key="item.id">
              <td class="ps-3 text-secondary tabular-nums">#{{ item.id }}</td>
              <td>
                <template v-if="item.catalogo">
                  <div class="fw-semibold">{{ item.catalogo.nome }}</div>
                  <div class="small text-secondary tabular-nums">
                    {{ formatarMoeda(item.catalogo.preco) }} · Estoque: {{ item.catalogo.estoque }} ·
                    {{ item.catalogo.categoriaNome }}
                  </div>
                </template>
                <span v-else class="text-secondary fst-italic small">Ausente no Master</span>
              </td>
              <td>
                <template v-if="item.vitrine">
                  <div class="fw-semibold">{{ item.vitrine.nome }}</div>
                  <div class="small text-secondary tabular-nums">
                    {{ formatarMoeda(item.vitrine.preco) }} · Estoque: {{ item.vitrine.estoque }} ·
                    {{ item.vitrine.categoriaNome }}
                  </div>
                </template>
                <span v-else class="text-danger fst-italic small">Não replicado na Vitrine</span>
              </td>
              <td>
                <span class="badge" :class="badgeStatus(item.status).css">
                  {{ badgeStatus(item.status).label }}
                </span>
                <div class="small text-secondary mt-1">{{ item.detalhe }}</div>
              </td>
              <td class="text-end pe-3">
                <button
                  v-if="item.catalogo"
                  type="button"
                  class="btn btn-sm btn-outline-primary"
                  :disabled="ressincronizandoId === item.id"
                  @click="ressincronizarProduto(item.id)"
                >
                  <span
                    v-if="ressincronizandoId === item.id"
                    class="spinner-border spinner-border-sm me-1"
                  ></span>
                  Ressincronizar
                </button>
                <button
                  v-else
                  type="button"
                  class="btn btn-sm btn-outline-secondary"
                  :disabled="reconciliando"
                  @click="reconciliarTudo"
                >
                  Limpar órfão
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Histórico de Webhooks -->
    <div class="surface-panel overflow-hidden">
      <div class="px-3 py-3 border-bottom d-flex flex-wrap align-items-center justify-content-between gap-2">
        <div>
          <h2 class="h6 fw-bold mb-0">Histórico de envios de Webhook</h2>
          <div v-if="syncSummary" class="small text-secondary d-flex flex-wrap gap-3 mt-1 tabular-nums">
            <span>Total: <strong>{{ syncSummary.totalTentativas }}</strong></span>
            <span class="text-success">Sucessos: <strong>{{ syncSummary.totalSucessos }}</strong></span>
            <span class="text-danger">Falhas: <strong>{{ syncSummary.totalFalhas }}</strong></span>
            <span>Latência média: <strong>{{ syncSummary.latenciaMediaMs }} ms</strong></span>
          </div>
        </div>
        <button
          v-if="syncSummary && syncSummary.logs.length > 0"
          type="button"
          class="btn btn-sm btn-outline-secondary"
          :disabled="limpandoLogs"
          @click="limparLogs"
        >
          Limpar histórico
        </button>
      </div>

      <div class="table-responsive">
        <table class="table table-sm table-hover mb-0">
          <thead>
            <tr>
              <th class="ps-3">Data / Hora</th>
              <th>Ação</th>
              <th>Produto</th>
              <th>Resultado</th>
              <th class="text-end">Latência</th>
              <th class="pe-3">Detalhe</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="!syncSummary || syncSummary.logs.length === 0">
              <td colspan="6" class="text-center text-secondary py-4">
                Nenhum webhook enviado ainda. Cadastre, edite ou clique em "Sincronizar catálogo inteiro".
              </td>
            </tr>
            <tr v-for="log in syncSummary?.logs" :key="log.syncLogId">
              <td class="ps-3 small text-secondary tabular-nums">
                {{ formatarDataHora(log.dataHoraUtc) }}
              </td>
              <td>
                <span class="badge text-bg-light border text-dark">{{ log.acao }}</span>
              </td>
              <td class="fw-medium">{{ log.produtoNome }}</td>
              <td>
                <span class="badge" :class="log.sucesso ? 'text-bg-success' : 'text-bg-danger'">
                  {{
                    log.sucesso
                      ? `HTTP ${log.httpStatusCode ?? 200}`
                      : log.httpStatusCode
                        ? `HTTP ${log.httpStatusCode}`
                        : 'Sem conexão'
                  }}
                </span>
              </td>
              <td class="text-end small tabular-nums">{{ log.latenciaMs }} ms</td>
              <td class="pe-3 small text-secondary">
                {{ log.mensagemErro || 'Entregue com sucesso' }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </section>
</template>
