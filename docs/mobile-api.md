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
| GET | `/api/admins` | SuperAdmin; lista todas as contas Identity |
| POST | `/api/admins` | SuperAdmin; cria conta com perfil Admin |
| PUT | `/api/admins` | SuperAdmin |
| DELETE | `/api/admins/{id}` | SuperAdmin |
| POST | `/api/admins/{id}/reset-password` | SuperAdmin |
| GET | `/api/admins/{id}/roles` | SuperAdmin |
| POST | `/api/admins/{id}/roles` | SuperAdmin |
| DELETE | `/api/admins/{id}/roles/{role}` | SuperAdmin |
| GET/POST/DELETE | `/api/admins/{id}/member-link` | SuperAdmin; vincula uma conta Aluno a um cadastro existente usando claim Identity, sem migração de banco |
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
| GET | `/api/me` | Aluno; somente o cadastro vinculado à conta autenticada |
| GET | `/api/me/bookings` | Aluno; somente seus próprios agendamentos |
| POST | `/api/me/bookings` | Aluno; agenda a própria conta vinculada |
| DELETE | `/api/me/bookings/{sessionId}` | Aluno; cancela somente o próprio agendamento antes do início |
| GET | `/api/bookings/sessions/{sessionId}/members` | SuperAdmin, Admin, Gerente, Recepcionista, Instrutor |
| POST | `/api/bookings` | SuperAdmin, Admin, Gerente, Recepcionista |
| POST | `/api/bookings/attendance` | SuperAdmin, Admin, Gerente, Recepcionista, Instrutor |
| DELETE | `/api/bookings/{sessionId}/{memberId}` | SuperAdmin, Admin, Gerente, Recepcionista |

## Important limitations

- The existing database has no direct Identity-user-to-Member foreign key. A SuperAdmin can explicitly link an Aluno account to one existing member through the `gymflow:member_id` Identity claim stored in the existing AspNetUserClaims table. A member can be linked to only one account. The student must sign in again after the link is changed so the new claim is included in the JWT.
- The MVC project currently has no real workout/exercise persistence service. Do not return sample workouts as if they were saved data.
- Student profile, membership and booking endpoints use only the member ID from the authenticated JWT claim; they never accept a member ID supplied by the student.
- These routes must be tested on a staging or approved deployment before the mobile app is considered production-ready.
