# Khidma demo script

Passwords are in user secrets (`Seed:DemoPassword`, `Seed:AdminPassword`). They are not in this repository.

## Accounts

| Role | Email | Notes |
| --- | --- | --- |
| Customer | `customer@khidma.local` | Ramallah |
| Provider (approved) | `provider1@khidma.local` | Ramallah, Home Services |
| Provider (pending) | `provider2@khidma.local` | Hebron — use for admin approval |
| Admin | `admin@khidma.local` | Catalog and approval |

## Happy path

1. Sign in as the customer. Open **My Requests** → **Create a request**. Choose Plumbing, city Ramallah, a future date.
2. Sign in as provider 1. Confirm the request is on **Available Requests**. Submit an offer with a price and future estimated date.
3. Back as the customer, open the request, accept the offer (confirm dialog). A booking is created.
4. As the provider, open the booking → **Start work** → **Complete job**.
5. As the customer, open the booking and leave a 1–5 star review. The public provider page shows the updated rating.

## Admin

1. Sign in as admin. **Providers** lists provider 2 as pending. Approve them.
2. **Catalog** can add a category or service; deleting a used category returns a conflict message.
