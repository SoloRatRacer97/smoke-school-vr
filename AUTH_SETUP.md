# Smoke School Auth Setup

Unity displays the login UI. Netlify Functions validate credentials server-side and set an HttpOnly session cookie.

## Required Netlify Environment Variables

- `DATABASE_URL`: Postgres connection string.
- `AUTH_SESSION_SECRET`: random string of at least 32 characters.
- `AUTH_COOKIE_SECURE`: optional. Set to `false` only for local HTTP testing.
- `AUTH_SESSION_DAYS`: optional session duration in days. Defaults to `30`.

## Deployment Note

The WebGL ZIP contains the static Unity build with `index.html` at the archive root. The auth endpoints require the `netlify/functions` source to be deployed by Netlify's build system, Netlify CLI, or Netlify API; a drag-and-drop static deploy alone does not deploy functions.

## Postgres Schema

```sql
create extension if not exists pgcrypto;

create table users (
  id uuid primary key default gen_random_uuid(),
  email text not null,
  display_name text,
  student_id text,
  password_hash text not null,
  active boolean not null default true,
  created_at timestamptz not null default now()
);

create unique index users_email_lower_idx on users (lower(email));

create table sessions (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null references users(id) on delete cascade,
  token_hash text not null unique,
  expires_at timestamptz not null,
  created_at timestamptz not null default now(),
  revoked_at timestamptz
);

create index sessions_lookup_idx on sessions (token_hash)
  where revoked_at is null;
```

## Create A Password Hash

```bash
npm install
npm run auth:hash -- "replace-with-password"
```

Use the output as `users.password_hash`.

```sql
insert into users (email, display_name, student_id, password_hash)
values (
  'student@example.com',
  'Student Name',
  'Student ID',
  'pbkdf2_sha256$...'
);
```
