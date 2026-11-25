using APIVitrine.Context;
using APIVitrine.DTOs;
using APIVitrine.Models;
using APIVitrine.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIVitrine.Controllers
{
    [Route("webhooks")]
    [ApiController]
    public class WebhooksController : ControllerBase
    {
        private readonly VitrineDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ChaosSimulationState _chaos;

        public WebhooksController(
            VitrineDbContext context,
            IConfiguration configuration,
            ChaosSimulationState chaos)
        {
            _context = context;
            _configuration = configuration;
            _chaos = chaos;
        }

        [HttpPost("produtos")]
        public async Task<ActionResult> ReceberProduto([FromBody] ProdutoWebhookPayloadDto payload)
        {
            if (!ValidarSecret())
            {
                return Unauthorized(new { message = "Header X-Webhook-Secret invalido ou ausente." });
            }

            if (_chaos.SimularFalha)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new { message = "Simulacao de indisponibilidade ativa na APIVitrine (503)." });
            }

            var existente = await _context.ProdutosVitrine
                .FirstOrDefaultAsync(p => p.ProdutoOrigemId == payload.ProdutoOrigemId);

            if (string.Equals(payload.Acao, "DELETE", StringComparison.OrdinalIgnoreCase))
            {
                if (existente is not null)
                {
                    _context.ProdutosVitrine.Remove(existente);
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    status = "REMOVIDO",
                    produtoOrigemId = payload.ProdutoOrigemId,
                    processadoEmUtc = DateTime.UtcNow
                });
            }

            if (existente is null)
            {
                existente = new ProdutoVitrine
                {
                    ProdutoOrigemId = payload.ProdutoOrigemId
                };
                _context.ProdutosVitrine.Add(existente);
            }

            AplicarDados(existente, payload);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                status = "SINCRONIZADO",
                produtoOrigemId = existente.ProdutoOrigemId,
                processadoEmUtc = existente.UltimaSincronizacaoUtc
            });
        }

        [HttpPost("reconciliar")]
        public async Task<ActionResult> ReconciliarCatalogo([FromBody] List<ProdutoWebhookPayloadDto> produtos)
        {
            if (!ValidarSecret())
            {
                return Unauthorized(new { message = "Header X-Webhook-Secret invalido ou ausente." });
            }

            if (_chaos.SimularFalha)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new { message = "Simulacao de indisponibilidade ativa na APIVitrine (503)." });
            }

            var existentes = await _context.ProdutosVitrine.ToDictionaryAsync(p => p.ProdutoOrigemId);
            var idsRecebidos = new HashSet<int>();

            foreach (var payload in produtos)
            {
                idsRecebidos.Add(payload.ProdutoOrigemId);

                if (!existentes.TryGetValue(payload.ProdutoOrigemId, out var entidade))
                {
                    entidade = new ProdutoVitrine
                    {
                        ProdutoOrigemId = payload.ProdutoOrigemId
                    };
                    _context.ProdutosVitrine.Add(entidade);
                }

                AplicarDados(entidade, payload);
            }

            var orfaos = existentes.Values
                .Where(p => !idsRecebidos.Contains(p.ProdutoOrigemId))
                .ToList();

            if (orfaos.Count > 0)
            {
                _context.ProdutosVitrine.RemoveRange(orfaos);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                status = "RECONCILIADO",
                sincronizados = produtos.Count,
                orfaosRemovidos = orfaos.Count,
                processadoEmUtc = DateTime.UtcNow
            });
        }

        private bool ValidarSecret()
        {
            var expected = _configuration["SyncOptions:WebhookSecret"] ?? "catalogo-sync-secret-2026";
            if (!Request.Headers.TryGetValue("X-Webhook-Secret", out var received))
            {
                return false;
            }

            return string.Equals(received.ToString(), expected, StringComparison.Ordinal);
        }

        private static void AplicarDados(ProdutoVitrine destino, ProdutoWebhookPayloadDto origem)
        {
            destino.Nome = origem.Nome;
            destino.Descricao = origem.Descricao;
            destino.Preco = origem.Preco;
            destino.ImagemUrl = origem.ImagemUrl;
            destino.Estoque = origem.Estoque;
            destino.CategoriaId = origem.CategoriaId;
            destino.CategoriaNome = origem.CategoriaNome;
            destino.DataCadastro = origem.DataCadastro;
            destino.DataAtualizacaoOrigem = origem.DataAtualizacao;
            destino.UltimaSincronizacaoUtc = DateTime.UtcNow;
        }
    }
}
