Here's the improved `README.md` file, incorporating the new content while maintaining the existing structure and coherence:

# LifeLink_V2 API Documentation

This document lists the main HTTP endpoints provided by the LifeLink_V2 project and example JSON responses. Use the examples as a starting point — many endpoints accept additional query parameters or DTO bodies.

> Base URL: `/api`

---

## Authentication (`AuthController`)

- `POST /api/auth/register/patient`
  - Register a new patient.
  - Request body: `RegisterPatientDto` (email, full name, phone, password, etc.)
  - Example successful response:
  {
    "success": true,
    "message": "تم تسجيل المريض بنجاح",
    "token": "<jwt-token>",
    "tokenExpiry": "2026-04-10T12:00:00Z",
    "refreshToken": "<refresh-token>",
    "refreshTokenExpiry": "2026-05-03T12:00:00Z",
    "user": {
      "userId": 123,
      "fullName": "Ahmed Ali",
      "email": "ahmed@example.com",
      "role": "Patient",
      "patientId": 45
    }
  }

- `POST /api/auth/register/provider`
- Register a provider (clinic/hospital/etc.). Provider accounts may require admin approval.
- Exampleresponse is similar to patient registration but `IsActive` may be `false` and message indicates pending approval.

- `POST /api/auth/login`
  - Body: `LoginDto` (`email`, `password`, `rememberMe`).
  - Success example:
  {
    "success": true,
    "message": "تم تسجيل الدخول بنجاح",
    "token": "<jwt-token>",
    "tokenExpiry": "2026-04-10T12:00:00Z",
    "refreshToken": "<refresh-token>",
    "refreshTokenExpiry": "2026-05-03T12:00:00Z",
    "user": { "userId": 123, "fullName": "Ahmed Ali", "email": "ahmed@example.com", "role": "Patient" }
  }

- `POST /api/auth/refresh-token`
- Body: `{ token: "<expired-or-expiring-jwt>", refreshToken: "<refresh-token>" }`
- Success example returns a new JWT and refresh token.

- `POST /api/auth/forgot-password`
- Body: `{ email: "user@example.com" }`
- Response indicates whether a reset email was sent (alwaysgeneric for security).

- `POST /api/auth/reset-password`
  - Body: `{ token: "<reset-token>", newPassword: "...", confirmPassword: "..." }`
  - Success example:
  { "success": true, "message": "تم تغيير كلمة المرور بنجاح" }

- `POST /api/auth/verify-email`
  - Body: `{ email: "user@example.com", code: "123456" }`
  - Confirms verification code.

- `POST /api/auth/change-password` (Authorized)
  - Body: `{ currentPassword, newPassword, confirmPassword }`

- `GET /api/auth/profile` (Authorized)
  - Returns basic claims/profile extracted from token.
  - Example:
  {
    "success": true,
    "message": "تم جلب معلومات الملف الشخصي بنجاح",
    "data": {
      "userId": 123,
      "fullName": "Ahmed Ali",
      "email": "ahmed@example.com",
      "role": "Patient"
    }
  }

---

## Appointments (`AppointmentsController`)

- `GET /api/appointments`
- Query: `patientId`, `providerId`, `statusId`, `dateFrom`, `dateTo`, `page`, `pageSize`.
- Example response (paginated):
  {
    "success": true,
    "message": "",
    "data": {
      "appointments": [
        {
          "appointmentId": 10,
          "appointmentCode": "APT-20260403-ABC123",
          "patient": { "patientId": 45, "fullName": "Ahmed Ali" },
          "provider": { "providerId": 5, "providerName": "City Clinic" },
          "doctor": null,
          "scheduledAt": "2026-04-10T09:00:00Z",
          "durationMinutes": 30,
          "status": "Pending"
        }
      ],
      "pagination": { "page": 1, "pageSize": 20, "totalCount": 1, "totalPages": 1 }
    }
  }

- `GET /api/appointments/{id}`
- Returns full appointment details.

- `POST /api/appointments` / `POST /api/appointments/request`
- Create a new appointment. Body: `CreateAppointmentDto`.
  - Example response:
  { "success": true, "message": "تم حجز الموعد بنجاح", "data": { "appointmentId": 10, "appointmentCode": "APT-...", "scheduledAt": "2026-04-10T09:00:00Z", "status": "Pending" } }

