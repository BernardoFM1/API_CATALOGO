using APICatalogo.Context;
using APICatalogo.DTOs;
using APICatalogo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APICatalogo.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebhookSyncService _syncService;

        public SyncController(AppDbContext context, IWebhookSyncService syncService)
        {
            _context = context;
            _syncService = syncService;
        }

        [HttpGet("logs")]
        public async Task<ActionResult<SyncLogsSummaryDto>> GetLogs([FromQuery] int limit = 50)
        {
            limit = Math.Clamp(limit, 1, 200);

            var totalTentativas = await _context.SyncLogs.CountAsync();
            var totalSucessos = await _context.SyncLogs.CountAsync(l => l.Sucesso);
            var totalFalhas = totalTentativas - totalSucessos;
            var latenciaMedia = totalTentativas > 0
                ? Math.Round(await _context.SyncLogs.AverageAsync(l => (double)l.LatenciaMs), 1)
                : 0;

            var logs = await _context.SyncLogs
                .AsNoTracking()
                .OrderByDescending(l => l.SyncLogId)
                .Take(limit)
                .Select(l => new SyncLogResponseDto
                {
                    SyncLogId = l.SyncLogId,
                    ProdutoId = l.ProdutoId,
                    ProdutoNome = l.ProdutoNome,
                    Acao = l.Acao,
                    Sucesso = l.Sucesso,
                    HttpStatusCode = l.HttpStatusCode,
                    LatenciaMs = l.LatenciaMs,
                    MensagemErro = l.MensagemErro,
                    DestinoUrl = l.DestinoUrl,
                    DataHoraUtc = l.DataHoraUtc
                })
                .ToListAsync();

            return Ok(new SyncLogsSummaryDto
            {
                TotalTentativas = totalTentativas,
                TotalSucessos = totalSucessos,
                TotalFalhas = totalFalhas,
                LatenciaMediaMs = latenciaMedia,
                Logs = logs
            });
        }

        [HttpPost("produtos/{id:int}")]
        public async Task<ActionResult<SyncOperationResultDto>> RessincronizarProduto(int id)
        {
            var produto = await _context.Produtos
                .AsNoTracking()
                .Include(p => p.categoria)
                .FirstOrDefaultAsync(p => p.ProdutoID == id);

            if (produto is null)
            {
                return NotFound(new { message = $"Produto #{id} nao encontrado no catalogo." });
            }

            var resultado = await _syncService.SincronizarProdutoAsync(
                produto,
                produto.categoria?.Nome ?? string.Empty,
                "RESYNC",
                "UPSERT");

            return Ok(resultado);
        }

        [HttpPost("reconciliar")]
        public async Task<ActionResult<SyncOperationResultDto>> ReconciliarCatalogo()
        {
            var produtos = await _context.Produtos
                .AsNoTracking()
                .Include(p => p.categoria)
                .OrderBy(p => p.ProdutoID)
                .Select(p => new ProdutoWebhookPayloadDto
                {
                    Acao = "UPSERT",
                    ProdutoOrigemId = p.ProdutoID,
                    Nome = p.Nome ?? string.Empty,
                    Descricao = p.Descricao ?? string.Empty,
                    Preco = p.Preco,
                    ImagemUrl = p.ImagemUrl ?? string.Empty,
                    Estoque = p.Estoque,
                    CategoriaId = p.CategoriaId,
                    CategoriaNome = p.categoria != null ? p.categoria.Nome ?? string.Empty : string.Empty,
                    DataCadastro = p.DataCadastro,
                    DataAtualizacao = p.DataAtualizacao
                })
                .ToListAsync();

            var resultado = await _syncService.ReconciliarCatalogoAsync(produtos);
            return Ok(resultado);
        }

        [HttpDelete("logs")]
        public async Task<ActionResult> LimparLogs()
        {
            var logs = await _context.SyncLogs.ToListAsync();
            _context.SyncLogs.RemoveRange(logs);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Historico de sincronizacao limpo com sucesso." });
        }
    }
}
