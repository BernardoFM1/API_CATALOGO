using Microsoft.VisualBasic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace APICatalogo.Models
{
    [Table("Categorias")]
    public class Categoria 
    {
    public Categoria()
    {
        produtos = new Collection<Produto>();
    }
        [Key]
    public int CategoriaId { get; set; }
        [Required]
        [StringLength(80)]
    public string? Nome { get; set; }

        [Required]
        [StringLength(80)]
    public string? ImageURL { get; set; }
    public ICollection<Produto> produtos { get; set; }
    }
}
