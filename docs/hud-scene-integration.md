# Подключение HUD к graybox-сцене

[Структура HUD](gameplay-hud.md) · [Управление graybox](graybox-controls.md) · [Оглавление](index.md)

## Ассеты и ссылки

Сцена: `Assets/_Project/Scenes/Graybox/SecondCatchGraybox.unity`.
Префаб: `Assets/_Project/Prefabs/UI/GameplayHud.prefab`.

В `Presentation` записан экземпляр префаба. Сценовые overrides назначают корневому `GameplayHud`:

- `_boat` → `Boat/BoatController`;
- `_player` → `Boat/Player/PlayerController`;
- `_mapCenter` → `Environment/Island`.

Префаб не хранит внешних ссылок на сцену. Он содержит Canvas, CanvasScaler, все представления и внутренние ссылки на UI. Его можно перенести в другую сцену и назначить эти три источника в Inspector или вызвать `Bind`. Префаб уже сохранён как самостоятельный ассет, все вложенные объекты готовы к дальнейшему редактированию через Prefab Mode.

```text
Presentation
  GameplayHud                       Canvas, CanvasScaler, GameplayHud
    SafeArea                        внутренние 90% экрана
      BoatStatusPanel                 BoatStatusPanel, Image
        StatusText                    Text
        HitPointsTrack                Image
          HitPointsFill               Image
      ControlModeView                 ControlModeView, Image
        ModeText                      Text
      InteractionHintView             InteractionHintView, Image
        HintText                      Text
      CompassView                     CompassView, Image
        HeadingText                   Text
        FutureDirectionMarkers        выключен
      MinimapView                     MinimapView, Image
        Caption                       Text
        SeaArea                       Image
          IslandPlaceholder           Image, подпись центра
          NorthLabel                  Text
          FutureMapMarkers
            FishMarkers               выключен
            RockMarkers               выключен
            PortMarkers               выключен
            SpiritMarkers             выключен
          BoatMarker                  Text, стрелка
        PlaceholderLabel              Text
      FutureGameplayIndicators        FutureGameplayIndicators
        CargoPlaceholder              выключен, Image + дочерний Label
        TensionPlaceholder            выключен, Image + дочерний Label
        ReelingPlaceholder            выключен, Image + дочерний Label
        LowHitPointsWarning           выключен, Image + дочерний Label
        FishReceivedNotification      выключен, Image + дочерний Label
        SpiritAppearedNotification    выключен, Image + дочерний Label
```

В сцене `BoatController` явно получает `Wind`, `HelmStation` и `SailStation`. `WindController._boat` указывает на `Boat`, `_islandCenter` на `Environment/Island`, `_seaSurface` на BoxCollider `Sea/SeaSurface`. Руль ссылается на `RudderPivot`, под которым размещена балка `RudderBeam`, вытянутая вдоль носа. В сохранённой версии сцены отдельной балки не было, она добавлена вместе с точкой поворота. Парус хранит три положения и угол в собственной станции, визуал остаётся в `SailCloth`.

Начальные HP равны 100/100, парус сложен, угол 0°, скорость 0. Обычный ветер направлен по +Z, скорость 3, сила 1. Случайная смена происходит через 20…40 с с плавным переходом за 6 с. Радиус вероятности возврата к острову равен 50, границу моря задаёт его BoxCollider. Полная [модель ветра и движения](boat-wind-movement.md) содержит формулы и настройки.

Статусная панель префаба расширена до 560 × 310, текст до 528 × 262, панель режима перенесена на Y −346. Восемь строк содержат тип ветра и его цвет, направление, скорость/силу, три положения паруса, эффективность вперёд, знаковую скорость, источник тяги и угол руля. Подсказки описывают удержание W/S для передней/обратной гребли и ступени паруса. Список сцен сборки сохранён.

`SafeArea` использует отступы 5% через якоря. Это фиксированная безопасная область HUD.
## Ожидаемое поведение при ручном запуске

Начальный режим — «Ходьба», парус опущен, скорость 0, HP 100/100. Подсказка E появляется только у доступной станции. У руля показываются режим «Руль», A/D, W/S и выход; у паруса — режим «Парус», W/S, A/D и выход. Вход на станцию не включает греблю и не переключает парус. Выход E/Esc возвращает подсказки ходьбы, сразу прекращает греблю и коррекцию курса, запускает возврат балки в центр и сохраняет положение паруса и угол. Статус показывает тип/цвет/направление ветра, три положения паруса, угол, эффективность вперёд, направление гребли и угол руля. Панель статуса увеличена, режим управления расположен ниже неё. Движение обновляет скорость и положение стрелки карты, поворот — курс и ориентацию стрелки. Будущие индикаторы остаются невидимыми.

При отключении лодки статус сообщает о недоступности, компас убирает курс, карта скрывает маркер и подсказки управления заменяются сообщением. При отключении только игрока данные лодки остаются доступны, управление недоступно.

Это описание ожидаемого результата по реализации, а не отчёт о пройденной проверке: по прямому указанию пользователя проверки, тесты, запуск Unity и сборка не выполнялись. Ручную оценку поведения и читаемости на 16:9 выполняет пользователь.

Отдельные ограничения: [локальный HP и респавн](hud-boat-status.md), [схематичная карта](hud-minimap.md), [выключенные механики среза](hud-future-indicators.md).

## Подключение волн и HP

В существующую лодку добавлены `BoatHealth`, `BoatRespawnController`, `BoatBuoyancy` и кинематический Rigidbody. `BoatController._waves` и `BoatBuoyancy._waves` указывают на `SeaSurface/WaveController`, тот использует `Island` и `OceanSystem` того же объекта моря. В Inspector плавучести явно назначены четыре дочерние точки корпуса. В порту создан `BoatRespawnPoint`, контроллер возврата ссылается на него и существующего игрока.

`GameplayHud` продолжает использовать прежние ссылки лодки и игрока. `BoatStatusPanel` показывает повреждение рядом с HP, меняет цвет полосы и выводит состояние возврата в строке движения. Дополнительно виден полный горизонтальный ход с дрейфом. При респавне подсказки и режим управления получают состояние недоступности игрока. Количество строк и размеры префаба сохраняются. Подробная [механика волн, контактов и возврата](boat-waves-buoyancy.md) содержит значения Inspector.
