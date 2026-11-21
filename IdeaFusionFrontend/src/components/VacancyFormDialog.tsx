import { useEffect, useState } from "react";
import { Button, Dialog, Field, Input, Notice, Textarea } from "../ui";

export interface VacancyFormValues {
  role: string;
  description?: string;
  requirements?: string;
}

/**
 * One dialog for both creating a vacancy and editing an existing one: with `initial`
 * it opens prefilled and submits as "Зберегти".
 */
export default function VacancyFormDialog({
  open,
  initial,
  onClose,
  onSubmit,
}: {
  open: boolean;
  initial?: VacancyFormValues | null;
  onClose: () => void;
  onSubmit: (values: VacancyFormValues) => Promise<void>;
}) {
  const editing = !!initial;
  const [role, setRole] = useState("");
  const [desc, setDesc] = useState("");
  const [req, setReq] = useState("");
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;
    setRole(initial?.role ?? "");
    setDesc(initial?.description ?? "");
    setReq(initial?.requirements ?? "");
    setErr(null);
    // Reset only when the dialog opens, so a parent re-render does not wipe what is typed.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  async function submit() {
    if (!role.trim()) return;
    setBusy(true);
    setErr(null);
    try {
      await onSubmit({ role: role.trim(), description: desc.trim() || undefined, requirements: req.trim() || undefined });
      onClose();
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Помилка");
    } finally {
      setBusy(false);
    }
  }

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={editing ? "Редагувати вакансію" : "Нова вакансія"}
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>
            Скасувати
          </Button>
          <Button variant="primary" loading={busy} disabled={!role.trim()} onClick={() => void submit()}>
            {editing ? "Зберегти" : "Створити"}
          </Button>
        </>
      }
    >
      <Field label="Роль" required htmlFor="vac-role">
        <Input id="vac-role" data-autofocus placeholder="Напр.: 2D артист" maxLength={100} value={role} onChange={e => setRole(e.target.value)} />
      </Field>
      <Field label="Опис задачі" htmlFor="vac-desc">
        <Textarea id="vac-desc" rows={4} maxLength={2000} placeholder="Що саме треба робити в проєкті" value={desc} onChange={e => setDesc(e.target.value)} />
      </Field>
      <Field label="Вимоги до кандидата" htmlFor="vac-req">
        <Textarea id="vac-req" rows={3} maxLength={2000} placeholder="Інструменти, досвід, портфоліо" value={req} onChange={e => setReq(e.target.value)} />
      </Field>
      {err && <Notice tone="error">{err}</Notice>}
    </Dialog>
  );
}
