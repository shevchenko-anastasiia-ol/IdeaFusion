"""Settings from environment variables (Aspire injects ConnectionStrings__imagecopydb)."""

from __future__ import annotations

from pathlib import Path

from pydantic import AliasChoices, Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_nested_delimiter="__", extra="ignore")

    model_dir: Path = Field(default=Path("../models/v1"), validation_alias=AliasChoices("MODEL_DIR", "model_dir"))
    # Npgsql-style ("Host=...;Username=...") as Aspire writes it, or a libpq URL / conninfo.
    database: str = Field(
        default="",
        validation_alias=AliasChoices("ConnectionStrings__imagecopydb", "DATABASE_URL", "database"),
    )
    minio_endpoint: str = Field(default="localhost:9000", validation_alias=AliasChoices("Minio__Endpoint", "MINIO_ENDPOINT"))
    minio_access_key: str = Field(default="minioadmin", validation_alias=AliasChoices("Minio__AccessKey", "MINIO_ACCESS_KEY"))
    minio_secret_key: str = Field(default="minioadmin", validation_alias=AliasChoices("Minio__SecretKey", "MINIO_SECRET_KEY"))
    minio_secure: bool = Field(default=False, validation_alias=AliasChoices("Minio__Secure", "MINIO_SECURE"))
    explanations_bucket: str = "ml-explanations"
    max_upload_bytes: int = 50 * 1024 * 1024
    torch_threads: int = Field(default=4, validation_alias=AliasChoices("TORCH_THREADS", "torch_threads"))


_NPGSQL_KEYS = {
    "host": "host",
    "server": "host",
    "port": "port",
    "username": "user",
    "user id": "user",
    "userid": "user",
    "user": "user",
    "password": "password",
    "database": "dbname",
    "sslmode": "sslmode",
    "ssl mode": "sslmode",
}


def to_libpq(conn: str) -> str:
    """Convert an Npgsql connection string to a libpq conninfo; other formats pass through."""
    if not conn or conn.startswith(("postgres://", "postgresql://")) or "=" not in conn or ";" not in conn:
        return conn
    parts = []
    for item in filter(None, (p.strip() for p in conn.split(";"))):
        key, _, value = item.partition("=")
        mapped = _NPGSQL_KEYS.get(key.strip().lower())
        if mapped:
            value = value.strip().replace("\\", "\\\\").replace("'", "\\'")
            parts.append(f"{mapped}='{value.lower() if mapped == 'sslmode' else value}'")
    return " ".join(parts)
