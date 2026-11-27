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
| `Voyage/VoyageAudio.cs` | Морской фон, скрипы корпуса, всплески, звуки руля, паруса и столкновений | [Звук рейса](#звук-рейса) |
| `Voyage/PortZone.cs` | Физический триггер возврата по входу корпуса после выхода со стоянки | [Порт](entities/port.md) |
| `UI/SceneFlowPanel.cs` | Главное меню, запуск Boot из автономной сцены и подсказка причала | [Приложение](entities/application.md) |
| `Game/SoloGameSession.cs` | Состояние одиночной игры и проверяемые команды | [Сессия](entities/session.md) |
| `Game/BoatState.cs` | HP, респавн, усиление скорости | [Лодка](entities/boat.md) |
| `Game/FishCargo.cs` | Одна бочка одного вида с лимитом | [Груз](entities/cargo.md) |
| `Game/StationAssignment.cs` | Заготовка локального назначения исполнителей, StationRole для v2 | [Станции и экипаж](entities/stations-crew.md) |
| `Boat/BoatController.cs` | Ход graybox, парус/гребля, инерция, руль, дрейф и сопротивление волн | [Движение](../boat-wind-movement.md) |
| `Boat/WaveController.cs` | Радиальное давление моря и настройки амплитуды OceanPro | [Волны](../boat-waves-buoyancy.md) |
| `Boat/BoatBuoyancy.cs` | Управляемая вертикальная поддержка и качка по четырём Transform | [Плавучесть](../boat-waves-buoyancy.md) |
| `Boat/BoatHealth.cs` | Адаптер HP и контактного урона для сессии, локальные HP автономной сцены | [HP](../boat-waves-buoyancy.md) |
| `Boat/BoatRespawnController.cs` | Возврат в порт, освобождение станций, сброс движения, восстановление игрока/HP | [Респавн](../boat-waves-buoyancy.md) |
| `Boat/BoatDamageSource.cs` | Reef/Rock/Debris, 15/25/5 HP, cooldown контактов | [Источники урона](../boat-waves-buoyancy.md) |
| `Fishing/FishSchool.cs`, `Stations/NetStation.cs` | Ручная сеть, захват косяка и выгрузка | [Груз](entities/cargo.md) |
| `Fishing/DeckCatch.cs`, `Fishing/FishOnDeck.cs` | Рыба на палубе, попадание ЛКМ и перенос в бочку | [Груз](entities/cargo.md) |
| `Fishing/CargoHold.cs`, `Fishing/CargoFish.cs` | Локальное отображение и укладка рыбы на лодке | [Груз](entities/cargo.md) |
| `UI/PortContractPanel.cs`, `Game/FishContract.cs` | Предложение портового мастера, прогресс и сдача улова | [Порт](entities/port.md#контракт-портового-мастера) |
| `UI/PortTradePanel.cs` | Продажа и улучшение через HomePortController | [Порт](entities/port.md) |
| `UI/GameplayHud.cs` | Статус лодки и счётчик улова в рейсе | [HUD](../gameplay-hud.md) |
| `World/SeaEnvironment.cs` | Заготовка поля направленных сил моря и ветра | [Мир](entities/world.md) |
| `Spirit/SeaSpiritDirector.cs` | Один дух на рейс, прямое преследование и интервальный урон | [Дух](entities/spirit.md) |

`Scripts/Editor/ProjectBuild.cs` — команды `ValidateScenes` и `BuildWindows`, проверяют порядок четырёх сцен и собирают development Windows x64. Сборка Editor остаётся editor-only; Runtime не использует UnityEditor.

`Scripts/Tests/SoloGameSessionTests.cs`, `StationAssignmentTests.cs`, `ScenePipelineTests.cs` — поведение модели, сохранение назначения при конфликте, порядок сцен/ссылки и реальный проход Boot → меню → порт → море → возврат, Continue, гибель, новая игра. Последний тест запускает Play Mode из EditMode через EnterPlayMode.

`Foundation/GameSession.cs`, `SessionState.cs`, `LocalSessionHost.cs`, `Player/LifebuoyController.cs`, `Leviathan/*.cs` перенесены с сохранением GUID и поведения. Старый API описан в [архиве](../project-foundation.md). Новые сцены не ссылаются на LocalSessionHost. Плагины и их публичные API перечислены в [запуске проекта](../unity-project.md).

`HomeShallows` и автономный `SecondCatchGraybox` используют `WaveController` и общий `OceanSystem` из `Plugins/OceanPro`, с четырьмя настраиваемыми Gerstner-волнами на CPU и в шейдере. `SeaEnvironment` остаётся прежним контрактом каркаса, в graybox не назначен. `OceanBuoyancy` сохраняется для остальных потребителей плагина. Управляемой лодкой graybox по вертикали владеет `BoatBuoyancy`. Существующий `GameplayHud/BoatStatusPanel` показывает HP, урон и состояние возврата.

В одиночном рейсе состоянием HP владеет `SoloGameSession.Boat`. В автономном graybox действует локальный BoatHealth с 100 HP и полным восстановлением. Точки подключения улова и правила возврата описаны в [pipeline](loading-pipeline.md).

## Звук рейса

В сцене [HomeShallows](../../Assets/_Project/Scenes/HomeShallows.unity) выберите объект `Boat` и откройте компонент `VoyageAudio`. Поле `Effects` указывает на `AudioSource` той же лодки. Его выход `Output` назначен группе `SFX` микшера [SecondCatch](../../Assets/_Project/Audio/SecondCatch.mixer). Через этот источник звучат всплески, руль, парус и удары о препятствия.

Поле `Ambience` компонента указывает на одноимённую группу того же микшера. В `Awake` компонент создаёт на лодке источники петель океана, прибоя, ветра и волн у корпуса, а для пространственных скрипов — дочерние объекты `Port Creak` и `Starboard Creak`. Все эти источники используют группу `Ambience`. Клипы для обеих групп назначены в инспекторе `VoyageAudio`.
