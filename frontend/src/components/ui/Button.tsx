"use client";

import Link from "next/link";
import type { ButtonHTMLAttributes, ReactNode } from "react";
import { Icon, type IconName } from "@/components/icons";
import { cx } from "@/lib/cx";
import { Spinner } from "./Feedback";

export type ButtonVariant = "primary" | "secondary" | "danger" | "ghost" | "subtle";
export type ButtonSize = "sm" | "md" | "lg";

// Primary = the one main action of a view. Secondary = neutral outline. Subtle = quiet toolbar action.
const VARIANTS: Record<ButtonVariant, string> = {
  primary: "bg-brand text-white shadow-card hover:bg-brand-dark active:bg-brand-900 disabled:bg-ink-4",
  secondary: "border border-line-strong bg-surface text-ink hover:bg-surface-2 hover:border-ink-4 disabled:text-ink-4",
  danger: "border border-danger-500/40 bg-surface text-danger-700 hover:bg-danger-50",
  ghost: "text-brand hover:bg-brand-50",
  subtle: "text-ink-2 hover:bg-neutral-50 hover:text-ink",
};

const SIZES: Record<ButtonSize, string> = {
  sm: "min-h-8 gap-1.5 px-2.5 text-label",
  md: "min-h-10 gap-2 px-3.5 text-body",
  lg: "min-h-12 gap-2 px-5 text-section",
};

const BASE = "inline-flex select-none items-center justify-center rounded-md font-medium transition duration-150 active:scale-[0.98] disabled:cursor-not-allowed disabled:active:scale-100";

/** Classes of a button, for links and other elements that must look like one. */
export function buttonClasses(variant: ButtonVariant = "primary", size: ButtonSize = "md", className?: string): string {
  return cx(BASE, SIZES[size], VARIANTS[variant], className);
}

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & { variant?: ButtonVariant; size?: ButtonSize; loading?: boolean; icon?: IconName };

/** Example: `<Button icon="plus" onClick={add}>New inspection</Button>` */
export function Button({ variant = "primary", size = "md", className, loading, icon, children, ...props }: ButtonProps) {
  return (
    <button {...props} disabled={props.disabled || loading} className={buttonClasses(variant, size, className)}>
      {loading ? <Spinner small /> : icon && <Icon name={icon} className="h-4 w-4 shrink-0" />}
      {children}
    </button>
  );
}

/** A link styled as a button. Example: `<LinkButton href="/agent/available" icon="search">Browse</LinkButton>` */
export function LinkButton({ href, variant = "primary", size = "md", className, icon, children }:
  { href: string; variant?: ButtonVariant; size?: ButtonSize; className?: string; icon?: IconName; children: ReactNode }) {
  return (
    <Link href={href} className={buttonClasses(variant, size, className)}>
      {icon && <Icon name={icon} className="h-4 w-4 shrink-0" />}
      {children}
    </Link>
  );
}

/** Square icon-only button; `label` is required because it is the accessible name. */
export function IconButton({ icon, label, className, ...props }: ButtonHTMLAttributes<HTMLButtonElement> & { icon: IconName; label: string }) {
  return (
    <button type="button" aria-label={label} title={label} {...props}
      className={cx("inline-grid h-9 w-9 place-items-center rounded-md text-ink-3 transition hover:bg-neutral-50 hover:text-ink", className)}>
      <Icon name={icon} className="h-5 w-5" />
    </button>
  );
}
