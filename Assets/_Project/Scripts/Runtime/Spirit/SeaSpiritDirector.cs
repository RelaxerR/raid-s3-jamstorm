using SecondCatch.Application;
using UnityEngine;
namespace SecondCatch.Spirit
{
    /// <summary>Сигнал появления опасности при полной бочке серого показа; навигация и атаки подключаются отдельно.</summary>
    public sealed class SeaSpiritDirector : MonoBehaviour
    {
        public bool PursuitRequested { get; private set; }
        private void Update()
        {
            var app = GameApplication.Instance;
            PursuitRequested = app != null && !app.IsLoading && app.Session != null && app.Session.AtSea && app.Session.Cargo.IsFull;
        }
    }
}
