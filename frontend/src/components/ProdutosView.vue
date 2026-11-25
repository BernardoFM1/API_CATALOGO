<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { catalogoApi, extrairMensagensErro } from '../services/api'
import type {
  Categoria,
  PagedResult,
  Produto,
  ProdutoPayload,
  SyncOperationResult
} from '../types/catalogo'
import { fallbackImagem, formatarDataHora, formatarMoeda } from '../utils/formatters'
import ProdutoModal from './ProdutoModal.vue'

const props = defineProps<{
  categorias: Categoria[]
}>()

const emit = defineEmits<{
  (e: 'catalog-changed', sync?: SyncOperationResult, acaoLabel?: string): void
  (e: 'notify', payload: { type: 'success' | 'warning' | 'danger' | 'info'; title: string; body: string }): void
}>()

const carregando = ref(false)
const erroCarregamento = ref<string | null>(null)
const resultado = ref<PagedResult<Produto>>({
  items: [],
  totalCount: 0,
  page: 1,
  pageSize: 8,
  totalPages: 1
})

const filtros = reactive({
  search: '',
  categoriaId: '' as number | '',
  ordenacao: 'nome:asc',
  page: 1,
  pageSize: 8
})

const modalAberto = ref(false)
const produtoSelecionado = ref<Produto | null>(null)
const salvando = ref(false)
const errosModal = ref<string[]>([])
const excluindoId = ref<number | null>(null)

let searchDebounce: number | undefined

const estoqueBaixoNaPagina = computed(() =>
  resultado.value.items.filter((p) => p.estoque <= 10).length
)

const valorEstoqueNaPagina = computed(() =>
  resultado.value.items.reduce((acc, p) => acc + p.preco * p.estoque, 0)
)

async function carregarProdutos() {
  carregando.value = true
  erroCarregamento.value = null

  const [sortBy, sortDir] = filtros.ordenacao.split(':') as [string, 'asc' | 'desc']

  try {
    resultado.value = await catalogoApi.getProdutos({
      search: filtros.search,
      categoriaId: filtros.categoriaId,
      sortBy,
      sortDir,
      page: filtros.page,
      pageSize: filtros.pageSize
    })
  } catch (err) {
    erroCarregamento.value = extrairMensagensErro(err)[0]
  } finally {
    carregando.value = false
  }
}

watch(
  () => filtros.search,
  () => {
    window.clearTimeout(searchDebounce)
    searchDebounce = window.setTimeout(() => {
      filtros.page = 1
      carregarProdutos()
    }, 280)
  }
)

watch(
  () => [filtros.categoriaId, filtros.ordenacao],
  () => {
    filtros.page = 1
    carregarProdutos()
  }
)

function mudarPagina(novaPagina: number) {
  if (novaPagina < 1 || novaPagina > resultado.value.totalPages) return
  filtros.page = novaPagina
  carregarProdutos()
}

function limparFiltros() {
  filtros.search = ''
  filtros.categoriaId = ''
  filtros.ordenacao = 'nome:asc'
  filtros.page = 1
  carregarProdutos()
}

function abrirNovo() {
  if (props.categorias.length === 0) {
    emit('notify', {
      type: 'warning',
      title: 'Cadastre uma categoria primeiro',
      body: 'É necessário ter ao menos uma categoria antes de criar produtos.'
    })
    return
  }
  produtoSelecionado.value = null
  errosModal.value = []
  modalAberto.value = true
}

function abrirEdicao(produto: Produto) {
  produtoSelecionado.value = produto
  errosModal.value = []
  modalAberto.value = true
}

async function salvarProduto(payload: ProdutoPayload) {
  salvando.value = true
  errosModal.value = []

  try {
    const isEdicao = Boolean(produtoSelecionado.value)
    const resp = isEdicao
      ? await catalogoApi.atualizarProduto(produtoSelecionado.value!.produtoID, payload)
      : await catalogoApi.criarProduto(payload)

    modalAberto.value = false
    await carregarProdutos()
    emit('catalog-changed', resp.sync, isEdicao ? 'Produto atualizado' : 'Produto cadastrado')
  } catch (err) {
    errosModal.value = extrairMensagensErro(err)
  } finally {
    salvando.value = false
  }
}

