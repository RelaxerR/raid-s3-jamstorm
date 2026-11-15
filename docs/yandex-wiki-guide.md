# Яндекс Вики через `tools/wiki.py`

Инструкция для сокомандников по настройке и использованию скрипта в репозитории TOP.

## Что умеет скрипт

- `read` получает содержимое страницы и идентификатор последней версии.
- `comments` получает комментарии страницы.
- `comment` публикует новый комментарий или ответ в существующей ветке обсуждения.

Скрипт принимает полный URL Яндекс Вики или slug страницы. Для GDD Second Catch slug равен `raid3/gdd-vtorojj-ulov-second-catch`.

## Требования

- Python 3.8 или новее.
- Доступ к организации в Яндекс Вики.
- OAuth-токен с правом `wiki:read` для чтения. Для публикации нужен `wiki:write`.
- Включённый Public API организации.

В текущей организации используется заголовок `X-Cloud-Org-Id`. Для другой организации уточните в настройках API, нужен ли `X-Org-Id`.

## Настройка

1. В корне репозитория создайте локальный файл `.env`.
2. Скопируйте в него шаблон из `.env.example`.
3. Заполните значения:

```dotenv
WIKI_TOKEN=ваш-токен
WIKI_ORG_ID=идентификатор-организации
WIKI_ORG_HEADER=X-Cloud-Org-Id
```

Для OAuth оставьте `WIKI_AUTH_SCHEME` пустым или не добавляйте его. Для IAM-токена укажите `WIKI_AUTH_SCHEME=Bearer`.

Файл `.env` добавлен в `.gitignore`. Не отправляйте его в Git, PR или чат.

## Как получить WIKI_TOKEN

1. Откройте [Яндекс OAuth](https://oauth.yandex.ru/) и нажмите **Создать**.
2. Выберите **Для доступа к API или отладки** и нажмите **Перейти к созданию**.
3. Укажите название приложения и контактную почту.
4. В разрешениях добавьте `Чтение из Вики (wiki:read)`. Для публикации комментариев дополнительно добавьте `Запись в Вики (wiki:write)`.
5. Создайте приложение, откройте его карточку и скопируйте `ClientID`.
6. Войдите в Яндекс под аккаунтом, от имени которого скрипт будет работать.
7. Откройте в браузере ссылку, подставив ClientID:

```text
https://oauth.yandex.ru/authorize?response_type=token&client_id=<ClientID>
```

8. Скопируйте OAuth-токен из результата авторизации и сохраните его в локальном `.env` в поле `WIKI_TOKEN`.

Проверьте токен чтением страницы. Ответ `401 Unauthorized` означает, что токен не получен, истёк или указан неверно. Администратор организации также должен включить Public API Яндекс Вики. [Официальная инструкция Яндекс](https://yandex.ru/support/wiki/ru/api-ref/access)

## Чтение страницы

```bash
python3 tools/wiki.py read \
  'https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/'
```

Ответ выводится в JSON. Основной текст документа находится в поле `content`, идентификатор страницы — в поле `id`.

## Чтение комментариев

```bash
python3 tools/wiki.py comments \
  raid3/gdd-vtorojj-ulov-second-catch
```

Если API вернул cursor для следующей страницы, передайте его повторно:

```bash
python3 tools/wiki.py comments \
  raid3/gdd-vtorojj-ulov-second-catch \
  --cursor 'значение-cursor'
```

## Публикация комментария

```bash
python3 tools/wiki.py comment \
  raid3/gdd-vtorojj-ulov-second-catch \
  --body 'Текст комментария'
```

Для длинного текста используйте файл:

```bash
python3 tools/wiki.py comment \
  raid3/gdd-vtorojj-ulov-second-catch \
  --body-file comment.txt
```

Чтобы передать текст через стандартный ввод:

```bash
cat comment.txt | python3 tools/wiki.py comment \
  raid3/gdd-vtorojj-ulov-second-catch \
  --body-file -
```

Ответ в существующей ветке обсуждения создаётся с параметрами `--parent-id` и `--thread-id` из ответа API:

```bash
python3 tools/wiki.py comment \
  raid3/gdd-vtorojj-ulov-second-catch \
  --body 'Ответ на замечание' \
  --parent-id 123 \
  --thread-id 456
```

## Рабочий порядок для ревью

Сначала получите страницу командой `read`, затем загрузите комментарии командой `comments`. После анализа сформулируйте замечание и опубликуйте его командой `comment`. Перед повторной отправкой после сетевой ошибки проверьте комментарии ещё раз, чтобы не создать дубликат.

## Частые ошибки

`Set WIKI_TOKEN and WIKI_ORG_ID` означает, что `.env` отсутствует или не заполнен.

`401 Unauthorized` означает, что токен истёк, неверен или выбран неправильный способ авторизации.

`403 Forbidden` означает, что у пользователя нет нужных прав в организации или на странице.

Ошибка, связанная с `compdef`, относится к автодополнению Yandex Cloud CLI и не влияет на работу скрипта.

## Быстрая проверка

```bash
python3 tools/wiki.py --help
python3 tools/wiki.py read raid3/gdd-vtorojj-ulov-second-catch
python3 tools/wiki.py comments raid3/gdd-vtorojj-ulov-second-catch
```

Официальная документация API доступна в [справке Яндекс Вики](https://yandex.ru/support/wiki/ru/api-ref/about).
