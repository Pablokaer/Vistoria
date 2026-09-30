import Link from "next/link";
import { Brand } from "@/components/AppShell";
import { Icon } from "./icons";
import { GET_STARTED_HREF } from "@/lib/billing";

export function FinalCallToAction() {
  return (
    <section aria-labelledby="cta-title" className="bg-white pb-20 sm:pb-24">
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        <div className="rounded-3xl bg-brand px-6 py-12 text-center sm:px-12 sm:py-16">
          <h2 id="cta-title" className="text-3xl font-semibold tracking-tight text-white sm:text-4xl">Your next inspection, documented properly.</h2>
          <p className="mx-auto mt-4 max-w-xl text-lg text-blue-100">Create your company account, subscribe and publish your first inspection today.</p>
          <Link href={GET_STARTED_HREF} className="mt-8 inline-flex min-h-12 items-center gap-2 rounded-lg bg-white px-6 text-base font-semibold text-brand hover:bg-blue-50">
            Get started <Icon name="arrowRight" className="h-4 w-4" />
          </Link>
        </div>
      </div>
    </section>
  );
}

export function LandingFooter() {
  return (
    <footer className="border-t border-slate-200 bg-slate-50">
      <div className="mx-auto flex max-w-6xl flex-col gap-6 px-4 py-10 sm:flex-row sm:items-center sm:justify-between sm:px-6">
        <div>
          <Brand alwaysShowName />
          <p className="mt-2 text-sm text-slate-500">Property inspections for companies, inspectors and tenants.</p>
        </div>
        <nav aria-label="Footer" className="flex flex-wrap gap-x-6 gap-y-2 text-sm text-slate-600">
          <Link href="/#how-it-works" className="hover:text-ink">How it works</Link>
          <Link href="/#features" className="hover:text-ink">Features</Link>
          <Link href="/pricing" className="hover:text-ink">Pricing</Link>
          <Link href="/login" className="hover:text-ink">Sign in</Link>
        </nav>
      </div>
    </footer>
  );
}
