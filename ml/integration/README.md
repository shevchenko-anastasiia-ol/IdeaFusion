# Підключення пошуку копій до IdeaFusion

## Що де лежить

| Частина | Файли | У git |
|---|---|---|
| ML-сервіс (Python, FastAPI) | `ml/service/` | так |
| Модель | `ml/models/v1/` (ваги SSCD завантажує `ml/models/download_weights.py`) | так, крім ваг SSCD |
| ContentService | `ContentAPI/CopyDetection/*`, `ContentAPI/Controllers/CopyCheckController.cs` | так |
| Шлюз | маршрут `/api/copycheck/**` в `ApiGateway/appsettings.json` | так |
| Фронтенд | `src/api/copycheck.ts`, `src/components/CopyCheckNotice.*`, `src/pages/CopyModerationPage.*` | так |
| Тести .NET | `Content.Tests/CopyDetection/CopyDetectionTests.cs` | так |
| **Гачки** у файлах, які на момент роботи мали ваші незакомічені зміни | `platform-hooks.patch` (див. нижче) | patch — так; самі правки — **ні** |

## Гачки (`platform-hooks.patch`)

Ці правки внесено в робочу копію, але **не закомічено**, щоб не змішати їх із вашими
незакоміченими змінами в тих самих файлах. Закомітьте їх разом зі своїми змінами або
застосуйте patch на чистій гілці (`git apply ml/integration/platform-hooks.patch`).

* `ContentAPI/Program.cs` — `builder.Services.AddCopyDetection(builder.Configuration);` після `AddBusinessLayer`.
* `AppHost/AppHost.cs` — образ Postgres `pgvector/pgvector:pg17` (Postgres 17 + розширення `vector`,
  наявний том даних сумісний), база `imagecopydb`, ресурс `mlservice` (Dockerfile з `ml/`, порт 5010,
  модель змонтована з `ml/models`), посилання ContentService → `mlservice` (без `WaitFor`: перевірка дорадча).
* `IdeaFusionFrontend/src/App.tsx` — маршрут `/moderation/copies`.
* `IdeaFusionFrontend/src/pages/NewPostPage.tsx` — `CopyCheckNotice` під кожним зображенням і
  підтвердження перед публікацією, якщо зображення в червоній зоні.
* `IdeaFusionFrontend/src/pages/ModerationLogPage.tsx` — кнопка переходу до черги можливих копій.

## Потік даних

```
Автор обирає зображення ─► POST /api/copycheck/preview ─► ContentService ─► mlservice /v1/check
                            (виключено власні роботи;         AuthorshipFilter: прибирає роботи
                             попередження до публікації)       команд, де автор — учасник (gRPC)

Пост опубліковано ─► CopyCheckingPostService (декоратор IPostService) ─► CopyCheckQueue
                     ─► CopyCheckWorker: /v1/check → фільтр авторства → якщо не зелена зона,
                        /v1/checks (черга модератора) → /v1/index (додати в індекс)
Пост видалено/архівовано ─► /v1/index/posts/{id} DELETE

Модератор ─► /moderation/copies ─► GET /api/copycheck/moderation, пояснення (PNG), рішення
```

Публікація ніколи не чекає на ML-сервіс і не блокується ним: черга працює у фоні,
а коли сервіс недоступний, попередження просто не показується.

## Налаштування ContentService

```json
"CopyDetection": { "Enabled": true, "BaseUrl": "http://mlservice", "TimeoutSeconds": 30, "QueueCapacity": 1000 }
```

## Запуск

1. `python ml/models/download_weights.py` — ваги SSCD (~100 МБ).
2. Запустити AppHost як завжди; перша збірка образу `mlservice` займає кілька хвилин (PyTorch CPU).
3. Наявні пости потрапляють в індекс після редагування медіа; для повного переіндексування
   достатньо пройти всі опубліковані пости через `CopyJob(IndexPost, postId)` (наприклад, з
   тимчасового ендпоінта або скрипта).
