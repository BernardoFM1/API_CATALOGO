using APICatalogo.Context;
using APICatalogo.DTOs;
using APICatalogo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APICatalogo.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CategoriasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public CategoriasController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Endpoint mantido como referencia de estudo de leitura de IConfiguration
        [HttpGet("LerArquivoConfiguracao")]
        public string GetValores()
        {
            var chave1 = _configuration["chave1"];
            var chave2 = _configuration["chave2"];
            var secao = _configuration["secao1:chave2"];

            return string.Format("Primeira chave: {0}, Segunda chave: {1} e secao: {2}", chave1, chave2, secao);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoriaResponseDto>>> Get()
        {
            var categorias = await _context.Categorias
                .AsNoTracking()
                .OrderBy(c => c.Nome)
                .Select(c => new CategoriaResponseDto
                {
                    CategoriaId = c.CategoriaId,
                    Nome = c.Nome ?? string.Empty,
                    ImageURL = c.ImageURL ?? string.Empty,
                    TotalProdutos = c.produtos.Count
                })
                .ToListAsync();

            return Ok(categorias);
        }

        [HttpGet("{id:int}", Name = "ObterCategoria")]
        public async Task<ActionResult<CategoriaResponseDto>> Get(int id)
        {
            var categoria = await _context.Categorias
                .AsNoTracking()
                .Where(c => c.CategoriaId == id)
                .Select(c => new CategoriaResponseDto
                {
                    CategoriaId = c.CategoriaId,
                    Nome = c.Nome ?? string.Empty,
                    ImageURL = c.ImageURL ?? string.Empty,
                    TotalProdutos = c.produtos.Count
                })
                .FirstOrDefaultAsync();

            if (categoria is null)
            {
                return NotFound(new { message = $"Categoria #{id} nao encontrada." });
            }

            return Ok(categoria);
        }

        [HttpPost]
        public async Task<ActionResult<CategoriaResponseDto>> Post([FromBody] CategoriaCreateUpdateDto dto)
        {
            var categoria = new Categoria
            {
                Nome = dto.Nome.Trim(),
                ImageURL = dto.ImageURL.Trim()
            };

            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();

            var response = new CategoriaResponseDto
            {
                CategoriaId = categoria.CategoriaId,
                Nome = categoria.Nome,
                ImageURL = categoria.ImageURL,
                TotalProdutos = 0
            };

            return CreatedAtRoute("ObterCategoria", new { id = categoria.CategoriaId }, response);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<CategoriaResponseDto>> Put(int id, [FromBody] CategoriaCreateUpdateDto dto)
        {
            var categoria = await _context.Categorias
                .Include(c => c.produtos)
                .FirstOrDefaultAsync(c => c.CategoriaId == id);

            if (categoria is null)
            {
                return NotFound(new { message = $"Categoria #{id} nao encontrada." });
            }

            categoria.Nome = dto.Nome.Trim();
            categoria.ImageURL = dto.ImageURL.Trim();

            await _context.SaveChangesAsync();

            return Ok(new CategoriaResponseDto
            {
                CategoriaId = categoria.CategoriaId,
                Nome = categoria.Nome,
                ImageURL = categoria.ImageURL,
                TotalProdutos = categoria.produtos.Count
            });
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var categoria = await _context.Categorias
                .Include(c => c.produtos)
                .FirstOrDefaultAsync(c => c.CategoriaId == id);

            if (categoria is null)
            {
                return NotFound(new { message = $"Categoria #{id} nao encontrada." });
            }

            if (categoria.produtos.Any())
            {
                return Conflict(new
                {
                    message = $"Nao e possivel excluir a categoria '{categoria.Nome}' pois existem {categoria.produtos.Count} produto(s) vinculado(s)."
                });
            }

            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Categoria '{categoria.Nome}' removida com sucesso." });
        }
    }
}
