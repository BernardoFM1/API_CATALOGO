using System.ComponentModel.DataAnnotations;

namespace APICatalogo.DTOs
{
    public class CategoriaResponseDto
    {
        public int CategoriaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string ImageURL { get; set; } = string.Empty;
        public int TotalProdutos { get; set; }
    }

    public class CategoriaCreateUpdateDto
    {
        [Required(ErrorMessage = "O nome da categoria e obrigatorio.")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "O nome da categoria deve ter entre 2 e 80 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "A URL da imagem e obrigatoria.")]
        [StringLength(300, ErrorMessage = "A URL da imagem deve ter no maximo 300 caracteres.")]
        public string ImageURL { get; set; } = string.Empty;
    }
}
