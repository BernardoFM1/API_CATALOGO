<script setup lang="ts">
import { reactive, ref } from 'vue'
import { catalogoApi, extrairMensagensErro } from '../services/api'
import type { Categoria, CategoriaPayload } from '../types/catalogo'
import { fallbackImagem } from '../utils/formatters'

defineProps<{
  categorias: Categoria[]
}>()

const emit = defineEmits<{
  (e: 'refresh-categories'): void
  (e: 'notify', payload: { type: 'success' | 'warning' | 'danger' | 'info'; title: string; body: string }): void
}>()

const modalAberto = ref(false)
const editando = ref<Categoria | null>(null)
const salvando = ref(false)
const errosModal = ref<string[]>([])
const excluindoId = ref<number | null>(null)

const form = reactive<CategoriaPayload>({
  nome: '',
  imageURL: ''
})

function abrirNova() {
  editando.value = null
  errosModal.value = []
  form.nome = ''
  form.imageURL = 'https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=400&q=80'
  modalAberto.value = true
}

function abrirEdicao(cat: Categoria) {
  editando.value = cat
  errosModal.value = []
  form.nome = cat.nome
  form.imageURL = cat.imageURL
  modalAberto.value = true
}

async function salvarCategoria() {
  salvando.value = true
  errosModal.value = []

  try {
    if (editando.value) {
      await catalogoApi.atualizarCategoria(editando.value.categoriaId, { ...form })
      emit('notify', {
        type: 'success',
        title: 'Categoria atualizada',
        body: `A categoria "${form.nome}" foi salva.`
      })
    } else {
      await catalogoApi.criarCategoria({ ...form })
      emit('notify', {
        type: 'success',
        title: 'Categoria criada',
        body: `A categoria "${form.nome}" já está disponível para os produtos.`
      })
    }
    modalAberto.value = false
    emit('refresh-categories')
  } catch (err) {
    errosModal.value = extrairMensagensErro(err)
  } finally {
    salvando.value = false
  }
}

async function excluirCategoria(cat: Categoria) {
  const confirmado = window.confirm(`Deseja excluir a categoria "${cat.nome}"?`)
  if (!confirmado) return

  excluindoId.value = cat.categoriaId
  try {
    await catalogoApi.excluirCategoria(cat.categoriaId)
    emit('notify', {
      type: 'success',
      title: 'Categoria excluída',
      body: `A categoria "${cat.nome}" foi removida.`
    })
    emit('refresh-categories')
  } catch (err) {
    emit('notify', {
      type: 'warning',
      title: 'Não foi possível excluir',
      body: extrairMensagensErro(err)[0]
    })
  } finally {
    excluindoId.value = null
  }
}
</script>

<template>
  <section>
    <div class="d-flex flex-wrap align-items-center justify-content-between gap-3 mb-4">
      <div>
        <h1 class="h4 fw-bold mb-1">Categorias</h1>
        <p class="text-secondary mb-0" style="font-size: 0.9rem;">
          Organize os produtos por grupo. Categorias com produtos vinculados são protegidas contra exclusão (HTTP 409).
        </p>
      </div>
      <button type="button" class="btn btn-primary" @click="abrirNova">
        <i class="bi bi-plus-lg me-1"></i>
        Nova categoria
      </button>
    </div>

    <div class="surface-panel overflow-hidden">
      <div v-if="categorias.length === 0" class="p-5 text-center">
        <i class="bi bi-tags fs-2 text-secondary d-block mb-2"></i>
        <div class="fw-semibold mb-1">Nenhuma categoria cadastrada</div>
        <button type="button" class="btn btn-sm btn-primary mt-2" @click="abrirNova">
          Criar categoria
        </button>
      </div>

      <div v-else class="table-responsive">
        <table class="table table-hover mb-0">
          <thead>
            <tr>
              <th class="ps-3">ID</th>
              <th>Categoria</th>
              <th class="text-end">Produtos vinculados</th>
              <th class="text-end pe-3">Ações</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="cat in categorias" :key="cat.categoriaId">
              <td class="ps-3 text-secondary tabular-nums">#{{ cat.categoriaId }}</td>
              <td>
                <div class="d-flex align-items-center gap-3">
                  <img
                    :src="cat.imageURL"
                    :alt="cat.nome"
                    class="category-thumb"
                    @error="fallbackImagem"
                  />
                  <span class="fw-semibold">{{ cat.nome }}</span>
                </div>
              </td>
              <td class="text-end tabular-nums">
                <span class="badge text-bg-light border text-dark">
                  {{ cat.totalProdutos }} produto(s)
                </span>
              </td>
              <td class="text-end pe-3">
                <div class="btn-group btn-group-sm">
                  <button
                    type="button"
                    class="btn btn-outline-secondary"
                    @click="abrirEdicao(cat)"
                  >
                    <i class="bi bi-pencil me-1"></i>
                    Editar
                  </button>
                  <button
                    type="button"
                    class="btn btn-outline-danger"
                    :disabled="excluindoId === cat.categoriaId"
                    @click="excluirCategoria(cat)"
                  >
                    <span
                      v-if="excluindoId === cat.categoriaId"
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
    </div>

    <!-- Modal de Categoria -->
    <div v-if="modalAberto" class="modal-backdrop-custom" @click.self="modalAberto = false">
      <div class="modal-dialog-custom" style="max-width: 480px;" role="dialog" aria-modal="true">
        <div class="d-flex align-items-center justify-content-between px-4 py-3 border-bottom">
          <h5 class="mb-0 fw-bold">
            {{ editando ? `Editar categoria #${editando.categoriaId}` : 'Nova categoria' }}
          </h5>
          <button type="button" class="btn-close" @click="modalAberto = false"></button>
        </div>

        <form class="p-4" @submit.prevent="salvarCategoria">
          <div v-if="errosModal.length > 0" class="alert alert-danger py-2 px-3 mb-3">
            <ul class="mb-0 ps-3 small">
              <li v-for="(erro, idx) in errosModal" :key="idx">{{ erro }}</li>
            </ul>
          </div>

          <div class="mb-3">
            <label class="form-label fw-medium">Nome</label>
            <input
              v-model="form.nome"
              type="text"
              class="form-control"
              placeholder="Ex.: Padaria"
              required
              minlength="2"
              maxlength="80"
            />
          </div>

          <div class="mb-4">
            <label class="form-label fw-medium">URL da imagem</label>
            <input
              v-model="form.imageURL"
              type="text"
              class="form-control"
              placeholder="https://..."
              required
              maxlength="300"
            />
          </div>

          <div class="d-flex justify-content-end gap-2 pt-2 border-top">
            <button
              type="button"
              class="btn btn-outline-secondary"
              :disabled="salvando"
              @click="modalAberto = false"
            >
              Cancelar
            </button>
            <button type="submit" class="btn btn-primary" :disabled="salvando">
              <span v-if="salvando" class="spinner-border spinner-border-sm me-2"></span>
              {{ editando ? 'Salvar alterações' : 'Criar categoria' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </section>
</template>
