-- Phobia Relief Therapy one-time cloud setup.
-- Creates tables, demo accounts, and login/register RPCs.
-- App auth does not use Supabase Email provider / confirm-email settings.

create extension if not exists "pgcrypto";

create table if not exists public.users (
  id uuid primary key default gen_random_uuid(),
  username text not null default '',
  email text not null unique,
  password_hash text not null default '',
  created_at timestamptz not null default now(),
  is_admin boolean not null default false,
  account_status text not null default 'active'
);

alter table public.users add column if not exists username text not null default '';
alter table public.users add column if not exists email text;
alter table public.users add column if not exists password_hash text not null default '';
alter table public.users add column if not exists created_at timestamptz not null default now();
alter table public.users add column if not exists is_admin boolean not null default false;
alter table public.users add column if not exists account_status text not null default 'active';

create unique index if not exists idx_users_email_unique on public.users (lower(email));

create table if not exists public.therapy_sessions (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null,
  phobia text not null,
  difficulty text not null,
  stage integer not null default 1,
  baseline_heart_rate integer not null default 0,
  final_heart_rate integer not null default 0,
  peak_heart_rate integer not null default 0,
  average_heart_rate double precision not null default 0,
  exposure_time double precision not null default 0,
  panic_event_count integer not null default 0,
  safety_trigger_count integer not null default 0,
  average_panic_score double precision not null default 0,
  highest_panic_score double precision not null default 0,
  feedback text,
  date_completed timestamptz not null default now()
);

alter table public.therapy_sessions add column if not exists peak_heart_rate integer not null default 0;
alter table public.therapy_sessions add column if not exists average_heart_rate double precision not null default 0;
alter table public.therapy_sessions add column if not exists panic_event_count integer not null default 0;
alter table public.therapy_sessions add column if not exists safety_trigger_count integer not null default 0;
alter table public.therapy_sessions add column if not exists average_panic_score double precision not null default 0;
alter table public.therapy_sessions add column if not exists highest_panic_score double precision not null default 0;

create table if not exists public.admin_audit_log (
  id uuid primary key default gen_random_uuid(),
  admin_user_id uuid,
  action text not null,
  target_user_id uuid,
  details text,
  created_at timestamptz not null default now()
);

do $$
declare r record;
begin
  for r in
    select c.conname, c.conrelid::regclass as tbl
    from pg_constraint c
    where c.contype = 'f'
      and c.confrelid = 'auth.users'::regclass
      and c.conrelid in ('public.users'::regclass, 'public.therapy_sessions'::regclass, 'public.admin_audit_log'::regclass)
  loop
    execute format('alter table %s drop constraint if exists %I', r.tbl, r.conname);
  end loop;
exception
  when undefined_table then
    null;
end $$;

create index if not exists idx_therapy_sessions_date_completed
  on public.therapy_sessions (date_completed desc);

create index if not exists idx_therapy_sessions_user_id
  on public.therapy_sessions (user_id);

create index if not exists idx_therapy_sessions_phobia
  on public.therapy_sessions (phobia);

create index if not exists idx_users_account_status
  on public.users (account_status);

insert into public.users (id, username, email, password_hash, is_admin, account_status, created_at)
values
  (
    '11111111-1111-1111-1111-111111111111',
    'admin',
    'admin@phobia.local',
    '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9',
    true,
    'active',
    '2026-01-01 00:00:00+00'
  ),
  (
    '22222222-2222-2222-2222-222222222222',
    'demo_user',
    'user@phobia.local',
    '831c237928e6212bedaa4451a514ace3174562f6761f6a157a2fe5082b36e2fb',
    false,
    'active',
    '2026-01-01 00:00:00+00'
  )
on conflict (email) do update
  set username = excluded.username,
      password_hash = excluded.password_hash,
      is_admin = excluded.is_admin,
      account_status = 'active';

create or replace function public.prt_health()
returns jsonb
language sql
stable
as $$
  select jsonb_build_object('ok', true, 'users', (select count(*) from public.users));
$$;

create or replace function public.prt_login(p_email text, p_password text)
returns jsonb
language plpgsql
security definer
set search_path = public
as $$
declare
  r public.users%rowtype;
  h text;
