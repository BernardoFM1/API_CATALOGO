namespace APIVitrine.DTOs
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

    public class SimularFalhaDto
    {
        public bool SimularFalha { get; set; }
    }
}
