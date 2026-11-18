# Источники, приоритет и открытые вопросы

[Оглавление](../index.md) · [Решения](architecture.md) · [Границы](scope.md)

Прочитано 18.11.2026 через `tools/wiki.py read` и тот же read-only API-модуль. Дерево получено GET `/v1/pages/descendants?slug=raid3`; все 24 доступные страницы прочитаны. У «Вертикального среза» комментариев нет. Записей и комментариев в Wiki не публиковалось.

Рабочее название «v2» взято из задачи. У прочитанных страниц нет единого номера версии v2: новая страница «Вертикальный срез» используется как основной источник, старые корневой GDD и ТЗ — только для непротиворечащих деталей. Страница 0. Общее положение всё ещё указывает GDD 0.1. Приоритет: явные указания пользователя → новая страница среза → непротиворечащие старые ТЗ → документированное временное решение реализации.

| Документ | Page ID | Last revision |
| --- | --- | --- |
| [GDD "Второй улов / Second Catch"](https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/) | 50615518 | 77413538 |
| [0. Общее положение](https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/596e9d5ac796/) | 50615525 | 77420222 |
| [5. Саунд](https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/5-saund/) | 50615529 | 77420049 |
| [4. Арт](https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/e02767d67a6b/) | 50615580 | 77420064 |
| [3. Механики](https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/cd21bcc9ca00/) | 50615582 | 77420082 |
| [2. Нарратив](https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/abce1ca058a3/) | 50615583 | 77420296 |
| [1. Геймдизайн](https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/6cf3c4f9b718/) | 50615584 | 77541645 |
| [6. Маркетинг](https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/8e65ca4afbbb/) | 50616209 | 77420046 |
| [7. Пост-релиз поддержка](https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/63ebcec16f77/) | 50616224 | 77420019 |
| [8. Идеи](https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/d29857bf9e52/) | 50616284 | 77420027 |
| [Инструкции](https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/998f9250f7c2/) | 50616285 | 77419554 |
| [ТЗ](https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/cd21bcc9ca00/tz/) | 50616296 | 77419463 |
| [1. Инструкция по GitHub Desktop](https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/998f9250f7c2/instrukcija-po-github-desktop/) | 50616304 | 77419761 |
| [1.1. Мореходство / Navigation](https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/cd21bcc9ca00/tz/1.1-morexodstvo-navigation/) | 50616310 | 77419631 |
| [1.7. Интерфейс и сохранения / UI & Save](https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/cd21bcc9ca00/tz/34a5de281cb3/) | 50616312 | 77419672 |
| [1.6. Мир и районы / World & Areas](https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/cd21bcc9ca00/tz/1.6-mir-i-rajjony-world-areas/) | 50616314 | 77419654 |
| [1.5. Экономика / Economy](https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/cd21bcc9ca00/tz/1.5-jekonomika-economy/) | 50616315 | 77419653 |
| [1.4. Станции и экипаж / Stations & Crew](https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/cd21bcc9ca00/tz/1.4-stancii-i-jekipazh-stations-crew/) | 50616316 | 77419650 |
| [1.3. Охота на морского духа / Sea Spirit Hunt](https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/cd21bcc9ca00/tz/6b09d619bbf8/) | 50616317 | 77419646 |
| [1.2. Рыбалка / Fishing](https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/cd21bcc9ca00/tz/1.2-rybalka-fishing/) | 50616318 | 77419643 |
| [1.0. Камера / Camera](https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/cd21bcc9ca00/tz/0237b3dec6cb/) | 50616322 | 77419681 |
| [Ссылки](https://wiki.yandex.ru/raid3/ssylki/) | 50617255 | 77428725 |
| [Вертикальный срез](https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/vertikalnyjj-srez/) | 50629658 | 77543151 |
| [Новая страница](https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/vertikalnyjj-srez/80a422aa80f3/) | 50630377 | 77547813 |

## Неопределённости, требующие решения ГД

1. §1.4 среза включает третий транспорт/вываживание духа, §2.4 и §9 называют духа угрозой, охоту — вне среза. Принято: только бегство, без босс-цикла; статус транспорта 3 уточнить.
2. Волны должны толкать к центру, но одна ранняя фраза говорит «волны от центра». Принято направление силы к порту по §2.1; визуальную фазовую скорость согласовать отдельно.
3. Точная дата серого показа не дана: «через 2 дня» без даты отсчёта. Дата 11 декабря задана пользователем, в прочитанном тексте Wiki не найдена.
4. Целевой цикл 5–8/15–20 минут в описании против 10–15 минут в критериях. До плейтеста не закреплять единый таймер.
5. Полный баланс видов/апгрейдов/найма/контрактов/древесины не задан. 10 за рыбу, 100 за одно усиление скорости x1.4 — временные параметры каркаса. Бочка одна для серого каркаса; план первого транспорта — две по 100.
6. При потере 50% нечётного груза и 30% дробных HP округление не задано. Принято потерю округлять вверх, HP — вверх; покрыто тестом.
7. Старые ТЗ требуют JSON-сохранения, процедурные районы, шторм и босса. Последние три исключены новым срезом; сроки сохранений уточнить, Continue сейчас только в памяти.
8. В старом ТЗ камера за кормой/сбоку, в новом — изометрия и станции. Новый срез приоритетен. Бюджет камеры, конкретные Input Actions, аппаратная цель 60 fps и окончательный перечень декабрьского контента требуют отдельных задач.

Эти вопросы не блокируют согласованный каркас. Решения здесь не объявляются утверждением ГД и не отправляются в Wiki.