begin
  h := encode(digest(coalesce(p_password, ''), 'sha256'), 'hex');
  select * into r
  from public.users
  where lower(email) = lower(trim(p_email))
    and password_hash = h;

  if not found then
    raise exception 'Invalid email or password';
  end if;

  if r.account_status = 'suspended' then
    raise exception 'This account is suspended. Contact an administrator.';
  end if;

  return jsonb_build_object(
    'id', r.id,
    'username', r.username,
    'email', r.email,
    'is_admin', r.is_admin,
    'account_status', r.account_status,
    'created_at', r.created_at
  );
end;
$$;

create or replace function public.prt_register(p_username text, p_email text, p_password text)
returns jsonb
language plpgsql
security definer
set search_path = public
as $$
declare
  r public.users%rowtype;
  h text;
begin
  if p_username is null or length(trim(p_username)) < 3 then
    raise exception 'Name must be 3-30 characters.';
  end if;
  if p_email is null or position('@' in p_email) = 0 then
    raise exception 'Enter a valid email address.';
  end if;
  if p_password is null or length(p_password) < 8 then
    raise exception 'Password must be at least 8 characters and include both letters and numbers.';
  end if;
  if exists (select 1 from public.users where lower(email) = lower(trim(p_email))) then
    raise exception 'Email already registered.';
  end if;

  h := encode(digest(p_password, 'sha256'), 'hex');
  insert into public.users (username, email, password_hash, is_admin, account_status)
  values (trim(p_username), trim(p_email), h, false, 'active')
  returning * into r;

  return jsonb_build_object(
    'id', r.id,
    'username', r.username,
    'email', r.email,
    'is_admin', r.is_admin,
    'account_status', r.account_status,
    'created_at', r.created_at
  );
end;
$$;

create or replace function public.prt_set_password(p_user_id uuid, p_new_password text)
returns jsonb
language plpgsql
security definer
set search_path = public
as $$
begin
  if p_new_password is null or length(p_new_password) < 8 then
    raise exception 'Password must be at least 8 characters.';
  end if;
  update public.users
     set password_hash = encode(digest(p_new_password, 'sha256'), 'hex')
   where id = p_user_id;
  if not found then
    raise exception 'Account not found.';
  end if;
  return jsonb_build_object('ok', true);
end;
$$;

create or replace function public.prt_lookup_username(p_email text)
returns jsonb
language plpgsql
security definer
set search_path = public
as $$
declare
  n text;
begin
  select username into n from public.users where lower(email) = lower(trim(p_email));
  if n is null then
    raise exception 'No account found for that email address.';
  end if;
  return jsonb_build_object('username', n);
end;
$$;

grant execute on function public.prt_health() to anon, authenticated;
grant execute on function public.prt_login(text, text) to anon, authenticated;
grant execute on function public.prt_register(text, text, text) to anon, authenticated;
grant execute on function public.prt_set_password(uuid, text) to anon, authenticated;
grant execute on function public.prt_lookup_username(text) to anon, authenticated;

alter table public.users enable row level security;
alter table public.therapy_sessions enable row level security;
alter table public.admin_audit_log enable row level security;

drop policy if exists users_select_all on public.users;
drop policy if exists users_insert_all on public.users;
drop policy if exists users_update_all on public.users;
drop policy if exists users_delete_admin on public.users;
drop policy if exists sessions_select_all on public.therapy_sessions;
drop policy if exists sessions_insert_all on public.therapy_sessions;
drop policy if exists audit_select_all on public.admin_audit_log;
drop policy if exists audit_insert_all on public.admin_audit_log;

create policy users_select_all on public.users for select using (true);
create policy users_insert_all on public.users for insert with check (true);
create policy users_update_all on public.users for update using (true) with check (true);
create policy users_delete_admin on public.users for delete using (true);
create policy sessions_select_all on public.therapy_sessions for select using (true);
create policy sessions_insert_all on public.therapy_sessions for insert with check (true);
create policy audit_select_all on public.admin_audit_log for select using (true);
create policy audit_insert_all on public.admin_audit_log for insert with check (true);

grant usage on schema public to anon, authenticated;
grant select, insert, update, delete on public.users to anon, authenticated;
grant select, insert, update, delete on public.therapy_sessions to anon, authenticated;
grant select, insert on public.admin_audit_log to anon, authenticated;

notify pgrst, 'reload schema';
