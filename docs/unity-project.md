---
aliases:
  - Unity-проект
---

# Unity-проект

[К оглавлению](index.md)

## Запуск из редактора

1. В Unity Hub установите **Unity 6000.5.1f1** и добавьте корень репозитория как существующий проект. Версия зафиксирована в `ProjectSettings/ProjectVersion.txt`.
2. Откройте проект и дождитесь импорта ассетов и загрузки пакетов. В `Packages/manifest.json` указаны, в частности, URP 17.5.0 и Input System 1.19.0.
3. В окне Project откройте `Assets/Scenes/SampleScene.unity` и нажмите **Play**. Для первичной проверки найдите в Hierarchy объекты `Ocean System` и компонент `LeviathanSpawner`. В Play Mode проверьте поверхность воды и появление экземпляров назначенного префаба левиафана.

## Сцены

В `ProjectSettings/EditorBuildSettings.asset` включена только `SampleScene`. В `Assets/Scenes` также лежат `Boot`, `MainMenu`, `HomePort` и `HomeShallows`. Текущую точку входа при проверке сборки определяйте по списку сцен в настройках сборки.

## Игровые системы

Пути в таблице указаны от корня репозитория. Игровые компоненты находятся в `Assets/Scripts/Runtime`, а компоненты океана, погоды и молний — в `Assets/Plugins`.

| Система | Где смотреть | Как устроена |
| --- | --- | --- |
| Океан | `Assets/Plugins/OceanPro/Runtime/OceanSystem.cs` | Генерирует поверхность воды. `GetWaterHeight` возвращает высоту волны в мировой позиции и через выходные параметры отдаёт нормаль и пену на гребне. `SetWakeSource` регистрирует источник следа для материала воды. |
| Плавучесть | `Assets/Plugins/OceanPro/Runtime/OceanBuoyancy.cs` | В `FixedUpdate` вычисляет погружение точек корпуса по высоте воды и прикладывает силы к `Rigidbody`. |
| Спасательный круг | `Assets/Scripts/Runtime/игрок/LifebuoyController.cs` | Управляет `Rigidbody`, рассчитывает плавучесть круга и обновляет его след. Клавиши W/S задают тягу, A/D — поворот. |
| Левиафаны | `Assets/Scripts/Runtime/левиа/LeviathanSpawner.cs` и `LeviathanSwimmer.cs` | Спавнер создаёт экземпляры префаба и передаёт им параметры движения и ссылку на океан. Пловец перемещается в заданной области и учитывает высоту поверхности при расчёте глубины. |
| Погода | `Assets/Plugins/WeatherKit/Runtime/WeatherController.cs` | `SetWeather` переключает пресеты освещения, тумана и осадков. |
| Молния | `Assets/Plugins/LightningBolt/Runtime/LightningBolt.cs` | `Strike` запускает эффект между двумя точками. У встроенного действия по умолчанию назначен пробел. |

Префабы спасательного круга и левиафана находятся в `Assets/Prefabs/Enemies`. При переносе или переименовании ассетов сохраняйте соответствующие файлы `.meta`, чтобы ссылки Unity по GUID продолжали работать.
