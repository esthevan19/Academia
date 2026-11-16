# GymFlow Mobile API

The mobile API is hosted by the existing MVC application under `/api`. All protected routes require:

`Authorization: Bearer <JWT>`

The JWT is issued by `POST /api/auth/login` after validating the existing ASP.NET Identity account.

## Authentication

### POST /api/auth/login

Request:

```json
{
  "email": "account@example.com",
  "password": "provided-by-user"
}
```

A successful response contains `success`, `token`, `expiresAt`, and `user` (id, email, userName, fullName, roles).

Required hosting configuration (environment variables; never commit values):

- `Jwt__Key`: at least 32 UTF-8 bytes
- `Jwt__Issuer`: normally `GymFlow`
- `Jwt__Audience`: normally `GymFlow.Mobile`

## Read and management endpoints

| Method | Route | Access |
|---|---|---|
| GET | `/api/admins` | SuperAdmin |
| POST | `/api/admins` | SuperAdmin; cria conta com perfil Admin |
| PUT | `/api/admins` | SuperAdmin |
| DELETE | `/api/admins/{id}` | SuperAdmin |
| POST | `/api/admins/{id}/reset-password` | SuperAdmin |
| GET | `/api/admins/{id}/roles` | SuperAdmin |
| POST | `/api/admins/{id}/roles` | SuperAdmin |
| DELETE | `/api/admins/{id}/roles/{role}` | SuperAdmin |
| GET | `/api/members` | SuperAdmin, Admin, Gerente, Recepcionista |
| GET | `/api/members/{id}` | SuperAdmin, Admin, Gerente, Recepcionista |
| POST | `/api/members` | SuperAdmin, Admin, Gerente, Recepcionista; multipart form, existing model requires photo and health record |
| PUT | `/api/members/{id}` | SuperAdmin, Admin, Gerente, Recepcionista |
| DELETE | `/api/members/{id}` | SuperAdmin, Admin, Gerente |
| GET | `/api/trainers` | SuperAdmin, Admin, Gerente, Instrutor |
| GET | `/api/trainers/{id}` | SuperAdmin, Admin, Gerente, Instrutor |
| POST/PUT/DELETE | `/api/trainers` and `/api/trainers/{id}` | SuperAdmin, Admin, Gerente |
| GET | `/api/plans` and `/api/plans/{id}` | SuperAdmin, Admin, Gerente |
| POST | `/api/plans` | SuperAdmin, Admin, Gerente |
| PUT | `/api/plans/{id}` | SuperAdmin, Admin, Gerente |
| POST | `/api/plans/{id}/toggle-status` | SuperAdmin, Admin, Gerente |
| GET | `/api/sessions` and `/api/sessions/{id}` | All six GymFlow roles |
| GET | `/api/sessions/lookups` | SuperAdmin, Admin, Gerente, Recepcionista |
| POST/PUT | `/api/sessions` and `/api/sessions/{id}` | SuperAdmin, Admin, Gerente, Recepcionista |
| DELETE | `/api/sessions/{id}` | SuperAdmin, Admin, Gerente |
| GET | `/api/memberships` and `/api/memberships/lookups` | SuperAdmin, Admin, Gerente, Recepcionista |
| POST | `/api/memberships` | SuperAdmin, Admin, Gerente, Recepcionista |
| DELETE | `/api/memberships/{memberId}` | SuperAdmin, Admin, Gerente |
| GET | `/api/bookings/sessions/{sessionId}/members` | SuperAdmin, Admin, Gerente, Recepcionista, Instrutor |
| POST | `/api/bookings` | SuperAdmin, Admin, Gerente, Recepcionista |
| POST | `/api/bookings/attendance` | SuperAdmin, Admin, Gerente, Recepcionista, Instrutor |
| DELETE | `/api/bookings/{sessionId}/{memberId}` | SuperAdmin, Admin, Gerente, Recepcionista |

## Important limitations

- There is no verified Identity-user-to-Member relationship in the current data model. Do not implement a student `/me` endpoint by guessing a member record.
- The MVC project currently has no real workout/exercise persistence service. Do not return sample workouts as if they were saved data.
- Student self-booking and individual membership access must wait until the account-to-member relationship is implemented and verified.
- These routes must be tested on a staging or approved deployment before the mobile app is considered production-ready.
