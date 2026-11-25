using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APICatalogo.Models
{
    [Table("SyncLogs")]
    public class SyncLog
    {
        [Key]
        public int SyncLogId { get; set; }

        public int ProdutoId { get; set; }

        [Required]
        [StringLength(120)]
        public string ProdutoNome { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Acao { get; set; } = string.Empty;

        public bool Sucesso { get; set; }

        public int? HttpStatusCode { get; set; }

        public long LatenciaMs { get; set; }

        [StringLength(500)]
        public string? MensagemErro { get; set; }

        [Required]
        [StringLength(250)]
        public string DestinoUrl { get; set; } = string.Empty;

        public DateTime DataHoraUtc { get; set; } = DateTime.UtcNow;
    }
}
