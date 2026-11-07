using APICatalogo.Context;
using APICatalogo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APICatalogo.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CategoriasController : Controller
    {
        

        private readonly AppDbContext _context;

        public CategoriasController(AppDbContext context) { _context = context; }


        [HttpGet]

        public ActionResult<IEnumerable<Categoria>> Get()
        {
           try
            {
                var categoria = _context.Categorias.AsNoTracking().ToList();

                if (categoria.Any()) { return categoria; }

                return categoria;
               
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,"Ocorreu um problema na solicitação.");
            }
          
        }



        [HttpGet("{id:int}", Name ="ObterCategoria")]

        public ActionResult<Categoria> Get(int id)
        {
            var categoria = _context.Categorias.FirstOrDefault(c => c.CategoriaId == id);

            if (categoria == null) { return NotFound("Categoria não encontrada"); }

            return categoria;
        }


        [HttpPost]

        public ActionResult Post(Categoria categoria)
        {
            if(categoria is null) { return BadRequest("Categoria não encontrada"); }
            
            _context.Categorias.Add(categoria);
            _context.SaveChanges();

            return new CreatedAtRouteResult("ObterCategoria", new { id = categoria.CategoriaId }, categoria);
        }

        [HttpPut("{id:int}")]

        public ActionResult Put(int id, Categoria categoria)
        {
            if (id != categoria.CategoriaId) { return BadRequest("Categoria não encontrada."); }
            _context.Entry(categoria).State = EntityState.Modified;
            _context.SaveChanges();

            return Ok(categoria);


        }

        [HttpDelete("{id:int}")]

        public ActionResult Delete(int id)
        {
            var categoria = _context.Categorias.FirstOrDefault(c => c.CategoriaId == id);

            if (categoria is null) { return BadRequest("Categoria não encontrada."); }


            _context.Categorias.Remove(categoria);
            _context.SaveChanges();

            return Ok(200);
        }




        
    }
}