- `PUT /api/appointments/{id}`
- Update appointment details (reschedule, notes).

- `POST /api/appointments/{id}/accept` (Provider role)
- Provider accepts a pending appointment; performs conflict checks.

- `POST /api/appointments/{id}/reject` (Provider role)
- Provider rejects a pending appointment; optional reason.

- `POST /api/appointments/{id}/reschedule` (Provider role)
- Provider reschedules appointment; body: `{ newScheduledAt, durationMinutes}`.

- `POST /api/appointments/{id}/cancel`
- Cancel an appointment (checks cancellation window).

- `POST /api/appointments/{id}/complete` (Provider/Admin)
- Mark appointment completed.

- `POST /api/appointments/{id}/medfiles/{medFileId}` (Provider/Admin)
- Attach a medical file to an appointment.

- `GET /api/appointments/availability/{providerId}?doctorId=&date=`
- Provider-level availability (default working hours).

---

## Doctors / Provider (`ProviderController`, `DoctorsController`)

- `POST /api/provider` (Admin)
- Create provider.

- `GET /api/provider/{id}`
- Provider details.

- `GET /api/provider/{providerId}/doctors`
  - List doctors for a provider.

- `POST /api/provider/{providerId}/doctors` (Provider/Admin)
  - Add a doctor to a provider.

- `PUT /api/provider/doctors/{doctorId}` (Provider/Admin)
  - Update doctor.

- `DELETE /api/provider/doctors/{doctorId}` (Provider/Admin)
  - Remove doctor.

- `GET /api/doctors/{id}/availability?date=YYYY-MM-DD`
  - Returns generated slots for the doctor on the requested date using `DoctorAvailability` rules. Example response:
  {
    "success": true,
    "message": "",
    "data": {
      "doctorId": 12,
      "doctorName": "Dr. Samir",
      "date": "2026-04-10",
      "slots": [
        { "start": "2026-04-10T09:00:00Z", "end": "2026-04-10T09:30:00Z", "isAvailable": true },
        { "start": "2026-04-10T09:30:00Z", "end": "2026-04-10T10:00:00Z", "isAvailable": false }
      ]
    }
  }

- `POST /api/doctors/{id}/availability` (Provider/Admin)
- Create a DoctorAvailability entry (dayOfWeek, startTime, endTime, slotDuration).

- `PUT /api/doctors/{id}/availability/{availabilityId}` (Provider/Admin)
  - Update availability entry.

- `DELETE /api/doctors/{id}/availability/{availabilityId}` (Provider/Admin)
  - Delete availability entry.

---

## Patients (`PatientController`)

Common patient endpoints include creating and retrieving patient profiles, listing patient appointments and medical records. Responses follow the `ApiResponse` wrapper:
{ "success": true, "message": "...", "data": { /* payload */ } }

---

## Pharmacy & Laboratory

Controllers `PharmacyController` and `LaboratoryController` expose endpoints to list orders, create orders, view order details and revenue reports. Example snippet for a completed pharmacy order list:
{
  "success": true,
  "message": "",
  "data": [ { "orderId": 100, "status": "Completed", "totalSyp": 15000 } ]
}

---

## Admin Analytics (`AdminAnalyticsController`)

Provides many analytics endpoints (platform overview, users analytics, revenue analytics, top providers, reports export). Typical response shape:
{ "success": true, "message": "", "data": { /* analytics object */ } }

---

## Common response wrapper

Most endpoints return `ApiResponse` (JSON):
{
  "success": true|false,
  "message": "string",
  "data": { /* optional */ },
  "errors": [ "optional error messages" ]
}

---

## Notes
- Authentication uses JWT and refresh tokens. Send the JWT in `Authorization: Bearer <token>` header for protected endpoints.
- Time fields are UTC in examples; adapt your client timezone accordingly.
- For any endpoint that modifies data, check the controller attributes to see required roles (`Authorize(Roles = "Provider")`, etc.).

Errors (validation or business) return `success: false` and `errors` array with messages.

---

If you want, I can:
- Generate a complete OpenAPI/Swagger spec from controllers.
- Produce example request bodies (DTO schemas) for each endpoint.
- Export this README to `README.md` in the repo.

This revised README maintains the original structure while integrating the new content seamlessly, ensuring clarity and coherence throughout the document.