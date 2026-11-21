import { useLocation } from "react-router-dom";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { ButtonLink, EmptyState } from "../ui";
import { IconSearch } from "../ui/icons";
import s from "./NotFoundPage.module.css";

export default function NotFoundPage() {
  usePageMeta("Сторінку не знайдено");
  const { pathname } = useLocation();
  return (
    <Page width="narrow">
      <p className={s.code}>
        404 <span>·</span> {pathname}
      </p>
      <EmptyState
        icon={<IconSearch />}
        title="Такої сторінки немає"
        text="Можливо, посилання застаріло або в адресі помилка."
        action={
          <div style={{ display: "flex", gap: 8, flexWrap: "wrap", justifyContent: "center" }}>
            <ButtonLink to="/home" variant="primary">
              До стрічки
            </ButtonLink>
            <ButtonLink to="/search">Пошук</ButtonLink>
          </div>
        }
      />
    </Page>
  );
}
