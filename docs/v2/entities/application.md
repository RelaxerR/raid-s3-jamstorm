# Приложение и сцены

[Реестр](../components.md) · [Pipeline](../loading-pipeline.md) · [Сессия](session.md)

`GameApplication` — единственный постоянный MonoBehaviour, сериализован в Boot. Владеет SoloGameSession, блокировкой загрузки и сообщением ошибки. GameScenes — каталог полных путей, а не поиск сцены по индексу. Изменение имени/пути требует обновить каталог, Build Settings и тест порядка сцен.

`MainMenuController` передаёт NewGame/ContinueGame. `HomePortController` передаёт Depart/Sell/BuyUpgrade. `HomeShallowsController` принимает CollectFish/Damage/ReturnToPort и возвращает погибшую лодку. Контроллеры не хранят копий баланса или груза.

`SceneFlowPanel` — временный сценовый IMGUI-пульт интеграции: меню, портовые действия, debug улов/урон в море. Прямой запуск сцены предлагает Boot. Производственный HUD должен читать модель и вызывать те же контроллеры после IsLoading=false; Replace панели не требует менять модель.

Сценовые ссылки не сохраняются на GameApplication: уничтожение Single-сцены не оставляет ссылок на старые лодки, камеры и триггеры. Continue не является загрузкой с диска.
