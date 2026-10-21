# ml/research — пошук копій зображень

Дослідницька частина ML-модуля IdeaFusion: дані, бейзлайни, навчання, оцінювання.
Загальний план — `../../../ML_PLAN.md`. Пакет `copydet` (у `src/`) згодом використовуватиме й сервіс.

## Середовище

Python 3.12 через [uv](https://docs.astral.sh/uv/). Якщо `uv` немає в PATH, запускайте як `py -m uv`.

```bash
py -m uv sync                      # етап 1: AugLy, Pillow, pandas, imagehash...
py -m uv sync --extra models       # етапи 2-4: torch (CPU), open_clip, faiss, opencv
py -m uv run pytest                # тести побудови датасету на синтетичних даних
```

На Windows замість `python-magic` ставиться `python-magic-bin`: без libmagic AugLy зависає на імпорті.

## Етап 1: датасет

```bash
# 1. Джерела (ArtBench-10, лише суспільне надбання, 46 тис. робіт, ~1,5 ГБ)
py -m uv run python scripts/01_download.py artbench-pd
#    необов'язково: WikiArt з мітками стилю (усього 34 ГБ, беремо частину)
py -m uv run python scripts/01_download.py wikiart --max-shards 5

# 2. Спільний каталог (+ власні роботи: data/raw/custom/<художник>/<файл>)
py -m uv run python scripts/02_build_catalog.py [--custom data/raw/custom]

# 3. Швидка перевірка на малому обсязі, потім повний набір
py -m uv run python scripts/03_build_dataset.py --n-originals 60 --name smoke
py -m uv run python scripts/03_build_dataset.py --config configs/dataset.yaml

# 4. Сітка прикладів для перевірки очима і для пояснювальної записки
py -m uv run python scripts/04_preview_dataset.py data/processed/artbench_v1
```

Офіційний сервер ArtBench (`artbench.eecs.berkeley.edu`) віддає 404, тому береться дзеркало
`alfredplpl/artbench-pd-256x256` на Hugging Face (256×256, мітки художника, без міток стилю).
Мітки стилю для негативів «той самий стиль» дає WikiArt.

### Що виходить (`data/processed/<name>/`)

| Файл | Зміст |
|---|---|
| `originals.parquet` | еталонні роботи: `image_id, path, split, artist, style, phash, phash_mirror` |
| `queries.parquet` | модифікації (`original_id`, `aug_type`, `params` у JSON) і складні негативи (`is_negative`, `negative_kind`: `same_artist` / `same_style`, `original_id` порожній) |
| `distractors.parquet` | сторонні роботи: тло для колажів і «шум» в індексі |
| `dataset_meta.json` | конфіг, версії, підсумкова статистика |

12 типів модифікацій (`src/copydet/augment.py`): `jpeg, resize, crop, hflip, rotate, color,
filter, text_overlay, watermark, screenshot, collage, fragment`.

Правила з розділу 1.4.3 перевіряються під час кожної збірки:
- розбиття за художником;
- `collage` і `screenshot` трапляються лише в `test`;
- негативи, що є майже-дублікатами оригіналу за pHash, відкидаються;
- кожен запит детермінований (seed + id), тож `params` повністю описують модифікацію.

DISC21 і Copydays завантажуються вручну (DISC21 потребує прийняття ліцензії на сайті Meta AI);
вони знадобляться на етапі 2 для перевірки відтворення SSCD і контролю «забування».
