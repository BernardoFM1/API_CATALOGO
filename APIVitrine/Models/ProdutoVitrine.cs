using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIVitrine.Models
{
    [Table("ProdutosVitrine")]
    public class ProdutoVitrine
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ProdutoOrigemId { get; set; }

        [Required]
        [StringLength(80)]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        public string Descricao { get; set; } = string.Empty;

        public decimal Preco { get; set; }

        [Required]
        [StringLength(300)]
        public string ImagemUrl { get; set; } = string.Empty;

        public float Estoque { get; set; }

        public int CategoriaId { get; set; }

        [StringLength(80)]
        public string CategoriaNome { get; set; } = string.Empty;

        public DateTime DataCadastro { get; set; }

        public DateTime? DataAtualizacaoOrigem { get; set; }

        public DateTime UltimaSincronizacaoUtc { get; set; } = DateTime.UtcNow;
    }
}
