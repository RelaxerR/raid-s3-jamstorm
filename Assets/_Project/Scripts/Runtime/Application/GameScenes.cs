namespace SecondCatch.Application
{
    /// <summary>Единый каталог сцен текущего ручного мира.</summary>
    public static class GameScenes
    {
        public const string BOOT = "Boot";
        public const string MENU = "MainMenu";
        public const string PORT = "HomePort";
        public const string SEA = "HomeShallows";
        public static string Path(string name) => "Assets/_Project/Scenes/" + name + ".unity";
    }
}
