# Postman API Documentation & Testing

This directory contains the Postman collection and environment files for testing all endpoints across the Sprintle microservices.

## Files

- [Sprintle.postman_collection.json](Sprintle.postman_collection.json): Full Postman Collection (v2.1.0) with all endpoints, request bodies, path variables, query parameters, authorization setup, and test scripts.
- [Sprintle.postman_environment.json](Sprintle.postman_environment.json): Pre-configured local environment with service base URLs and placeholder variables.

---

## Service Endpoints & Port Mapping

| Service | Protocol / Port (HTTPS) | Protocol / Port (HTTP) | Base Variable |
| :--- | :--- | :--- | :--- |
| **Users Microservice** | `https://localhost:7001` | `http://localhost:5001` | `{{usersBaseUrl}}` |
| **Workspaces Microservice** | `https://localhost:7002` | `http://localhost:5002` | `{{workspacesBaseUrl}}` / `{{tasksBaseUrl}}` |
| **API Gateway** | `https://localhost:6001` | `http://localhost:4001` | `{{gatewayBaseUrl}}` |

---

## Token & Variable Extraction Scripts

The authentication requests (`Login`, `Register`, and `Refresh Token`) include an automatic token capture test script:

```javascript
const json = pm.response.json();

if (!json.token && json.accessToken) {
    json.token = json.accessToken;
}

if (json.token) {
    pm.collectionVariables.set("access_token", json.token);
    if (pm.environment && pm.environment.name) {
        pm.environment.set("access_token", json.token);
    }
    console.log("Token saved:", json.token);
} else {
    console.error("Token NOT FOUND in response");
}
```

Similarly, `Create Task` and `Create Workspace` automatically persist `taskId` and `workspaceId` to both collection and active environment scopes upon success (`200 OK`).

### How It Works:
1. Upon successful authentication (200 OK), the script parses the JSON response.
2. It extracts the JWT token from `json.token` (or `json.accessToken`).
3. It saves the value into both the collection variable and active environment variable `access_token`.
4. All secured requests in the collection inherit `Bearer {{access_token}}` authentication automatically.

---

## Quick Start Guide

1. **Import Files into Postman**:
   - In Postman, click **Import** in the top-left corner.
   - Select both `Sprintle.postman_collection.json` and `Sprintle.postman_environment.json`.
   - Select the `Sprintle - Local Environment` in the environment dropdown (top right).

2. **Register or Log in**:
   - Open folder `01. Auth` &rarr; Send `Register` (or `Login`).
   - Check the Postman console (`Ctrl + Alt + C`): you should see `"Token saved: <jwt_token>"`.
   - The collection variable `{{access_token}}` is now populated.

3. **Interact with Endpoints**:
   - **Users**: View user list, inspect profiles, update user information.
   - **Roles**: (Requires user with `Admin` role) View system roles, assign or remove roles.
   - **Tasks**: Create tasks (the new `taskId` is automatically captured), assign tasks, log time, change statuses (`Todo`, `InProgress`, `InReview`, `Done`, `Cancelled`), or cancel tasks.
   - **Workspaces**: Create workspaces (new `workspaceId` is automatically captured), manage workspace details, list accessible workspaces, and manage workspace members.

---

## Endpoints Overview

### 1. Auth (`api/v1/auth`)
- `POST /api/v1/auth/register` &mdash; Register a new user account
- `POST /api/v1/auth/login` &mdash; Log in and acquire access/refresh tokens
- `POST /api/v1/auth/refresh` &mdash; Exchange refresh token cookie for a new access token
- `POST /api/v1/auth/revoke` &mdash; Invalidate the user's refresh token

### 2. Users (`api/v1/users`)
- `GET /api/v1/users` &mdash; Get paginated list of users (`page`, `pageSize`)
- `GET /api/v1/users/:id` &mdash; Get user profile by ID
- `PUT /api/v1/users/:id/profile` &mdash; Update user display name and username
- `PUT /api/v1/users/:id/password` &mdash; Change user password
- `DELETE /api/v1/users/:id` &mdash; Delete user account

### 3. Roles (`api/v1/roles` & `/api/v1/users/:userId/roles`)
- `GET /api/v1/roles` &mdash; List all available system roles
- `GET /api/v1/users/:userId/roles` &mdash; Get roles assigned to a user
- `POST /api/v1/users/:userId/roles/:role` &mdash; Assign role (`Admin`, `Manager`, `User`)
- `DELETE /api/v1/users/:userId/roles/:role` &mdash; Remove role

### 4. Tasks (`api/v1/tasks`)
- `GET /api/v1/tasks` &mdash; Get paginated list of all tasks
- `GET /api/v1/tasks/my` &mdash; Get user-specific tasks (query: `type=assigned|created`)
- `GET /api/v1/tasks/:id` &mdash; Get task details
- `POST /api/v1/tasks` &mdash; Create a task (saves `taskId` automatically)
- `PUT /api/v1/tasks/:id` &mdash; Update task details
- `PUT /api/v1/tasks/:id/assign/:userId` &mdash; Assign task to user
- `PATCH /api/v1/tasks/:id/status` &mdash; Update status (`Todo`, `InProgress`, `InReview`, `Done`, `Cancelled`)
- `POST /api/v1/tasks/:id/time-logs` &mdash; Log spent time on a task
- `POST /api/v1/tasks/:id/complete` &mdash; Mark task as completed
- `DELETE /api/v1/tasks/:id` &mdash; Cancel a task

### 5. Workspaces (`api/v1/workspaces`)
- `GET /api/v1/workspaces` &mdash; Get paginated list of workspaces (query: `page`, `pageSize`, `name`)
- `GET /api/v1/workspaces/:id` &mdash; Get workspace details (Guarded: Owner or Admin)
- `POST /api/v1/workspaces` &mdash; Create a new workspace (saves `workspaceId` automatically)
- `PUT /api/v1/workspaces/:id` &mdash; Update workspace name and description (Guarded: Owner or Admin)
- `DELETE /api/v1/workspaces/:id` &mdash; Delete workspace and associated members (Guarded: Owner or Admin)
- `POST /api/v1/workspaces/:id/members` &mdash; Add a user as workspace member (Guarded: Owner or Admin)
- `DELETE /api/v1/workspaces/:id/members/:userId` &mdash; Remove member from workspace (Guarded: Owner, Admin, or self-leaving)
