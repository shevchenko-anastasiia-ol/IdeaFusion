# Build context: the ml/ folder (the service imports research/src/copydet).
#   docker build -f service/Dockerfile -t ideafusion-copydet ml/
# Model artefacts are mounted at /models (see AppHost), not baked into the image.
FROM python:3.12-slim

ENV PYTHONUNBUFFERED=1 PIP_NO_CACHE_DIR=1 UV_LINK_MODE=copy
RUN apt-get update \
    && apt-get install -y --no-install-recommends libglib2.0-0 libgomp1 \
    && rm -rf /var/lib/apt/lists/* \
    && pip install "uv>=0.5"

WORKDIR /srv/ml
COPY research/src/copydet research/src/copydet
COPY service/pyproject.toml service/uv.lock service/
COPY service/app service/app

WORKDIR /srv/ml/service
RUN uv sync --frozen --no-dev --no-editable

ENV PATH=/srv/ml/service/.venv/bin:$PATH \
    MODEL_DIR=/models/v1 \
    TORCH_THREADS=4
EXPOSE 8000
HEALTHCHECK --interval=30s --timeout=5s --start-period=60s CMD python -c "import urllib.request; urllib.request.urlopen('http://localhost:8000/health')"
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
