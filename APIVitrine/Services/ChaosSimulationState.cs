namespace APIVitrine.Services
{
    public class ChaosSimulationState
    {
        private volatile bool _simularFalha;

        public bool SimularFalha
        {
            get => _simularFalha;
            set => _simularFalha = value;
        }
    }
}
