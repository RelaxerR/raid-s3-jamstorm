# Приложение и сцены

[Реестр](../components.md) · [Pipeline](../loading-pipeline.md) · [Сессия](session.md)

`GameApplication` — единственный постоянный MonoBehaviour, сериализован в Boot. Владеет SoloGameSession, блокировкой загрузки и сообщением ошибки. GameScenes — каталог полных путей, а не поиск сцены по индексу. Изменение имени/пути требует обновить каталог, Build Settings и тест порядка сцен.

`MainMenuController` передаёт NewGame/ContinueGame. `HomePortController` передаёт Depart/Sell/SellFish/BuyUpgrade. `HomeShallowsController` привязывает BoatHealth и тягу лодки к сессии, обрабатывает ReturnToPort и возвращает погибшую лодку. `NetStation` передаёт улов косяка в сеть, а `FishOnDeck` зачисляет выбранную рыбу в бочку через сессию. Контроллеры не хранят копий баланса или груза.

`SceneFlowPanel` отображает главное меню и подсказку причала. Торговлю и выход из порта обслуживает `PortTradePanel`. Урон и причал приходят от коллайдеров, а состояние лодки и груз показывает GameplayHud. Прямой запуск сцены предлагает Boot.

Сценовые ссылки не сохраняются на GameApplication: уничтожение Single-сцены не оставляет ссылок на старые лодки, камеры и триггеры. Continue не является загрузкой с диска.
