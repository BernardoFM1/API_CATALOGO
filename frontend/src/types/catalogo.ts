export interface Categoria {
  categoriaId: number
  nome: string
  imageURL: string
  totalProdutos: number
}

export interface CategoriaPayload {
  nome: string
  imageURL: string
}

export interface Produto {
  produtoID: number
  nome: string
  descricao: string
  preco: number
  imagemUrl: string
  estoque: number
  dataCadastro: string
  dataAtualizacao?: string | null
  categoriaId: number
  categoriaNome: string
}

export interface ProdutoPayload {
  nome: string
  descricao: string
  preco: number
  imagemUrl: string
  estoque: number
  categoriaId: number
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface SyncOperationResult {
  sucesso: boolean
  acao: string
  httpStatusCode?: number | null
  latenciaMs: number
  mensagemErro?: string | null
  dataHoraUtc: string
}

export interface ProdutoMutationResponse {
  produto: Produto
  sync: SyncOperationResult
}

export interface SyncLog {
  syncLogId: number
  produtoId: number
  produtoNome: string
  acao: string
  sucesso: boolean
  httpStatusCode?: number | null
  latenciaMs: number
  mensagemErro?: string | null
  destinoUrl: string
  dataHoraUtc: string
}

export interface SyncLogsSummary {
  totalTentativas: number
  totalSucessos: number
  totalFalhas: number
  latenciaMediaMs: number
  logs: SyncLog[]
}

export interface ProdutoVitrine {
  produtoOrigemId: number
  nome: string
  descricao: string
  preco: number
  imagemUrl: string
  estoque: number
  categoriaId: number
  categoriaNome: string
  dataCadastro: string
  dataAtualizacaoOrigem?: string | null
  ultimaSincronizacaoUtc: string
}

export interface CatalogoHealth {
  servico: string
  status: string
  databaseProvider: string
  totalProdutos: number
  totalCategorias: number
  timestampUtc: string
}

export interface VitrineStatus {
  servico: string
  status: string
  simularFalha: boolean
  databaseProvider: string
  totalProdutos: number
  ultimaSincronizacaoUtc?: string | null
  timestampUtc: string
}

export interface ToastMessage {
  id: number
  type: 'success' | 'warning' | 'danger' | 'info'
  title: string
  body: string
}
