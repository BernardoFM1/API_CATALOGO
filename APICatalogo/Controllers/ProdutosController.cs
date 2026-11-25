using APICatalogo.Context;
using APICatalogo.DTOs;
using APICatalogo.Models;
using APICatalogo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APICatalogo.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ProdutosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebhookSyncService _syncService;

        public ProdutosController(AppDbContext context, IWebhookSyncService syncService)
        {
            _context = context;
            _syncService = syncService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResultDto<ProdutoResponseDto>>> Get(
            [FromQuery] string? search,
            [FromQuery] int? categoriaId,
            [FromQuery] decimal? precoMin,
            [FromQuery] decimal? precoMax,
            [FromQuery] string? sortBy = "nome",
            [FromQuery] string? sortDir = "asc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.Produtos
                .AsNoTracking()
                .Include(p => p.categoria)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p =>
                    (p.Nome != null && p.Nome.ToLower().Contains(term)) ||
                    (p.Descricao != null && p.Descricao.ToLower().Contains(term)));
            }

            if (categoriaId.HasValue && categoriaId.Value > 0)
            {
                query = query.Where(p => p.CategoriaId == categoriaId.Value);
            }

            if (precoMin.HasValue)
            {
                query = query.Where(p => p.Preco >= precoMin.Value);
            }

            if (precoMax.HasValue)
            {
                query = query.Where(p => p.Preco <= precoMax.Value);
            }

            var isDesc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

            query = (sortBy?.ToLowerInvariant()) switch
            {
                "preco" => isDesc
                    ? query.OrderByDescending(p => (double)p.Preco)
                    : query.OrderBy(p => (double)p.Preco),
                "estoque" => isDesc
                    ? query.OrderByDescending(p => p.Estoque)
                    : query.OrderBy(p => p.Estoque),
                "datacadastro" => isDesc
                    ? query.OrderByDescending(p => p.DataCadastro)
                    : query.OrderBy(p => p.DataCadastro),
                _ => isDesc
                    ? query.OrderByDescending(p => p.Nome)
                    : query.OrderBy(p => p.Nome)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => MapToDto(p))
                .ToListAsync();

            return Ok(new PagedResultDto<ProdutoResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        [HttpGet("{id:int}", Name = "ObterProduto")]
        public async Task<ActionResult<ProdutoResponseDto>> Get(int id)
        {
            var produto = await _context.Produtos
                .AsNoTracking()
                .Include(p => p.categoria)
                .FirstOrDefaultAsync(p => p.ProdutoID == id);

            if (produto is null)
            {
                return NotFound(new { message = $"Produto #{id} nao encontrado." });
            }

            return Ok(MapToDto(produto));
        }

        [HttpPost]
        public async Task<ActionResult<ProdutoMutationResponseDto>> Post([FromBody] ProdutoCreateUpdateDto dto)
        {
            var categoria = await _context.Categorias
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoriaId == dto.CategoriaId);

            if (categoria is null)
            {
                return BadRequest(new { message = $"Categoria #{dto.CategoriaId} nao encontrada." });
            }

            var agora = DateTime.UtcNow;
            var produto = new Produto
            {
                Nome = dto.Nome.Trim(),
                Descricao = dto.Descricao.Trim(),
                Preco = dto.Preco,
                ImagemUrl = dto.ImagemUrl.Trim(),
                Estoque = dto.Estoque,
                CategoriaId = dto.CategoriaId,
                DataCadastro = agora,
                DataAtualizacao = agora
            };

            _context.Produtos.Add(produto);
            await _context.SaveChangesAsync();

            produto.categoria = categoria;

            var syncResult = await _syncService.SincronizarProdutoAsync(
                produto,
                categoria.Nome ?? string.Empty,
                "CREATED",
                "UPSERT");

            var response = new ProdutoMutationResponseDto
            {
                Produto = MapToDto(produto),
                Sync = syncResult
            };

            return CreatedAtRoute("ObterProduto", new { id = produto.ProdutoID }, response);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ProdutoMutationResponseDto>> Put(int id, [FromBody] ProdutoCreateUpdateDto dto)
        {
            var produto = await _context.Produtos.FirstOrDefaultAsync(p => p.ProdutoID == id);
            if (produto is null)
            {
                return NotFound(new { message = $"Produto #{id} nao encontrado." });
            }

            var categoria = await _context.Categorias
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoriaId == dto.CategoriaId);

            if (categoria is null)
            {
                return BadRequest(new { message = $"Categoria #{dto.CategoriaId} nao encontrada." });
            }

            produto.Nome = dto.Nome.Trim();
            produto.Descricao = dto.Descricao.Trim();
            produto.Preco = dto.Preco;
            produto.ImagemUrl = dto.ImagemUrl.Trim();
            produto.Estoque = dto.Estoque;
            produto.CategoriaId = dto.CategoriaId;
            produto.DataAtualizacao = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            produto.categoria = categoria;

            var syncResult = await _syncService.SincronizarProdutoAsync(
                produto,
                categoria.Nome ?? string.Empty,
                "UPDATED",
                "UPSERT");

            return Ok(new ProdutoMutationResponseDto
            {
                Produto = MapToDto(produto),
                Sync = syncResult
            });
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ProdutoMutationResponseDto>> Delete(int id)
        {
            var produto = await _context.Produtos
                .Include(p => p.categoria)
                .FirstOrDefaultAsync(p => p.ProdutoID == id);

            if (produto is null)
            {
                return NotFound(new { message = $"Produto #{id} nao localizado." });
            }

            var produtoDto = MapToDto(produto);
            var categoriaNome = produto.categoria?.Nome ?? string.Empty;

            _context.Produtos.Remove(produto);
            await _context.SaveChangesAsync();

            var syncResult = await _syncService.SincronizarProdutoAsync(
                produto,
                categoriaNome,
                "DELETED",
                "DELETE");

            return Ok(new ProdutoMutationResponseDto
            {
                Produto = produtoDto,
                Sync = syncResult
            });
        }

        private static ProdutoResponseDto MapToDto(Produto p) => new()
        {
            ProdutoID = p.ProdutoID,
            Nome = p.Nome ?? string.Empty,
            Descricao = p.Descricao ?? string.Empty,
            Preco = p.Preco,
            ImagemUrl = p.ImagemUrl ?? string.Empty,
            Estoque = p.Estoque,
            DataCadastro = p.DataCadastro,
            DataAtualizacao = p.DataAtualizacao,
            CategoriaId = p.CategoriaId,
            CategoriaNome = p.categoria?.Nome ?? string.Empty
        };
    }
}
