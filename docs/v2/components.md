# Реестр компонентов и точек интеграции

[Оглавление](../index.md) · [Архитектура](architecture.md) · [Проверки](validation.md)

Все пути ниже относительно `Assets/_Project/Scripts/Runtime/`. Обозначение «заготовка» означает контракт для интегратора, без подключения к игровому миру и без заявления о завершённой механике.

| Файл / тип | Ответственность и статус | Документ |
| --- | --- | --- |
| `Application/GameScenes.cs` | Полные пути четырёх сцен, используется приложением и сборкой | [Приложение](entities/application.md) |
| `Application/GameApplication.cs` | Постоянный корень Boot, сессия, переходы, блокировка, ошибки; подключён | [Приложение](entities/application.md), [Pipeline](loading-pipeline.md) |
| `Application/MainMenuController.cs` | Новая игра и продолжение в памяти; подключён в MainMenu | [Приложение](entities/application.md) |
| `Port/HomePortController.cs` | Портовые команды; подключён в HomePort | [Порт](entities/port.md) |
| `Voyage/HomeShallowsController.cs` | Улов, урон, возврат и обработка гибели; подключён в HomeShallows | [Сессия](entities/session.md) |
| `UI/SceneFlowPanel.cs` | Технический пульт переходов, debug-сигналы; подключён в трёх сценах | [Приложение](entities/application.md) |
| `Game/SoloGameSession.cs` | Состояние одиночной игры и проверяемые команды | [Сессия](entities/session.md) |
| `Game/BoatState.cs` | HP, респавн, усиление скорости | [Лодка](entities/boat.md) |
| `Game/FishCargo.cs` | Одна бочка одного вида с лимитом | [Груз](entities/cargo.md) |
| `Game/StationAssignment.cs` | Заготовка локального назначения исполнителей, StationRole для v2 | [Станции и экипаж](entities/stations-crew.md) |
| `Boat/BoatController.cs` | Заготовка Rigidbody-мотора, внешний SetInput, ветер/волны/апгрейд | [Лодка](entities/boat.md) |
| `Fishing/FishSchool.cs` | Заготовка одноразового источника улова через TryCollect | [Груз](entities/cargo.md) |
| `World/SeaEnvironment.cs` | Заготовка поля направленных сил моря и ветра | [Мир](entities/world.md) |
| `Spirit/SeaSpiritDirector.cs` | Заготовка сигнала опасности от полной бочки, без AI | [Дух](entities/spirit.md) |

`Scripts/Editor/ProjectBuild.cs` — команды `ValidateScenes` и `BuildWindows`, проверяют порядок четырёх сцен и собирают development Windows x64. Сборка Editor остаётся editor-only; Runtime не использует UnityEditor.

`Scripts/Tests/SoloGameSessionTests.cs`, `StationAssignmentTests.cs`, `ScenePipelineTests.cs` — поведение модели, сохранение назначения при конфликте, порядок сцен/ссылки и реальный проход Boot → меню → порт → море → возврат, Continue, гибель, новая игра. Последний тест запускает Play Mode из EditMode через EnterPlayMode.

`Foundation/GameSession.cs`, `SessionState.cs`, `LocalSessionHost.cs`, `Player/LifebuoyController.cs`, `Leviathan/*.cs` перенесены с сохранением GUID и поведения. Старый API описан в [архиве](../project-foundation.md). Новые сцены не ссылаются на LocalSessionHost. Плагины и их публичные API перечислены в [запуске проекта](../unity-project.md).

Для следующей интеграции: назначить SeaEnvironment и OceanBuoyancy на ручном уровне, создать лодку с единственным мотором, передавать Input System-ввод в SetInput, проверенные контакты в TryCollect/Damage/ReturnToPort, затем подключить духа к сигналу директора. Модель состояния продолжает принадлежать приложению. Не создавать независимые копии кошелька/груза на префабах.
