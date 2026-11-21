export type MediaKind = "image" | "video" | "audio" | "pdf";

export function mediaKind(url: string): MediaKind {
  const ext = url.split("?")[0].split(".").pop()?.toLowerCase() ?? "";
  if (["mp4", "webm", "mov", "avi", "mkv"].includes(ext)) return "video";
  if (["mp3", "wav", "ogg", "aac", "m4a", "flac"].includes(ext)) return "audio";
  if (ext === "pdf") return "pdf";
  return "image";
}

export function fileNameOf(url: string): string {
  const clean = url.split("?")[0];
  return decodeURIComponent(clean.substring(clean.lastIndexOf("/") + 1)) || url;
}

export function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}
