# ml/service — сервіс пошуку копій зображень

FastAPI-сервіс, що розгортає модель з `ml/models/<версія>`. Код конвеєра не дублюється: пакет
`copydet` з `ml/research/src` вбудовується під час встановлення (`force-include` у pyproject),
тож сервіс рахує ознаки рівно так, як вони рахувались під час навчання й оцінювання
(це перевіряє `tests/test_parity.py`).

## API

| Метод | Що робить |
|---|---|
| `POST /v1/check` | зображення (`file`) або об'єкт MinIO (`bucket`, `object_name`) + `exclude_user_ids`, `exclude_team_ids` → зона, P(copy), тип збігу, до 5 кандидатів з ознаками та областями збігу |
| `POST /v1/index` | додати опубліковане зображення до індексу (pgvector, один запис на медіафайл) |
| `DELETE /v1/index/posts/{post_id}` | прибрати пост з індексу |
| `DELETE /v1/index` | очистити індекс перед повним переіндексуванням |
| `POST /v1/checks`, `GET /v1/checks`, `GET /v1/checks/{id}` | черга модератора |
| `POST /v1/checks/{id}/decision` | рішення модератора: `confirmed_copy`, `dismissed`, `open` |
| `GET /v1/checks/{id}/explanation.png` | порівняння «завантаження ↔ знайдена робота» з відповідностями |
| `GET /health`, `GET /v1/model` | стан, версія та конфігурація моделі |

## Змінні середовища

| Змінна | За замовчуванням |
|---|---|
| `MODEL_DIR` | `../models/v1` |
| `ConnectionStrings__imagecopydb` (Aspire) або `DATABASE_URL` | порожньо → індекс у пам'яті |
| `Minio__Endpoint`, `Minio__AccessKey`, `Minio__SecretKey`, `Minio__Secure` | `localhost:9000`, `minioadmin`, `minioadmin`, `false` |
| `TORCH_THREADS` | 4 |

## Локально

```bash
cd ml/service
py -m uv sync
py -m uv run pytest            # з запущеним Docker тести сховища й API йдуть і на Postgres + pgvector
py -m uv run uvicorn app.main:app --port 8000
```

Після змін у `ml/research/src/copydet` перевстановіть пакет: `py -m uv sync --reinstall-package copydet-service`.

## Docker

```bash
docker build -f ml/service/Dockerfile -t ideafusion-copydet ml
docker run -p 5010:8000 -v %cd%/ml/models:/models:ro ideafusion-copydet
```
