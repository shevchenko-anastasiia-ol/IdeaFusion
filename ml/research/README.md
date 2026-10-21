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

## Етап 2: протокол оцінювання і бейзлайни

```bash
py -m uv sync --extra models
curl -L -o data/weights/sscd_disc_mixup.torchscript.pt https://dl.fbaipublicfiles.com/sscd-copy-detection/sscd_disc_mixup.torchscript.pt
py -m uv run python scripts/05_eval_baselines.py --dataset data/processed/artbench_v1
```

Протокол (`src/copydet/evaluation.py`):
- **Індекс на спліт** складається з оригіналів цього спліта та всіх дистракторів.
- **Запити** — модифікації (у кожної один правильний оригінал) і складні негативи (без правильної відповіді).
- **μAP** — офіційна метрика DISC21: усі передбачення всіх запитів ранжуються разом.
- **Повнота при точності ≥ 0,99** — загальна і для кожного типу модифікації. Поріг підбирається на val і переноситься на test.
- **Частка хибних збігів на складних негативах (FPR)** — при P ≥ 0,99 і при повноті 80%. Якщо повноти 80% метод не досягає, у звіті «—».
- **ROC-AUC** «правильно знайдена копія vs складний негатив» і **мс на зображення** (CPU).

Варіант `+norm` — нормалізація оцінки з SSCD: від схожості віднімається схожість до найближчої
фонової роботи (2000 робіт каталогу, яких немає в датасеті).

Ембединги кешуються в `data/embeddings/<dataset>/`, результати пишуться в `runs/baselines_<dataset>/`
(`summary.md`, `per_type.csv`, `per_type.png`, `metrics.json`).

## Етап 3: проєкційна голова над замороженим SSCD

```bash
py -m uv run python scripts/06_embed_train.py     # ~40 хв на CPU: 3,7 тис. зображень x 6 переглядів
py -m uv run python scripts/07_train_head.py      # хвилини: 3 варіанти x 3 seeds
```

- **Дані** (`06`): train-оригінали, train-негативи й до 2 000 робіт художників, яких немає у val/test (без
  дистракторів і фонового набору). На кожне зображення чиста версія й 5 модифікацій лише навчальних типів.
- **Голова** (`src/copydet/head.py`): залишкова, `normalize(x + MLP(x))`; останній шар ініціалізовано нулями,
  тому на старті вона збігається з SSCD.
- **Батчі**: семплер за художником кладе в батч кілька робіт кожного художника, тож негативами в InfoNCE
  стають роботи тієї ж руки чи серії. Абляція: випадкові батчі.
- **Вибір епохи** — на val за повнотою при ≤ 1% хибних звинувачень; test оцінюється з порогами з val.
- Колажі й скріншоти в навчання не потрапляють, тому test показує, чи збереглася стійкість SSCD.

Результати: `runs/head_<dataset>/summary.md` (середнє ± std за seeds), ваги — `runs/head_<dataset>/<variant>/seed<k>/head.pt`.

DISC21 і Copydays завантажуються вручну (DISC21 потребує прийняття ліцензії на сайті Meta AI);
вони знадобляться на етапі 2 для перевірки відтворення SSCD і контролю «забування».
