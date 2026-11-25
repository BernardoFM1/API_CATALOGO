namespace APICatalogo.Services
{
    public class MeuServico : IMeuServico
    {

        public string saudacao(string nome)
        {
            return string.Format("Bem vindo, %s - %d ", nome, DateTime.UtcNow);
        }
    }
}
