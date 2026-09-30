/** Joins class names, skipping falsy values. Example: `cx("px-2", active && "bg-brand-50")`. */
export const cx = (...classes: (string | false | null | undefined)[]): string => classes.filter(Boolean).join(" ");
