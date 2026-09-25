# Stripe Checkout

Server configuration supports the existing Render variable names:

- `STRIPE_MODE=Test` (default; use `Live` only when ready)
- `STRIPE__SECRETKEY_TEST` / `STRIPE__SECRETKEY_LIVE`
- `STRIPE__PUBLISHABLEKEY_TEST` / `STRIPE__PUBLISHABLEKEY_LIVE` may remain set; hosted Checkout redirects do not require these in the browser.
- `STRIPE__CLIENTURL=https://higiatrade.com` (optional, defaults to `ClientApp:BaseUrl`)
- `STRIPE__WEBHOOKSECRET_TEST` / `STRIPE__WEBHOOKSECRET_LIVE`, or `STRIPE__WEBHOOKSECRET` for the selected mode.

No keys belong in source control. A missing/placeholder secret disables the card option.

Webhook: `POST /api/payments/stripe/webhook`. Register checkout.session.completed,
checkout.session.expired, checkout.session.async_payment_succeeded and
checkout.session.async_payment_failed in the matching Stripe mode. The endpoint
always verifies the raw body signature. Other applications' sessions are ignored.

A server worker reconciles pending payments every minute using Stripe's API;
return-page verification and the worker also work without a configured webhook.
Payment is never accepted based on redirect parameters. Amount, EUR currency,
order metadata and Test/Live mode are checked before changing the order to Paid.

Creating a session reprices products server-side and reserves stock in a PostgreSQL
transaction. Same-attempt retries use a durable fingerprint and Stripe idempotency
key. Expired/cancelled sessions restore stock exactly once. Network errors retain
the reservation until Stripe confirms the outcome. Reservations last up to one hour.
Older ambiguous session creation attempts are looked up in Stripe before release.

Unpaid card orders are excluded from purchase eligibility and cannot be manually
fulfilled. Paid card orders cannot be cancelled through the generic status API:
refunds must be managed in Stripe. This integration does not implement refunds.

Test in Stripe Test mode using 4242 4242 4242 4242, a future expiry and any three-digit
CVC. Check a successful payment, declined payment, cancel/expired session,
return-page refresh, duplicate webhook and a retry after a network interruption.
Expected stock effect: reserve once, keep on paid, restore once on expiry.
