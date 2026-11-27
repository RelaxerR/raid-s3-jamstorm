namespace SecondCatch.Solo
{
    public enum FishContractState { Available, Active, Closed }

    /// <summary>Единственное предложение портового мастера в пилоте S2.</summary>
    public sealed class FishContract
    {
        public const string FishId = FishCargo.AzureFish;
        public const int Target = 5;
        public const int Reward = 60;

        public FishContractState State { get; internal set; }
    }
}
