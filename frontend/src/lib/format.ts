const dateFmt = new Intl.DateTimeFormat("en-IE", { day: "numeric", month: "short", year: "numeric" });
const dateTimeFmt = new Intl.DateTimeFormat("en-IE", { day: "numeric", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" });

export const formatDate = (v?: string | null) => (v ? dateFmt.format(new Date(v)) : "—");
export const formatDateTime = (v?: string | null) => (v ? dateTimeFmt.format(new Date(v)) : "—");

const labels: Record<string, string> = {
  MoveIn: "Move In", MoveOut: "Move Out", InProgress: "In progress", AwaitingTenant: "Awaiting tenant",
  LivingRoom: "Living room", DiningRoom: "Dining room", NewDamage: "New damage", PreExisting: "Pre-existing",
  NormalWear: "Normal wear", UnableToDetermine: "Unable to determine", Review: "In review",
};
export const humanize = (v?: string | null) => (v ? labels[v] ?? v.replace(/([a-z])([A-Z])/g, "$1 $2") : "—");

export const fileSize = (bytes: number) => (bytes > 1024 * 1024 ? `${(bytes / 1024 / 1024).toFixed(1)} MB` : `${Math.round(bytes / 1024)} KB`);
