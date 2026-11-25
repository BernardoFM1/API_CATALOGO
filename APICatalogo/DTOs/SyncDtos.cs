namespace APICatalogo.DTOs
{
    public class ProdutoWebhookPayloadDto
    {
        public string Acao { get; set; } = "UPSERT";
        public int ProdutoOrigemId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public string ImagemUrl { get; set; } = string.Empty;
        public float Estoque { get; set; }
        public int CategoriaId { get; set; }
        public string CategoriaNome { get; set; } = string.Empty;
        public DateTime DataCadastro { get; set; }
        public DateTime? DataAtualizacao { get; set; }
    }

    public class SyncOperationResultDto
    {
        public bool Sucesso { get; set; }
        public string Acao { get; set; } = string.Empty;
        public int? HttpStatusCode { get; set; }
        public long LatenciaMs { get; set; }
        public string? MensagemErro { get; set; }
        public DateTime DataHoraUtc { get; set; }
    }

    public class SyncLogResponseDto
    {
        public int SyncLogId { get; set; }
        public int ProdutoId { get; set; }
        public string ProdutoNome { get; set; } = string.Empty;
        public string Acao { get; set; } = string.Empty;
        public bool Sucesso { get; set; }
        public int? HttpStatusCode { get; set; }
        public long LatenciaMs { get; set; }
        public string? MensagemErro { get; set; }
        public string DestinoUrl { get; set; } = string.Empty;
        public DateTime DataHoraUtc { get; set; }
    }

    public class SyncLogsSummaryDto
    {
        public int TotalTentativas { get; set; }
        public int TotalSucessos { get; set; }
        public int TotalFalhas { get; set; }
        public double LatenciaMediaMs { get; set; }
        public IEnumerable<SyncLogResponseDto> Logs { get; set; } = Enumerable.Empty<SyncLogResponseDto>();
    }
}
