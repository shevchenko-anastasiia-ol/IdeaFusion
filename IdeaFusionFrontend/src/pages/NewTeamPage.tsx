import { useEffect, useRef, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { teamsApi } from "../api/teams";
import { authApi } from "../api/auth";
import { useAuth } from "../context/AuthContext";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { Avatar, Button, Field, Input, Notice, PageHeader, Select, Tag, Textarea, useToast } from "../ui";
import { IconPlus, IconUpload } from "../ui/icons";
import sh from "./shared.module.css";
import s from "./NewTeamPage.module.css";

const CATEGORIES = ["Дизайн", "Розробка", "Музика", "Арт", "Фото", "Відео", "Анімація", "Gamedev", "Медіа"];
const CLASSIFICATIONS = [
  "UI/UX дизайн", "Графічний дизайн", "Веб-дизайн", "Моушн-дизайн", "3D дизайн",
  "Веб-розробка", "Мобільна розробка", "Gamedev", "Backend розробка", "Ілюстрація",
  "Концепт-арт", "Анімація", "Комікси та манга", "Композиція", "Саундтрек / OST",
  "Звукорежисура", "Вокал", "Фотографія", "Відеомонтаж", "Сценарист",
];
const ROLES = [
  "UI/UX дизайнер", "Графічний дизайнер", "Веб-розробник", "Мобільний розробник",
  "Game developer", "Backend розробник", "Ілюстратор", "Аніматор", "Концепт-художник",
  "Композитор", "Звукорежисер", "Вокаліст", "Фотограф", "Відеомонтажер", "Сценарист", "3D художник",
];

function toggle(list: string[], value: string): string[] {
  return list.includes(value) ? list.filter(v => v !== value) : [...list, value];
}

export default function NewTeamPage() {
  usePageMeta("Нова команда", [{ label: "Команди", to: "/collaborations" }]);
  const navigate = useNavigate();
  const { user } = useAuth();
  const toast = useToast();
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [category, setCategory] = useState("");
  const [tags, setTags] = useState<string[]>([]);
  const [roles, setRoles] = useState<string[]>([]);
  const [customTag, setCustomTag] = useState("");
  const [customRole, setCustomRole] = useState("");
  const [avatarFile, setAvatarFile] = useState<File | null>(null);
  const [avatarPreview, setAvatarPreview] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [touched, setTouched] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => () => {
    if (avatarPreview) URL.revokeObjectURL(avatarPreview);
  }, [avatarPreview]);

  function handleAvatarChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    setAvatarFile(file);
    setAvatarPreview(URL.createObjectURL(file));
  }

  function clearAvatar() {
    setAvatarFile(null);
    setAvatarPreview(null);
    if (fileInputRef.current) fileInputRef.current.value = "";
  }

  function addCustom(kind: "tag" | "role") {
    const value = (kind === "tag" ? customTag : customRole).trim();
    if (!value) return;
    if (kind === "tag") {
      setTags(t => (t.includes(value) ? t : [...t, value]));
      setCustomTag("");
    } else {
      setRoles(r => (r.includes(value) ? r : [...r, value]));
      setCustomRole("");
    }
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setTouched(true);
    if (!name.trim() || !description.trim() || !category) {
      setError("Заповніть назву, опис і категорію");
      return;
    }
    if (!user) {
      navigate("/login");
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      const allTags = customTag.trim() ? [...tags, customTag.trim()] : tags;
      let teamAvatarUrl: string | undefined;
      if (avatarFile) {
        try {
          const { url } = await authApi.uploadFile(avatarFile);
          teamAvatarUrl = url;
        } catch {
          toast.error("Аватар не завантажився — команду створено без нього");
        }
      }
      const team = await teamsApi.create({ name: name.trim(), description: description.trim(), category, tags: allTags, teamAvatarUrl });
      const failedRoles: string[] = [];
      for (const role of roles) {
        try {
          await teamsApi.addRequiredRole(team.id, { teamId: team.id, role });
        } catch {
          failedRoles.push(role);
        }
      }
      if (failedRoles.length) toast.error(`Не вдалося додати ролі: ${failedRoles.join(", ")}`);
      const opened = roles.length - failedRoles.length;
      toast.success(
        opened > 0
          ? `Команду «${team.name}» створено, відкрито вакансій: ${opened}. Додайте до них опис у вкладці «Вакансії».`
          : `Команду «${team.name}» створено`,
      );
      navigate(`/teams/${team.id}`);
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : String(ex));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Page width="narrow">
      <PageHeader title="Нова команда" description="Опишіть проєкт і кого ви шукаєте — команда одразу з'явиться в каталозі." />

      <form className={sh.form} onSubmit={e => void handleCreate(e)} noValidate>
        <section className={sh.formSection}>
          <div className={s.identity}>
            <div className={s.avatarPick}>
              <button type="button" className={s.avatarBtn} onClick={() => fileInputRef.current?.click()} aria-label="Обрати аватар команди">
                {avatarPreview ? <img src={avatarPreview} alt="" /> : name ? <Avatar name={name} size={72} square seed={name} /> : <IconUpload size={22} />}
              </button>
              <input ref={fileInputRef} type="file" accept="image/*" className="visually-hidden" tabIndex={-1} onChange={handleAvatarChange} />
              {avatarPreview ? (
                <button type="button" className={s.linkDanger} onClick={clearAvatar}>
                  прибрати
                </button>
              ) : (
                <span className={sh.mono}>аватар</span>
              )}
            </div>
            <div className={s.identityFields}>
              <Field label="Назва" required htmlFor="team-name" error={touched && !name.trim() ? "Обов'язкове поле" : null}>
                <Input id="team-name" value={name} invalid={touched && !name.trim()} onChange={e => setName(e.target.value)} autoFocus />
              </Field>
              <Field label="Категорія" required htmlFor="team-cat" error={touched && !category ? "Оберіть категорію" : null}>
                <Select id="team-cat" value={category} onChange={e => setCategory(e.target.value)}>
                  <option value="">Оберіть категорію…</option>
                  {CATEGORIES.map(c => (
                    <option key={c}>{c}</option>
                  ))}
                </Select>
              </Field>
            </div>
          </div>
          <Field label="Опис" required htmlFor="team-desc" error={touched && !description.trim() ? "Обов'язкове поле" : null}>
            <Textarea id="team-desc" rows={4} value={description} invalid={touched && !description.trim()} onChange={e => setDescription(e.target.value)} placeholder="Над чим працюєте, яка мета, як влаштована співпраця…" />
          </Field>
        </section>

        <section className={sh.formSection}>
          <span className={s.label}>Класифікація</span>
          <div className={s.chips} role="group" aria-label="Класифікація">
            {CLASSIFICATIONS.map(c => (
              <Tag key={c} active={tags.includes(c)} onClick={() => setTags(t => toggle(t, c))}>
                {c}
              </Tag>
            ))}
            {tags.filter(t => !CLASSIFICATIONS.includes(t)).map(t => (
              <Tag key={t} active onClick={() => setTags(list => toggle(list, t))}>
                {t}
              </Tag>
            ))}
          </div>
          <div className={s.addRow}>
            <Input
              aria-label="Власна класифікація"
              placeholder="Власна класифікація…"
              value={customTag}
              onChange={e => setCustomTag(e.target.value)}
              onKeyDown={e => {
                if (e.key === "Enter") {
                  e.preventDefault();
                  addCustom("tag");
                }
              }}
            />
            <Button icon={<IconPlus size={15} />} onClick={() => addCustom("tag")} disabled={!customTag.trim()}>
              Додати
            </Button>
          </div>
        </section>

        <section className={sh.formSection}>
          <span className={s.label}>Кого шукаєте</span>
          <div className={s.chips} role="group" aria-label="Необхідні ролі">
            {ROLES.map(r => (
              <Tag key={r} active={roles.includes(r)} onClick={() => setRoles(list => toggle(list, r))}>
                {r}
              </Tag>
            ))}
            {roles.filter(r => !ROLES.includes(r)).map(r => (
              <Tag key={r} active onClick={() => setRoles(list => toggle(list, r))}>
                {r}
              </Tag>
            ))}
          </div>
          <div className={s.addRow}>
            <Input
              aria-label="Інша роль"
              placeholder="Інша роль…"
              value={customRole}
              onChange={e => setCustomRole(e.target.value)}
              onKeyDown={e => {
                if (e.key === "Enter") {
                  e.preventDefault();
                  addCustom("role");
                }
              }}
            />
            <Button icon={<IconPlus size={15} />} onClick={() => addCustom("role")} disabled={!customRole.trim()}>
              Додати
            </Button>
          </div>
          <span className={sh.mono}>ролі можна змінити пізніше на сторінці команди</span>
        </section>

        {error && <Notice tone="error">{error}</Notice>}

        <div className={sh.formActions}>
          <Button variant="ghost" onClick={() => navigate(-1)}>
            Скасувати
          </Button>
          <Button type="submit" variant="primary" loading={submitting}>
            Створити команду
          </Button>
        </div>
      </form>
    </Page>
  );
}
