using System.ComponentModel.DataAnnotations;

namespace APICatalogo.DTOs
{
    public class ProdutoResponseDto
    {
        public int ProdutoID { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public string ImagemUrl { get; set; } = string.Empty;
        public float Estoque { get; set; }
        public DateTime DataCadastro { get; set; }
        public DateTime? DataAtualizacao { get; set; }
        public int CategoriaId { get; set; }
        public string CategoriaNome { get; set; } = string.Empty;
    }

    public class ProdutoCreateUpdateDto : IValidatableObject
    {
        [Required(ErrorMessage = "O nome e obrigatorio.")]
        [StringLength(80, MinimumLength = 5, ErrorMessage = "O nome deve ter entre 5 e 80 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "A descricao e obrigatoria.")]
        [StringLength(300, ErrorMessage = "A descricao deve ter no maximo 300 caracteres.")]
        public string Descricao { get; set; } = string.Empty;

        [Required]
        [Range(1, 10000, ErrorMessage = "O preco deve estar entre {1} e {2}.")]
        public decimal Preco { get; set; }

        [Required(ErrorMessage = "A URL da imagem e obrigatoria.")]
        [StringLength(300, ErrorMessage = "A URL da imagem deve ter no maximo 300 caracteres.")]
        public string ImagemUrl { get; set; } = string.Empty;

        public float Estoque { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Selecione uma categoria valida.")]
        public int CategoriaId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrWhiteSpace(Nome))
            {
                var primeiraLetra = Nome.Trim()[0].ToString();
                if (primeiraLetra != primeiraLetra.ToUpper())
                {
                    yield return new ValidationResult(
                        "A primeira letra do produto deve ser maiuscula.",
                        new[] { nameof(Nome) });
                }
            }

            if (Estoque <= 0)
            {
                yield return new ValidationResult(
                    "O estoque deve ser maior que zero.",
                    new[] { nameof(Estoque) });
            }
        }
    }

    public class PagedResultDto<T>
    {
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class ProdutoMutationResponseDto
    {
        public ProdutoResponseDto Produto { get; set; } = new();
        public SyncOperationResultDto Sync { get; set; } = new();
    }
}
