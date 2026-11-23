# Patient appointment payment flow

The patient online-booking flow now creates a pending appointment and a payment record together in the API workflow.

## Flow

1. A signed-in patient selects an active doctor and an available slot.
2. `POST /api/v1/me/appointment-payments/checkout` creates a pending appointment and snapshots the doctor's current `VisitFeeToman`.
3. The selected slot is held for 10 minutes while the patient makes a bank transfer.
4. The patient submits the transaction reference and a JPG, PNG, or WEBP receipt (maximum 1 MB) to `POST /api/v1/me/appointment-payments/{id}/receipt`.
5. A submitted receipt remains held until staff review. The 10-minute expiry applies only before a receipt is submitted.
6. A clinic administrator or super administrator approves or rejects the receipt from `/admin/payments`. Approval confirms the appointment; rejection cancels it and releases the slot.
7. A background worker checks unpaid holds every 30 seconds and releases expired appointments.

Direct patient appointment booking is no longer exposed. Staff-created appointments remain available through the reception endpoint.

## Bank transfer configuration

Set these values in API configuration (prefer environment variables or user secrets outside local development):

- `Payments:BankName`
- `Payments:AccountHolder`
- `Payments:CardNumber`
- `Payments:Iban`

The repository's appsettings values are intentionally blank; do not commit real bank account details.

## Apply the database migration

After pulling this branch, run from the repository root:

```powershell
dotnet ef database update --project src\VaghtNegar.Infrastructure --startup-project src\VaghtNegar.API
dotnet build
```

Existing appointment records are not modified. The migration adds the `AppointmentPayments` table and its indexes.

## API endpoints

- Patient: `GET /api/v1/me/appointment-payments`
- Patient: `POST /api/v1/me/appointment-payments/checkout`
- Patient: `POST /api/v1/me/appointment-payments/{id}/receipt`
- Staff: `GET /api/v1/appointment-payments/pending`
- Staff: `POST /api/v1/appointment-payments/{id}/approve`
- Staff: `POST /api/v1/appointment-payments/{id}/reject`

The current implementation is a manual bank-transfer/receipt workflow, not an online payment gateway. Receipt images are stored as Base64 in SQL Server for this initial implementation; production deployment should move them to private object/file storage and retain only a protected storage key in the database.
