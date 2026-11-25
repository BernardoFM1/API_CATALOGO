using APIVitrine.Context;
using APIVitrine.DTOs;
using APIVitrine.Models;
using APIVitrine.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIVitrine.Controllers
{
    [Route("vitrine")]
    [ApiController]
    public class VitrineController : ControllerBase
    {
        private readonly VitrineDbContext _context;
        private readonly ChaosSimulationState _chaos;
        private readonly IConfiguration _configuration;

        public VitrineController(
            VitrineDbContext context,
            ChaosSimulationState chaos,
            IConfiguration configuration)
        {
            _context = context;
            _chaos = chaos;
            _configuration = configuration;
        }

        [HttpGet("produtos")]
        public async Task<ActionResult<IEnumerable<ProdutoVitrine>>> GetProdutos([FromQuery] string? search)
        {
            var query = _context.ProdutosVitrine.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Nome.ToLower().Contains(term) ||
                    p.CategoriaNome.ToLower().Contains(term));
            }

            var itens = await query
                .OrderBy(p => p.Nome)
                .ToListAsync();

            return Ok(itens);
        }

        [HttpGet("status")]
        public async Task<ActionResult> GetStatus()
        {
            var totalProdutos = await _context.ProdutosVitrine.CountAsync();
            var ultimaSync = await _context.ProdutosVitrine
                .OrderByDescending(p => p.UltimaSincronizacaoUtc)
                .Select(p => (DateTime?)p.UltimaSincronizacaoUtc)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                servico = "APIVitrine (Replica / PDV)",
                status = _chaos.SimularFalha ? "Degraded (Simulando 503)" : "Healthy",
                simularFalha = _chaos.SimularFalha,
                databaseProvider = _configuration["DatabaseProvider"] ?? "Sqlite",
                totalProdutos,
                ultimaSincronizacaoUtc = ultimaSync,
                timestampUtc = DateTime.UtcNow
            });
        }

        [HttpPost("simular-falha")]
        public ActionResult AlternarSimulacaoFalha([FromBody] SimularFalhaDto dto)
        {
            _chaos.SimularFalha = dto.SimularFalha;
            return Ok(new
            {
                simularFalha = _chaos.SimularFalha,
                message = _chaos.SimularFalha
                    ? "Modo de falha ativado: webhooks retornarao HTTP 503."
                    : "Modo de falha desativado: webhooks operando normalmente."
            });
        }
    }
}
