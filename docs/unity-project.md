# Запуск Unity-проекта

[Оглавление](index.md) · [Pipeline](v2/loading-pipeline.md) · [Компоненты](v2/components.md)

Unity **6000.5.1f1**, URP 17.5.0, Input System 1.19.0. Открыть корень через Hub, дождаться импорта.
Открыть `Assets/_Project/Scenes/Boot.unity`, нажать Play. Техническая панель позволяет пройти New game → Depart → debug collect fish → Return to port → Sell catch → Buy speed upgrade → Depart. Для апгрейда собрать не меньше 10 рыб. Это проверка каркаса, не готовый серый билд.

Build Settings содержит ровно Boot, MainMenu, HomePort, HomeShallows в таком порядке. SampleScene исключена из сборки, но сохранена для автономной проверки океана, круга и левиафана: `Assets/_Project/Scenes/SampleScene.unity`.
Прямой запуск трёх игровых сцен показывает кнопку Open Boot; скрытое создание сессии не производится.

| Система прототипа | Новый путь / API |
| --- | --- |
| Круг | `Assets/_Project/Scripts/Runtime/Player/LifebuoyController.cs` |
| Левиафан | `Assets/_Project/Scripts/Runtime/Leviathan/LeviathanSpawner.cs`, `LeviathanSwimmer.cs` |
| Старая сессия | `Assets/_Project/Scripts/Runtime/Foundation/LocalSessionHost.cs` |
| Океан | `Assets/Plugins/OceanPro/Runtime/OceanSystem.cs`, `GetWaterHeight`, `SetWakeSource` |
| Плавучесть | `Assets/Plugins/OceanPro/Runtime/OceanBuoyancy.cs` |
| Погода / молнии | `Assets/Plugins/WeatherKit`, `Assets/Plugins/LightningBolt` — сохранены, не подключены к v2 |

Модели, префабы, настройки и скрипты перенесены вместе с `.meta`. Не добавлять одновременно LifebuoyController и новый BoatController к одному Rigidbody: это два независимых владельца движения. Сохраняйте GUID при последующих переносах.
