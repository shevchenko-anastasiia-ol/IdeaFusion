import type { Tone } from "../ui";
import { applicationStatusKey, vacancyStatusKey } from "../api/types";

export function teamStatusTone(status: number | string): Tone {
  const n = typeof status === "string" ? parseInt(status, 10) : status;
  if (n === 0 || status === "Active") return "teal";
  if (n === 1 || status === "Searching") return "amber";
  return "neutral";
}

export function vacancyTone(status: number | string): Tone {
  return vacancyStatusKey(status) === "open" ? "teal" : "neutral";
}

export function applicationTone(status: number | string): Tone {
  switch (applicationStatusKey(status)) {
    case "pending": return "amber";
    case "accepted": return "teal";
    case "rejected": return "red";
    default: return "neutral";
  }
}

export function requestTone(key: "new" | "accepted" | "rejected" | "cancelled"): Tone {
  switch (key) {
    case "new": return "amber";
    case "accepted": return "teal";
    case "rejected": return "red";
    default: return "neutral";
  }
}

export const REQUEST_LABEL: Record<"new" | "accepted" | "rejected" | "cancelled", string> = {
  new: "Очікує",
  accepted: "Прийнято",
  rejected: "Відхилено",
  cancelled: "Скасовано",
};
