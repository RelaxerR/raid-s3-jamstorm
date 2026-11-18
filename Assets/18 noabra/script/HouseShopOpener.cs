using UnityEngine;

/// <summary>
/// Открывает лавку по клику на дом.
/// Вешается на объект "дом" в твоей сцене.
/// Работает даже если у дома Is Trigger = true (рейкаст с QueryTriggerInteraction.Collide).
/// </summary>
public class HouseShopOpener : MonoBehaviour
{
    [Tooltip("Сюда перетащи объект с PortShopUI. Если пусто - найдёт сам")]
    [SerializeField] private PortShopUI shopUI;

    private void Awake()
    {
        if (shopUI == null)
            shopUI = FindObjectOfType<PortShopUI>();
    }

    private void Update()
    {
        if (shopUI == null) return;
        if (!Input.GetMouseButtonDown(0)) return;

        // Не открываем, если кликнули по UI (кнопки лавки)
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, -1, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.gameObject == gameObject)
            {
                shopUI.Open();
            }
        }
    }
}