async function excluirProduto(produto: Produto) {
  const confirmado = window.confirm(
    `Excluir o produto "${produto.nome}"? A remoção também será enviada via webhook para a APIVitrine.`
  )
  if (!confirmado) return

  excluindoId.value = produto.produtoID
  try {
    const resp = await catalogoApi.excluirProduto(produto.produtoID)
    if (resultado.value.items.length === 1 && filtros.page > 1) {
      filtros.page -= 1
    }
    await carregarProdutos()
    emit('catalog-changed', resp.sync, 'Produto excluído')
  } catch (err) {
    emit('notify', {
      type: 'danger',
      title: 'Erro ao excluir produto',
      body: extrairMensagensErro(err)[0]
    })
  } finally {
    excluindoId.value = null
  }
}

onMounted(() => {
  carregarProdutos()
})
</script>

<template>
  <section>
    <div class="d-flex flex-wrap align-items-center justify-content-between gap-3 mb-4">
      <div>
        <h1 class="h4 fw-bold mb-1">Produtos do catálogo</h1>
        <p class="text-secondary mb-0" style="font-size: 0.9rem;">
          Cada criação, edição ou exclusão salva no SQLite/SQL Server local e dispara webhook para a APIVitrine.
        </p>
      </div>
      <button type="button" class="btn btn-primary" @click="abrirNovo">
        <i class="bi bi-plus-lg me-1"></i>
        Novo produto
      </button>
    </div>

    <!-- Métricas enxutas -->
    <div class="row g-3 mb-4">
      <div class="col-md-4">
        <div class="surface-panel p-3">
          <div class="metric-label">Produtos encontrados</div>
          <div class="metric-value tabular-nums">{{ resultado.totalCount }}</div>
        </div>
      </div>
      <div class="col-md-4">
        <div class="surface-panel p-3">
          <div class="metric-label">Valor em estoque (página atual)</div>
          <div class="metric-value tabular-nums">{{ formatarMoeda(valorEstoqueNaPagina) }}</div>
        </div>
      </div>
      <div class="col-md-4">
        <div class="surface-panel p-3">
          <div class="metric-label">Estoque baixo (≤ 10 un. na página)</div>
          <div class="metric-value tabular-nums" :class="{ 'text-warning': estoqueBaixoNaPagina > 0 }">
            {{ estoqueBaixoNaPagina }}
          </div>
        </div>
      </div>
    </div>

    <!-- Filtros -->
    <div class="surface-panel p-3 mb-3">
      <div class="row g-2 align-items-end">
        <div class="col-md-5">
          <label class="form-label small text-secondary mb-1">Buscar por nome ou descrição</label>
          <div class="input-group">
            <span class="input-group-text bg-white">
              <i class="bi bi-search text-secondary"></i>
            </span>
            <input
              v-model="filtros.search"
              type="text"
              class="form-control"
              placeholder="Ex.: Hambúrguer, Coca-Cola, Pudim..."
            />
          </div>
        </div>

        <div class="col-md-3">
          <label class="form-label small text-secondary mb-1">Categoria</label>
          <select v-model="filtros.categoriaId" class="form-select">
            <option value="">Todas as categorias</option>
            <option
              v-for="cat in categorias"
              :key="cat.categoriaId"
              :value="cat.categoriaId"
            >
              {{ cat.nome }} ({{ cat.totalProdutos }})
            </option>
          </select>
        </div>

        <div class="col-md-3">
          <label class="form-label small text-secondary mb-1">Ordenar por</label>
          <select v-model="filtros.ordenacao" class="form-select">
            <option value="nome:asc">Nome (A–Z)</option>
            <option value="nome:desc">Nome (Z–A)</option>
            <option value="preco:asc">Menor preço</option>
            <option value="preco:desc">Maior preço</option>
            <option value="estoque:asc">Menor estoque</option>
            <option value="datacadastro:desc">Mais recentes</option>
          </select>
        </div>

        <div class="col-md-1 d-grid">
          <button
            type="button"
            class="btn btn-outline-secondary"
            title="Limpar filtros"
            @click="limparFiltros"
          >
            <i class="bi bi-x-lg"></i>
          </button>
        </div>
      </div>
    </div>

    <!-- Tabela -->
    <div class="surface-panel overflow-hidden">
      <div v-if="erroCarregamento" class="p-4 text-center">
        <p class="text-danger fw-medium mb-2">{{ erroCarregamento }}</p>
        <button type="button" class="btn btn-sm btn-outline-primary" @click="carregarProdutos">
          Tentar novamente
        </button>
      </div>

      <div v-else-if="carregando" class="p-5 text-center text-secondary">
        <div class="spinner-border spinner-border-sm me-2"></div>
        Carregando produtos...
      </div>

      <div v-else-if="resultado.items.length === 0" class="p-5 text-center">
        <i class="bi bi-inbox fs-2 text-secondary d-block mb-2"></i>
        <div class="fw-semibold mb-1">Nenhum produto encontrado</div>
        <p class="text-secondary small mb-3">
          Ajuste os filtros de busca ou cadastre um novo produto no catálogo.
        </p>
        <button type="button" class="btn btn-sm btn-primary" @click="abrirNovo">
          Cadastrar primeiro produto
        </button>
      </div>

      <div v-else class="table-responsive">
        <table class="table table-hover mb-0">
          <thead>
            <tr>
              <th class="ps-3">Produto</th>
              <th>Categoria</th>
              <th class="text-end">Preço</th>
              <th class="text-end">Estoque</th>
              <th>Atualizado em</th>
              <th class="text-end pe-3">Ações</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="produto in resultado.items" :key="produto.produtoID">
              <td class="ps-3">
                <div class="d-flex align-items-center gap-3">
                  <img
                    :src="produto.imagemUrl"
                    :alt="produto.nome"
                    class="product-thumb"
                    @error="fallbackImagem"
                  />
                  <div>
                    <div class="fw-semibold">{{ produto.nome }}</div>
                    <div class="text-secondary small text-truncate" style="max-width: 320px;">
                      {{ produto.descricao }}
                    </div>
                  </div>
                </div>
              </td>
              <td>
                <span class="badge text-bg-light border text-dark fw-medium">
                  {{ produto.categoriaNome || `#${produto.categoriaId}` }}
                </span>
              </td>
              <td class="text-end fw-semibold tabular-nums">
                {{ formatarMoeda(produto.preco) }}
              </td>
              <td class="text-end tabular-nums">
                <span
                  class="badge rounded-pill"
                  :class="produto.estoque <= 10 ? 'text-bg-warning text-dark' : 'text-bg-success'"
                >
                  {{ produto.estoque }} un.
                </span>
              </td>
              <td class="small text-secondary tabular-nums">
                {{ formatarDataHora(produto.dataAtualizacao || produto.dataCadastro) }}
              </td>
              <td class="text-end pe-3">
                <div class="btn-group btn-group-sm">
                  <button
                    type="button"
                    class="btn btn-outline-secondary"
                    @click="abrirEdicao(produto)"
                  >
                    <i class="bi bi-pencil me-1"></i>
                    Editar
                  </button>
                  <button
                    type="button"
                    class="btn btn-outline-danger"
                    :disabled="excluindoId === produto.produtoID"
                    @click="excluirProduto(produto)"
                  >
                    <span
                      v-if="excluindoId === produto.produtoID"
                      class="spinner-border spinner-border-sm"
                    ></span>
                    <i v-else class="bi bi-trash"></i>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Paginação -->
      <div
        v-if="resultado.totalPages > 1"
        class="d-flex align-items-center justify-content-between px-3 py-2 border-top bg-light"
      >
        <small class="text-secondary tabular-nums">
          Página {{ resultado.page }} de {{ resultado.totalPages }} ({{ resultado.totalCount }} itens)
        </small>
        <ul class="pagination pagination-sm mb-0">
          <li class="page-item" :class="{ disabled: resultado.page <= 1 }">
            <button class="page-link" @click="mudarPagina(resultado.page - 1)">Anterior</button>
          </li>
          <li
            v-for="p in resultado.totalPages"
            :key="p"
            class="page-item"
            :class="{ active: p === resultado.page }"
          >
            <button class="page-link" @click="mudarPagina(p)">{{ p }}</button>
          </li>
          <li class="page-item" :class="{ disabled: resultado.page >= resultado.totalPages }">
            <button class="page-link" @click="mudarPagina(resultado.page + 1)">Próxima</button>
          </li>
        </ul>
      </div>
    </div>

    <ProdutoModal
      :visible="modalAberto"
      :produto="produtoSelecionado"
      :categorias="categorias"
      :salvando="salvando"
      :erros-api="errosModal"
      @close="modalAberto = false"
      @save="salvarProduto"
    />
  </section>
</template>
