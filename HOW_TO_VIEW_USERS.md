# How to View Registered Users

## Method 1: Supabase Dashboard (View ALL Users) ⭐ Recommended

### Steps:
1. **Go to Supabase Dashboard**
   - Visit: https://supabase.com/dashboard
   - Sign in with your Supabase account

2. **Select Your Project**
   - Find your project: `wdzfgezvxydmmcyybnet`
   - Or look for the project with URL: `https://wdzfgezvxydmmcyybnet.supabase.co`

3. **Navigate to Authentication**
   - In the left sidebar, click on **"Authentication"**
   - Then click on **"Users"**

4. **View All Users**
   - You'll see a table with all registered users
   - Information displayed:
     - **Email** address
     - **User ID** (UUID)
     - **Created At** timestamp
     - **Last Sign In** timestamp
     - **Email Confirmed** status
     - **Phone** (if provided)
     - **Metadata** (custom user data)

5. **Additional Actions**
   - Click on any user to see detailed information
   - You can manually confirm emails
   - Reset passwords
   - Delete users
   - View user sessions

## Method 2: In-App Users Page (Current User Only)

### Access:
- **URL**: `https://localhost:7084/users`
- **Navigation**: Click "Users" in the sidebar menu

### What You'll See:
- Information about the currently logged-in user
- Link to Supabase Dashboard for viewing all users

### Limitations:
- Only shows the current logged-in user
- To see all users, use Method 1 (Supabase Dashboard)

## Method 3: Direct Database Query (Advanced)

If you have database access, you can query the `auth.users` table directly:

```sql
SELECT 
    id,
    email,
    created_at,
    last_sign_in_at,
    email_confirmed_at,
    phone,
    raw_user_meta_data
FROM auth.users
ORDER BY created_at DESC;
```

## Quick Access Links

- **Supabase Dashboard**: https://supabase.com/dashboard
- **Your Project URL**: https://wdzfgezvxydmmcyybnet.supabase.co
- **Direct Users Page**: https://supabase.com/dashboard/project/wdzfgezvxydmmcyybnet/auth/users

## Notes

- All user data is securely stored in Supabase's PostgreSQL database
- User passwords are hashed and never stored in plain text
- Email addresses must be unique (one account per email)
- Users can be managed directly from the Supabase Dashboard

