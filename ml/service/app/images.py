"""Where images come from: MinIO (the platform's media store) or, in tests, a dict."""

from __future__ import annotations

from typing import Protocol

from copydet.imageio import load_rgb
from PIL import Image


class ImageSource(Protocol):
    def get(self, bucket: str, object_name: str) -> bytes: ...
    def put(self, bucket: str, object_name: str, data: bytes, content_type: str) -> None: ...


class MinioSource:
    def __init__(self, endpoint: str, access_key: str, secret_key: str, secure: bool = False):
        from minio import Minio

        self.client = Minio(endpoint, access_key=access_key, secret_key=secret_key, secure=secure)

    def get(self, bucket: str, object_name: str) -> bytes:
        resp = self.client.get_object(bucket, object_name)
        try:
            return resp.read()
        finally:
            resp.close()
            resp.release_conn()

    def put(self, bucket: str, object_name: str, data: bytes, content_type: str) -> None:
        import io

        if not self.client.bucket_exists(bucket):
            self.client.make_bucket(bucket)
        self.client.put_object(bucket, object_name, io.BytesIO(data), len(data), content_type=content_type)


class DictSource:
    def __init__(self, items: dict[tuple[str, str], bytes] | None = None):
        self.items = items or {}

    def get(self, bucket: str, object_name: str) -> bytes:
        try:
            return self.items[(bucket, object_name)]
        except KeyError:
            raise FileNotFoundError(f"{bucket}/{object_name}") from None

    def put(self, bucket: str, object_name: str, data: bytes, content_type: str) -> None:
        self.items[(bucket, object_name)] = data


def open_image(data: bytes) -> Image.Image:
    return load_rgb(data)
