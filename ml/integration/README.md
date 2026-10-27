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
* `AppHost/AppHost.cs`:
  * образ Postgres `pgvector/pgvector:pg17-trixie`. Це Postgres 17 з розширенням `vector` на тій самій
    Debian trixie (glibc 2.41), що й офіційний `postgres:17`, яким створено ваш том. Тег `pg17` без
    суфікса зібраний на bookworm (glibc 2.36): з ним Postgres попереджає про розбіжність collation, і
    текстові індекси можуть сортуватися інакше;
  * база `imagecopydb` (Aspire 9.5 лише оголошує її, створює ML-сервіс під час першого старту);
  * ресурс `mlservice`: Dockerfile з `ml/`, порт 5010, модель змонтована з `ml/models`, чекає на Postgres і MinIO;
  * посилання ContentService → `mlservice`, без `WaitFor`, бо перевірка лише дорадча.
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
Адміністратор ─► POST /api/copycheck/reindex ─► очистити індекс → усі пости від найстаріших:
                 кожен порівнюється лише зі старшими, тож позначається копія, а не оригінал

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
3. Один раз після підключення (і після кожної нової версії моделі) адміністратор викликає
   `POST /api/copycheck/reindex`. Пости, опубліковані раніше, потрапляють в індекс і перевіряються
   між собою. Операцію можна повторювати: вирішені модератором пари не перевідкриваються.

## Що перевірено наживо (27.10.2026)

Платформу піднято через AppHost: Postgres `pg17-trixie` на наявному томі, ML-сервіс у Docker, MinIO,
Keycloak з тестовими користувачами з realm-файлу.

* Публікація поста з зображенням: воркер забрав зображення з MinIO, і в pgvector з'явився відбиток.
* Перевірка до публікації:
  * зміненої копії **своєї** роботи — зелена зона (власні роботи виключено);
  * тієї самої копії від **іншого** автора — червона зона, тип «змінена копія», 632 ключові точки, 0,5 с.
* Публікація копії: перевірка потрапила в чергу модератора. Черга, PNG-порівняння, рішення
  модератора й переіндексація працюють через шлюз.

## Проблеми платформи, знайдені під час перевірки (не пов'язані з ML, не виправлено)

* **Шлюз відхиляє всі автентифіковані запити (401).** Причина — трансформ в `ApiGateway/Program.cs`
  (незакомічені зміни): `ProxyRequest.Headers.TryAddWithoutValidation("Authorization", ...)`
  додає заголовок удруге, хоча YARP і так його пересилає, і ContentService отримує подвійне значення.
  Перевірено: збірка шлюзу без цього блоку пропускає той самий запит (200 замість 401).
* **`PostRepository.GetByStatusAsync` нічого не знаходить**: нові пости зберігаються зі статусом `'0'`,
  а запит порівнює з `'Published'`. Тому переіндексація бере ідентифікатори постів через `ContentDbContext`,
  а статус перевіряє вже при завантаженні кожного поста.
