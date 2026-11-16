using UnityEngine;

namespace SecondCatch.Foundation
{
    /// <summary>
    /// Точка подключения локальной сессии. Добавляется в отдельную тестовую сцену.
    /// В сетевой сессии эту роль выполняет адаптер Fusion на стороне State Authority.
    /// </summary>
    public sealed class LocalSessionHost : MonoBehaviour
    {
        #region Настройки
        [SerializeField, Min(1)] private int _capacity = 20;
        [SerializeField, Min(1)] private int _fishPrice = 10;
        [SerializeField, Min(1)] private int _upgradePrice = 100;
        public GameSession Session { get; private set; }
        #endregion

        #region Жизненный цикл
        private void Awake()
        {
            Session = new GameSession(new FishDefinition("fish.demo", Mathf.Max(1, _fishPrice), 5),
                Mathf.Max(1, _capacity), Mathf.Max(1, _upgradePrice));
        }

        /// <summary>Локальный адаптер станции передаёт сюда проверенный ввод оператора.</summary>
        public void Simulate(float deltaTime, bool winchHeld)
        {
            Session?.Tick(deltaTime, winchHeld);
        }
        #endregion
    }
}
