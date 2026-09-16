# Khidma demo script

Passwords are in user secrets (`Seed:DemoPassword`, `Seed:AdminPassword`). They are not in this repository.

## Accounts

| Role | Email | Notes |
| --- | --- | --- |
| Customer | `customer@khidma.local` | Ramallah |
| Provider (approved) | `provider1@khidma.local` | Ramallah, Home Services |
| Provider (pending review) | `provider2@khidma.local` | Hebron — upload documents, then admin review |
| Admin | `admin@khidma.local` | Verification, suspension, users, audit, catalog |

## Happy path

1. Sign in as the customer. Open **My Requests** → **Create a request**. Choose Plumbing, city Ramallah, a future date.
2. Sign in as provider 1. Confirm the request is on **Available Requests**. Submit an offer with a price and future estimated date.
3. Back as the customer, open the request, accept the offer (confirm dialog). A booking is created.
4. As the provider, open the booking → **Start work** → **Complete job**.
5. As the customer, open the booking and leave a 1–5 star review. The public provider page shows the updated rating.

## Admin

1. Sign in as admin. **Overview** shows stats, a needs-attention queue, and recent audit events.
2. **Provider Verification** lists provider 2 as pending review. Open the profile, review documents, then approve the provider only after a document is approved.
3. **Providers** can suspend with a reason (pending offers are rejected; bookings continue) and reactivate.
4. **Users** and **Audit Logs** are read-only monitoring. Logs cannot be edited or deleted.
5. **Catalog** can add a category or service; deleting a used category returns a conflict message.
