# Development Plan

## Foundation
- [x] .NET 10 solution and Clean Architecture project boundaries
- [x] Initial clinic, doctor, patient and appointment domain entities
- [x] SQL Server EF Core DbContext and initial migration
- [x] Clinic application service, repository and REST endpoints
- [x] API bootstrap, OpenAPI document and health endpoints

## Next milestones
1. Add clinic branches, specialties and doctor management APIs.
2. Model weekly schedules, holidays and one-off availability exceptions.
3. Implement appointment slot discovery and atomic booking with database-level protection against overlapping bookings.
4. Add patient registration and authentication/authorization.
5. Add payment intents and separate online/on-site payment workflows.
6. Add notifications, audit trail, integration tests and deployment pipeline.

## Implementation rules
- Store appointment instants in UTC; retain each clinic's IANA time-zone identifier for local schedule interpretation.
- Never trust a client-provided availability result at booking time. Revalidate and reserve the slot inside a transaction.
- Keep payment state separate from appointment state.
- Do not commit credentials, production connection strings, or payment secrets.
