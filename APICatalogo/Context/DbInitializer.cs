using APICatalogo.Models;

namespace APICatalogo.Context
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            context.Database.EnsureCreated();

            if (context.Categorias.Any())
            {
                return;
            }

            var bebidas = new Categoria
            {
                Nome = "Bebidas",
                ImageURL = "https://images.unsplash.com/photo-1554866585-cd94860890b7?w=400&q=80"
            };
            var lanches = new Categoria
            {
                Nome = "Lanches",
                ImageURL = "https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=400&q=80"
            };
            var sobremesas = new Categoria
            {
                Nome = "Sobremesas",
                ImageURL = "https://images.unsplash.com/photo-1551024601-bec78aea704b?w=400&q=80"
            };
            var cafeteria = new Categoria
            {
                Nome = "Cafeteria",
                ImageURL = "https://images.unsplash.com/photo-1509042239860-f550ce710b93?w=400&q=80"
            };

            context.Categorias.AddRange(bebidas, lanches, sobremesas, cafeteria);
            context.SaveChanges();

            var agora = DateTime.UtcNow;

            var produtos = new List<Produto>
            {
                new()
                {
                    Nome = "Coca-Cola Diet",
                    Descricao = "Refrigerante de Cola 350 ml gelado",
                    Preco = 5.45m,
                    ImagemUrl = "https://images.unsplash.com/photo-1622483767028-3f66f32aef97?w=400&q=80",
                    Estoque = 50,
                    DataCadastro = agora.AddDays(-10),
                    DataAtualizacao = agora.AddDays(-10),
                    CategoriaId = bebidas.CategoriaId
                },
                new()
                {
                    Nome = "Suco Natural de Laranja",
                    Descricao = "Suco fresco espremido na hora 500 ml",
                    Preco = 9.90m,
                    ImagemUrl = "https://images.unsplash.com/photo-1600271886742-f049cd451bba?w=400&q=80",
                    Estoque = 18,
                    DataCadastro = agora.AddDays(-8),
                    DataAtualizacao = agora.AddDays(-8),
                    CategoriaId = bebidas.CategoriaId
                },
                new()
                {
                    Nome = "Lanche de Atum",
                    Descricao = "Pao ciabatta com pasta de atum, maionese temperada e alface",
                    Preco = 18.50m,
                    ImagemUrl = "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?w=400&q=80",
                    Estoque = 12,
                    DataCadastro = agora.AddDays(-7),
                    DataAtualizacao = agora.AddDays(-7),
                    CategoriaId = lanches.CategoriaId
                },
                new()
                {
                    Nome = "Hamburguer Artesanal",
                    Descricao = "Pao brioche, blend bovino 180g, cheddar cremoso e bacon",
                    Preco = 32.90m,
                    ImagemUrl = "https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=400&q=80",
                    Estoque = 24,
                    DataCadastro = agora.AddDays(-5),
                    DataAtualizacao = agora.AddDays(-5),
                    CategoriaId = lanches.CategoriaId
                },
                new()
                {
                    Nome = "Pudim 100g",
                    Descricao = "Pudim tradicional de leite condensado com calda de caramelo",
                    Preco = 8.75m,
                    ImagemUrl = "https://images.unsplash.com/photo-1528975604071-b4dc52a2d18c?w=400&q=80",
                    Estoque = 20,
                    DataCadastro = agora.AddDays(-4),
                    DataAtualizacao = agora.AddDays(-4),
                    CategoriaId = sobremesas.CategoriaId
                },
                new()
                {
                    Nome = "Brownie de Chocolate",
                    Descricao = "Brownie meio amargo com nozes e cobertura de ganache",
                    Preco = 14.00m,
                    ImagemUrl = "https://images.unsplash.com/photo-1606313564200-e75d5e30476c?w=400&q=80",
                    Estoque = 5,
                    DataCadastro = agora.AddDays(-2),
                    DataAtualizacao = agora.AddDays(-2),
                    CategoriaId = sobremesas.CategoriaId
                },
                new()
                {
                    Nome = "Cappuccino Cremoso",
                    Descricao = "Espresso duplo com leite vaporizado e toque de cacau",
                    Preco = 11.50m,
                    ImagemUrl = "https://images.unsplash.com/photo-1572442388796-11668a67e53d?w=400&q=80",
                    Estoque = 35,
                    DataCadastro = agora.AddDays(-1),
                    DataAtualizacao = agora.AddDays(-1),
                    CategoriaId = cafeteria.CategoriaId
                }
            };

            context.Produtos.AddRange(produtos);
            context.SaveChanges();
        }
    }
}
