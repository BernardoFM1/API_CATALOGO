<script setup lang="ts">
import { reactive, watch } from 'vue'
import type { Categoria, Produto, ProdutoPayload } from '../types/catalogo'

const props = defineProps<{
  visible: boolean
  produto: Produto | null
  categorias: Categoria[]
  salvando: boolean
  errosApi: string[]
}>()

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'save', payload: ProdutoPayload): void
}>()

const form = reactive<ProdutoPayload>({
  nome: '',
  descricao: '',
  preco: 10,
  imagemUrl: '',
  estoque: 10,
  categoriaId: 0
})

watch(
  () => [props.visible, props.produto, props.categorias] as const,
  ([visible, produto, categorias]) => {
    if (!visible) return
    if (produto) {
      form.nome = produto.nome
      form.descricao = produto.descricao
      form.preco = produto.preco
      form.imagemUrl = produto.imagemUrl
      form.estoque = produto.estoque
      form.categoriaId = produto.categoriaId
    } else {
      form.nome = ''
      form.descricao = ''
      form.preco = 15
      form.imagemUrl = 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=400&q=80'
      form.estoque = 10
      form.categoriaId = categorias[0]?.categoriaId ?? 0
    }
  },
  { immediate: true }
)

function handleSubmit() {
  emit('save', {
    nome: form.nome,
    descricao: form.descricao,
    preco: Number(form.preco),
    imagemUrl: form.imagemUrl,
    estoque: Number(form.estoque),
    categoriaId: Number(form.categoriaId)
  })
}
</script>

<template>
  <div v-if="visible" class="modal-backdrop-custom" @click.self="$emit('close')">
    <div class="modal-dialog-custom" role="dialog" aria-modal="true">
      <div class="d-flex align-items-center justify-content-between px-4 py-3 border-bottom">
        <div>
          <h5 class="mb-0 fw-bold">
            {{ produto ? `Editar produto #${produto.produtoID}` : 'Cadastrar produto' }}
          </h5>
          <small class="text-secondary">
            Ao salvar, um webhook será enviado automaticamente para a APIVitrine.
          </small>
        </div>
        <button type="button" class="btn-close" aria-label="Fechar" @click="$emit('close')"></button>
      </div>

      <form class="p-4" @submit.prevent="handleSubmit">
        <div v-if="errosApi.length > 0" class="alert alert-danger py-2 px-3 mb-3">
          <div class="fw-semibold mb-1" style="font-size: 0.88rem;">
            Corrija os campos abaixo:
          </div>
          <ul class="mb-0 ps-3" style="font-size: 0.84rem;">
            <li v-for="(erro, idx) in errosApi" :key="idx">{{ erro }}</li>
          </ul>
        </div>

        <div class="mb-3">
          <label class="form-label fw-medium">Nome do produto</label>
          <input
            v-model="form.nome"
            type="text"
            class="form-control"
            placeholder="Ex.: Suco de Uva Integral"
            required
            minlength="5"
            maxlength="80"
          />
          <div class="form-text">
            Mínimo de 5 caracteres e a primeira letra deve ser maiúscula (regra validada na API).
          </div>
        </div>

        <div class="mb-3">
          <label class="form-label fw-medium">Descrição</label>
          <textarea
            v-model="form.descricao"
            class="form-control"
            rows="2"
            placeholder="Descreva ingredientes, volume ou diferenciais do produto"
            required
            maxlength="300"
          ></textarea>
        </div>

        <div class="row g-3 mb-3">
          <div class="col-md-4">
            <label class="form-label fw-medium">Preço (R$)</label>
            <input
              v-model.number="form.preco"
              type="number"
              step="0.01"
              min="1"
              max="10000"
              class="form-control tabular-nums"
              required
            />
          </div>
          <div class="col-md-4">
            <label class="form-label fw-medium">Estoque inicial</label>
            <input
              v-model.number="form.estoque"
              type="number"
              step="1"
              min="1"
              class="form-control tabular-nums"
              required
            />
          </div>
          <div class="col-md-4">
            <label class="form-label fw-medium">Categoria</label>
            <select v-model.number="form.categoriaId" class="form-select" required>
              <option :value="0" disabled>Selecione...</option>
              <option
                v-for="cat in categorias"
                :key="cat.categoriaId"
                :value="cat.categoriaId"
              >
                {{ cat.nome }}
              </option>
            </select>
          </div>
        </div>

        <div class="mb-4">
          <label class="form-label fw-medium">URL da imagem</label>
          <input
            v-model="form.imagemUrl"
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
            @click="$emit('close')"
          >
            Cancelar
          </button>
          <button type="submit" class="btn btn-primary" :disabled="salvando">
            <span v-if="salvando" class="spinner-border spinner-border-sm me-2"></span>
            {{ produto ? 'Salvar alterações' : 'Cadastrar produto' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>
