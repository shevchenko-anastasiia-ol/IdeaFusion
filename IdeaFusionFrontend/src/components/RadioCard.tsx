import type { ReactNode } from "react";
import { cx } from "../ui";
import s from "./RadioCard.module.css";

/** A selectable row inside a radio group (team picker etc.). */
export default function RadioCard({
  checked,
  onSelect,
  children,
  name,
}: {
  checked: boolean;
  onSelect: () => void;
  children: ReactNode;
  name: string;
}) {
  return (
    <label className={cx(s.card, checked && s.checked)}>
      <input type="radio" name={name} checked={checked} onChange={onSelect} className={s.input} />
      <span className={s.body}>{children}</span>
      <span className={s.dot} aria-hidden="true" />
    </label>
  );
}
