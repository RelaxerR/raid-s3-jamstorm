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

В сцене дополнительно записаны `_maxHitPoints: 100` и `_hitPoints: 100` существующего `BoatController`. Ссылки игрока на руль и парус сохранены. Существующие окружение, движение, камера и логика станций не заменялись. Список сцен сборки не менялся. `SafeArea` — обычный RectTransform с отступами 5% через якоря; дополнительных компонентов и внешних зависимостей для него нет. Это фиксированная безопасная область HUD, а не адаптер вырезов экрана устройства.

## Ожидаемое поведение при ручном запуске

Начальный режим — «Ходьба», парус опущен, скорость 0, HP 100/100. Подсказка E появляется только у доступной станции. У руля показываются режим «Руль», A/D, W/S и выход; у паруса — режим «Парус», W/S и выход. Выход E/Esc возвращает подсказки ходьбы. Движение обновляет скорость и положение стрелки карты, поворот — курс и ориентацию стрелки. Будущие индикаторы остаются невидимыми.

При отключении лодки статус сообщает о недоступности, компас убирает курс, карта скрывает маркер и подсказки управления заменяются сообщением. При отключении только игрока данные лодки остаются доступны, управление недоступно.

Это описание ожидаемого результата по реализации, а не отчёт о пройденной проверке: по прямому указанию пользователя проверки, тесты, запуск Unity и сборка не выполнялись. Ручную оценку поведения и читаемости на 16:9 выполняет пользователь.

Отдельные ограничения: [локальный HP без урона](hud-boat-status.md), [схематичная карта](hud-minimap.md), [выключенные механики среза](hud-future-indicators.md).
