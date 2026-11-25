import axios, { AxiosError } from 'axios'
import type {
  CatalogoHealth,
  Categoria,
  CategoriaPayload,
  PagedResult,
  Produto,
  ProdutoMutationResponse,
  ProdutoPayload,
  ProdutoVitrine,
  SyncLogsSummary,
  SyncOperationResult,
  VitrineStatus
} from '../types/catalogo'

export const CATALOGO_API_URL =
  import.meta.env.VITE_CATALOGO_API_URL || 'http://localhost:5070'
export const VITRINE_API_URL =
  import.meta.env.VITE_VITRINE_API_URL || 'http://localhost:5071'

const catalogoClient = axios.create({
  baseURL: CATALOGO_API_URL,
  timeout: 8000
})

const vitrineClient = axios.create({
  baseURL: VITRINE_API_URL,
  timeout: 6000
})

export function extrairMensagensErro(err: unknown): string[] {
  const axiosErr = err as AxiosError<{
    message?: string
    title?: string
    errors?: Record<string, string[]>
  }>

  if (axiosErr?.response?.data) {
    const data = axiosErr.response.data
    if (data.errors) {
      const msgs = Object.values(data.errors).flat()
      if (msgs.length > 0) return msgs
    }
    if (data.message) return [data.message]
    if (data.title) return [data.title]
  }

  if (axiosErr?.code === 'ERR_NETWORK') {
    return ['Não foi possível conectar ao servidor da API. Verifique se o backend está em execução.']
  }

  return ['Ocorreu um erro inesperado ao comunicar com a API.']
}

export interface ProdutoFiltros {
  search?: string
  categoriaId?: number | ''
  sortBy?: string
  sortDir?: 'asc' | 'desc'
  page?: number
  pageSize?: number
}

export const catalogoApi = {
  async getHealth(): Promise<CatalogoHealth> {
    const { data } = await catalogoClient.get<CatalogoHealth>('/health')
    return data
  },

  async getProdutos(filtros: ProdutoFiltros = {}): Promise<PagedResult<Produto>> {
    const params: Record<string, string | number> = {
      page: filtros.page ?? 1,
      pageSize: filtros.pageSize ?? 8,
      sortBy: filtros.sortBy ?? 'nome',
      sortDir: filtros.sortDir ?? 'asc'
    }

    if (filtros.search?.trim()) params.search = filtros.search.trim()
    if (filtros.categoriaId) params.categoriaId = filtros.categoriaId

    const { data } = await catalogoClient.get<PagedResult<Produto>>('/Produtos', { params })
    return data
  },

  async criarProduto(payload: ProdutoPayload): Promise<ProdutoMutationResponse> {
    const { data } = await catalogoClient.post<ProdutoMutationResponse>('/Produtos', payload)
    return data
  },

  async atualizarProduto(id: number, payload: ProdutoPayload): Promise<ProdutoMutationResponse> {
    const { data } = await catalogoClient.put<ProdutoMutationResponse>(`/Produtos/${id}`, payload)
    return data
  },

  async excluirProduto(id: number): Promise<ProdutoMutationResponse> {
    const { data } = await catalogoClient.delete<ProdutoMutationResponse>(`/Produtos/${id}`)
    return data
  },

  async getCategorias(): Promise<Categoria[]> {
    const { data } = await catalogoClient.get<Categoria[]>('/Categorias')
    return data
  },

  async criarCategoria(payload: CategoriaPayload): Promise<Categoria> {
    const { data } = await catalogoClient.post<Categoria>('/Categorias', payload)
    return data
  },

  async atualizarCategoria(id: number, payload: CategoriaPayload): Promise<Categoria> {
    const { data } = await catalogoClient.put<Categoria>(`/Categorias/${id}`, payload)
    return data
  },

  async excluirCategoria(id: number): Promise<void> {
    await catalogoClient.delete(`/Categorias/${id}`)
  },

  async getSyncLogs(limit = 40): Promise<SyncLogsSummary> {
    const { data } = await catalogoClient.get<SyncLogsSummary>('/Sync/logs', {
      params: { limit }
    })
    return data
  },

  async ressincronizarProduto(id: number): Promise<SyncOperationResult> {
    const { data } = await catalogoClient.post<SyncOperationResult>(`/Sync/produtos/${id}`)
    return data
  },

  async reconciliarCatalogo(): Promise<SyncOperationResult> {
    const { data } = await catalogoClient.post<SyncOperationResult>('/Sync/reconciliar')
    return data
  },

  async limparSyncLogs(): Promise<void> {
    await catalogoClient.delete('/Sync/logs')
  }
}

export const vitrineApi = {
  async getStatus(): Promise<VitrineStatus> {
    const { data } = await vitrineClient.get<VitrineStatus>('/vitrine/status')
    return data
  },

  async getProdutos(): Promise<ProdutoVitrine[]> {
    const { data } = await vitrineClient.get<ProdutoVitrine[]>('/vitrine/produtos')
    return data
  },

  async alternarSimulacaoFalha(simularFalha: boolean): Promise<{ simularFalha: boolean; message: string }> {
    const { data } = await vitrineClient.post('/vitrine/simular-falha', { simularFalha })
    return data
  }
}
