namespace APICatalogo.Services
{
    public interface MeuServico : IMeuServico
    {

        public string saudacao(string nome)
        {
            return string.Format("Bem vindo, %s - %d ", nome, DateTime.UtcNow);
        }
    }
}
