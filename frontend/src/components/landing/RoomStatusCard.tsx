import Image from "next/image";
import { Icon } from "@/components/icons";
import { getServerTranslator } from "@/i18n/server";
import { landingMessages } from "@/i18n/messages/landing";
import { cx } from "@/lib/cx";
import { heroCardLivingRoom } from "./landingPhotos";

/**
 * A completed room as the inspection app lists it: thumbnail, room name and position, photo count, AI description
 * and the success status badge the app uses. Shown over the hero photo; the caption describes it to screen readers.
 * Example: `<RoomStatusCard className="absolute bottom-6 left-6" />`
 */
export async function RoomStatusCard({ className }: { className?: string }) {
  const t = await getServerTranslator(landingMessages);
  return (
    <figure className={cx("w-[19.5rem] max-w-full rounded-xl border border-site-line bg-white p-3 shadow-site-float", className)}>
      <figcaption className="sr-only">{t("cardCaption")}</figcaption>
      <div aria-hidden="true">
        <div className="flex gap-3">
          <Image src={heroCardLivingRoom} alt="" sizes="88px" className="h-[4.25rem] w-[5.5rem] shrink-0 rounded-md object-cover" />
          <div className="min-w-0 pt-0.5">
            <p className="flex items-baseline justify-between gap-2">
              <span className="text-[0.9375rem] font-semibold text-site-ink">{t("cardRoom")}</span>
              <span className="text-[0.6875rem] text-site-muted">{t("cardRoomCount")}</span>
            </p>
            <p className="mt-1 text-[0.8125rem] leading-snug text-site-muted">{t("cardPhotos")}</p>
            <p className="text-[0.8125rem] leading-snug text-site-muted">{t("cardReady")}</p>
          </div>
        </div>
        <div className="mt-3 flex items-center justify-between border-t border-site-line pt-2.5">
          <span className="text-[0.75rem] text-site-muted">{t("cardStatus")}</span>
          <span className="inline-flex items-center gap-1 rounded-sm bg-success-50 px-2 py-0.5 text-[0.75rem] font-semibold text-success-700 ring-1 ring-inset ring-success-700/15">
            <Icon name="checkCircle" className="h-3.5 w-3.5" />{t("cardCompleted")}
          </span>
        </div>
      </div>
    </figure>
  );
}
