# Development Plan

## Foundation
- [x] .NET 10 solution and Clean Architecture project boundaries
- [x] Initial clinic, doctor, patient and appointment domain entities
- [x] SQL Server EF Core DbContext and initial indexes
- [x] API bootstrap, OpenAPI document and liveness/readiness endpoints

## Next milestones
1. Add EF Core migrations and a repeatable local database setup.
2. Add clinic, specialty and doctor management APIs.
3. Model weekly schedules, holidays and one-off availability exceptions.
4. Implement appointment slot discovery and atomic booking with database-level protection against overlapping bookings.
5. Add patient registration and authentication/authorization.
6. Add payment intents and separate online/on-site payment workflows.
7. Add notifications, audit trail, integration tests and deployment pipeline.

## Implementation rules
- Store appointment instants in UTC; retain each clinic's IANA time-zone identifier for local schedule interpretation.
- Never trust a client-provided availability result at booking time. Revalidate and reserve the slot inside a transaction.
- Keep payment state separate from appointment state.
- Do not commit credentials, production connection strings, or payment secrets.
