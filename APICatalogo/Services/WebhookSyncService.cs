using APICatalogo.Context;
using APICatalogo.DTOs;
using APICatalogo.Models;
using System.Diagnostics;
using System.Net.Http.Json;

namespace APICatalogo.Services
{
    public class SyncOptions
    {
        public string TargetBaseUrl { get; set; } = "http://localhost:5071";
        public string WebhookSecret { get; set; } = "catalogo-sync-secret-2026";
        public int TimeoutSeconds { get; set; } = 5;
    }

    public interface IWebhookSyncService
    {
        Task<SyncOperationResultDto> SincronizarProdutoAsync(Produto produto, string categoriaNome, string acaoLog, string webhookAcao);
        Task<SyncOperationResultDto> ReconciliarCatalogoAsync(IEnumerable<ProdutoWebhookPayloadDto> produtos);
    }

    public class WebhookSyncService : IWebhookSyncService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppDbContext _context;
        private readonly SyncOptions _options;
        private readonly ILogger<WebhookSyncService> _logger;

        public WebhookSyncService(
            IHttpClientFactory httpClientFactory,
            AppDbContext context,
            IConfiguration configuration,
            ILogger<WebhookSyncService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
            _logger = logger;
            _options = configuration.GetSection("SyncOptions").Get<SyncOptions>() ?? new SyncOptions();
        }

        public async Task<SyncOperationResultDto> SincronizarProdutoAsync(
            Produto produto,
            string categoriaNome,
            string acaoLog,
            string webhookAcao)
        {
            var destinoUrl = $"{_options.TargetBaseUrl.TrimEnd('/')}/webhooks/produtos";
            var payload = new ProdutoWebhookPayloadDto
            {
                Acao = webhookAcao,
                ProdutoOrigemId = produto.ProdutoID,
                Nome = produto.Nome ?? string.Empty,
                Descricao = produto.Descricao ?? string.Empty,
                Preco = produto.Preco,
                ImagemUrl = produto.ImagemUrl ?? string.Empty,
                Estoque = produto.Estoque,
                CategoriaId = produto.CategoriaId,
                CategoriaNome = categoriaNome,
                DataCadastro = produto.DataCadastro,
                DataAtualizacao = produto.DataAtualizacao
            };

            return await EnviarWebhookAsync(
                destinoUrl,
                payload,
                produto.ProdutoID,
                produto.Nome ?? $"Produto #{produto.ProdutoID}",
                acaoLog);
        }

        public async Task<SyncOperationResultDto> ReconciliarCatalogoAsync(IEnumerable<ProdutoWebhookPayloadDto> produtos)
        {
            var lista = produtos.ToList();
            var destinoUrl = $"{_options.TargetBaseUrl.TrimEnd('/')}/webhooks/reconciliar";

            return await EnviarWebhookAsync(
                destinoUrl,
                lista,
                0,
                $"Catalogo completo ({lista.Count} itens)",
                "RECONCILE");
        }

        private async Task<SyncOperationResultDto> EnviarWebhookAsync(
            string destinoUrl,
            object payload,
            int produtoId,
            string produtoNome,
            string acaoLog)
        {
            var stopwatch = Stopwatch.StartNew();
            var sucesso = false;
            int? statusCode = null;
            string? mensagemErro = null;

            try
            {
                var client = _httpClientFactory.CreateClient("VitrineWebhook");
                client.Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds));

                using var request = new HttpRequestMessage(HttpMethod.Post, destinoUrl);
                request.Headers.Add("X-Webhook-Secret", _options.WebhookSecret);
                request.Content = JsonContent.Create(payload);

                using var response = await client.SendAsync(request);
                statusCode = (int)response.StatusCode;
                sucesso = response.IsSuccessStatusCode;

                if (!sucesso)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    mensagemErro = string.IsNullOrWhiteSpace(body)
                        ? $"APIVitrine retornou HTTP {statusCode}."
                        : body.Length > 450 ? body[..450] : body;
                }
            }
            catch (TaskCanceledException)
            {
                mensagemErro = $"Timeout ({_options.TimeoutSeconds}s) ao conectar na APIVitrine ({_options.TargetBaseUrl}).";
            }
            catch (HttpRequestException ex)
            {
                mensagemErro = $"Nao foi possivel alcancar a APIVitrine em {_options.TargetBaseUrl}: {ex.Message}";
            }
            catch (Exception ex)
            {
                mensagemErro = $"Erro inesperado ao enviar webhook: {ex.Message}";
            }
            finally
            {
                stopwatch.Stop();
            }

            if (!sucesso)
            {
                _logger.LogWarning("Falha na sincronizacao ({Acao}) para {Produto}: {Erro}", acaoLog, produtoNome, mensagemErro);
            }

            var agora = DateTime.UtcNow;
            var log = new SyncLog
            {
                ProdutoId = produtoId,
                ProdutoNome = produtoNome,
                Acao = acaoLog,
                Sucesso = sucesso,
                HttpStatusCode = statusCode,
                LatenciaMs = stopwatch.ElapsedMilliseconds,
                MensagemErro = mensagemErro,
                DestinoUrl = destinoUrl,
                DataHoraUtc = agora
            };

            _context.SyncLogs.Add(log);
            await _context.SaveChangesAsync();

            return new SyncOperationResultDto
            {
                Sucesso = sucesso,
                Acao = acaoLog,
                HttpStatusCode = statusCode,
                LatenciaMs = stopwatch.ElapsedMilliseconds,
                MensagemErro = mensagemErro,
                DataHoraUtc = agora
            };
        }
    }
}
