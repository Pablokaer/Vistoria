# 0012 — Mandatory subscription, provider-agnostic billing

**Context.** InspectFlow has no free plan. A new company goes Landing → Register → Plan → Checkout → Payment confirmation → Dashboard, and a user who abandons checkout must be able to sign in later and finish paying without creating a second account. There was no billing code. Access must be decided by the backend, and a payment must never count as approved just because the browser came back to a success URL.

**Decision.**
- New `Billing` module (schema `billing`): `Subscription` (Pending → Active ⇄ PastDue → Cancelled | Expired; Cancelled/Expired are terminal), `CheckoutSession` (one attempt at the provider) and `BillingEvent` (processed webhook ids).
- **Who pays:** the roles listed in `Billing:SubscriptionRequiredRoles` pay, which by default is `Company` only. Inspectors and tenants join for free, because companies invite them and they cannot pay for the company's work. The subscription belongs to the user who registered (the company owner), since it exists before the workspace does.
- **Access rule** (`SubscriptionAccessPolicy`, pure domain): Active → access until `CurrentPeriodEnd + GraceDays`. PastDue → access until `PastDueSince + GraceDays`, and the UI shows a billing banner. Pending, Cancelled and Expired → no access. The API, the `/me` summary and checkout all use this one rule.
- **Enforcement:** an `ActiveSubscriptionRequirement` is added to every role policy. Its handler reads the subscription from the database on each request, not from token claims, which would stay stale for up to 15 minutes. When this requirement is the only one that failed, the API answers **402** with code `subscription.required`. The frontend guards (`RoleGate`, `homeFor`) only exist to give a good navigation experience.
- **Payments behind `IPaymentProvider`:** the provider creates a hosted checkout (returning a URL), verifies webhook signatures and parses events into provider-neutral `ProviderEvent`s. There are two implementations:
  - `StripePaymentProvider`: Stripe Checkout in subscription mode over REST, with no SDK. Our checkout id is sent as `client_reference_id` and as the `Idempotency-Key`.
  - `SandboxPaymentProvider`: for Development and Testing only, unless `Billing:Sandbox:AllowOutsideDevelopment` is set. The user pays with test cards on `/checkout/sandbox/{id}`, and the sandbox then emits a Stripe-shaped event signed with a random per-process secret. That event goes through the same verification and processing path as a real webhook.
  - Provider selection mirrors the AI selection: `Auto` uses Stripe when a key is configured, otherwise the Sandbox where it is allowed, otherwise `Unavailable`.
- **Activation only from verified events.** `BillingWebhookProcessor` is the only code that changes a subscription's status. The success page (`/subscription/success`) only polls `GET /api/billing/checkout/{id}`.
- **Idempotency:**
  - A filtered unique index allows only one open subscription (Pending/Active/PastDue) per user.
  - A filtered unique index allows only one Open checkout per subscription, so an abandoned checkout is resumed rather than duplicated.
  - A unique `(provider, provider_event_id)` index stops a replayed webhook from being applied twice.
  - When parallel requests lose the race on one of these indexes, they get 409, and the client or provider retries.
- **Plans come from configuration** (`Billing:Plans`), exposed publicly at `GET /api/billing/plans`. The landing page and checkout render whatever that endpoint returns, so adding a plan requires no UI changes.

**Consequences.**
- Company staff members, if they are added later, should be checked against the company owner's subscription (today every company has a single Owner).
- Customers cannot yet manage their subscription themselves: there is no Stripe billing-portal link, card update or cancellation button.
- There is no background job that expires lapsed subscriptions. The effective status is computed at read time, and a lapsed row is closed when the user starts a new checkout.
- The Stripe adapter is covered by contract tests (fake HTTP handler, signature scheme, event mapping) but has not been run against a live Stripe account.
