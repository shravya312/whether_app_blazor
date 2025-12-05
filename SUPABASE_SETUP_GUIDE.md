# Supabase Table Setup Guide - Favorite Cities

This guide will walk you through creating the `favorite_cities` table in your Supabase project.

## Prerequisites

- A Supabase account (sign up at https://supabase.com if you don't have one)
- Access to your Supabase project dashboard

## Step-by-Step Instructions

### Step 1: Access Your Supabase Project

1. Go to https://supabase.com and sign in to your account
2. You'll see a list of your projects. Click on your project (or create a new one if needed)
3. You'll be taken to your project dashboard

### Step 2: Navigate to SQL Editor

1. In the left sidebar, look for the **"SQL Editor"** option
2. Click on **"SQL Editor"** (it has a database icon)
3. You should see a SQL editor interface with a query input area

### Step 3: Create the Table

1. In the SQL Editor, you'll see a text area where you can write SQL queries
2. Copy and paste the following SQL code:

```sql
CREATE TABLE favorite_cities (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES auth.users(id),
  city TEXT NOT NULL,
  country TEXT NOT NULL,
  added_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  UNIQUE(user_id, city, country)
);

CREATE INDEX idx_favorite_cities_user_id ON favorite_cities(user_id);
```

3. Make sure the entire SQL code is selected in the editor

### Step 4: Run the SQL Query

1. Look for a **"Run"** button (usually at the bottom right of the SQL editor, or press `Ctrl+Enter` / `Cmd+Enter`)
2. Click the **"Run"** button or press the keyboard shortcut
3. Wait for the query to execute (should take a few seconds)

### Step 5: Verify Table Creation

1. After running the query, you should see a success message like "Success. No rows returned"
2. To verify the table was created:
   - Go to the **"Table Editor"** in the left sidebar
   - Look for **"favorite_cities"** in the list of tables
   - Click on it to see the table structure

### Step 6: Verify Table Structure (Optional)

In the Table Editor, you should see the following columns:
- `id` (uuid, primary key)
- `user_id` (uuid, not null, foreign key to auth.users)
- `city` (text, not null)
- `country` (text, not null)
- `added_at` (timestamp with time zone)

### Step 7: Set Up Row Level Security (RLS) - IMPORTANT

For security, you need to enable Row Level Security so users can only access their own favorite cities:

1. Go back to **"SQL Editor"**
2. Run the following SQL commands:

```sql
-- Enable Row Level Security
ALTER TABLE favorite_cities ENABLE ROW LEVEL SECURITY;

-- Create policy: Users can only see their own favorite cities
CREATE POLICY "Users can view their own favorite cities"
  ON favorite_cities
  FOR SELECT
  USING (auth.uid() = user_id);

-- Create policy: Users can insert their own favorite cities
CREATE POLICY "Users can insert their own favorite cities"
  ON favorite_cities
  FOR INSERT
  WITH CHECK (auth.uid() = user_id);

-- Create policy: Users can delete their own favorite cities
CREATE POLICY "Users can delete their own favorite cities"
  ON favorite_cities
  FOR DELETE
  USING (auth.uid() = user_id);

-- Create policy: Users can update their own favorite cities
CREATE POLICY "Users can update their own favorite cities"
  ON favorite_cities
  FOR UPDATE
  USING (auth.uid() = user_id);
```

3. Click **"Run"** to execute these policies

## Alternative Method: Using Table Editor (GUI)

If you prefer using the graphical interface:

### Step 1: Navigate to Table Editor

1. Click on **"Table Editor"** in the left sidebar
2. Click the **"New Table"** button (usually at the top)

### Step 2: Create Table

1. **Table Name**: Enter `favorite_cities`
2. Click **"Create Table"**

### Step 3: Add Columns

Add each column one by one:

1. **Column 1 - id**:
   - Name: `id`
   - Type: `uuid`
   - Check: `Is Primary Key`
   - Default Value: `gen_random_uuid()`

2. **Column 2 - user_id**:
   - Name: `user_id`
   - Type: `uuid`
   - Check: `Is Nullable` (uncheck this)
   - Foreign Key: Click "Add Foreign Key"
     - Referenced Table: `auth.users`
     - Referenced Column: `id`

3. **Column 3 - city**:
   - Name: `city`
   - Type: `text`
   - Check: `Is Nullable` (uncheck this)

4. **Column 4 - country**:
   - Name: `country`
   - Type: `text`
   - Check: `Is Nullable` (uncheck this)

5. **Column 5 - added_at**:
   - Name: `added_at`
   - Type: `timestamptz` (timestamp with time zone)
   - Default Value: `now()`

### Step 4: Add Unique Constraint

1. After creating all columns, click on the table settings
2. Go to **"Constraints"** tab
3. Add a **Unique Constraint**:
   - Columns: `user_id`, `city`, `country`
   - This ensures a user can't add the same city twice

### Step 5: Create Index

1. Go to **"SQL Editor"**
2. Run: `CREATE INDEX idx_favorite_cities_user_id ON favorite_cities(user_id);`

## Verification Checklist

After setup, verify:

- ✅ Table `favorite_cities` exists in Table Editor
- ✅ All columns are present with correct types
- ✅ Foreign key relationship to `auth.users` is set
- ✅ Unique constraint on (user_id, city, country) is set
- ✅ Index on `user_id` is created
- ✅ Row Level Security policies are enabled

## Testing the Setup

To test if everything works:

1. Run this query in SQL Editor (replace with a real user_id from your auth.users table):

```sql
-- Insert a test favorite city (replace USER_ID_HERE with actual user ID)
INSERT INTO favorite_cities (user_id, city, country)
VALUES ('USER_ID_HERE', 'London', 'GB');

-- Query to see if it was inserted
SELECT * FROM favorite_cities;
```

2. If you see the inserted row, the table is working correctly!

## Troubleshooting

### Error: "relation auth.users does not exist"
- This means you're using a different Supabase setup
- The `auth.users` table should exist by default in Supabase
- Try refreshing the page or checking if you're in the correct project

### Error: "permission denied"
- Make sure you're logged in as the project owner/admin
- Check that you have the correct permissions

### Foreign Key Constraint Error
- Ensure the `user_id` you're using exists in `auth.users` table
- You can check existing users in the Authentication > Users section

### RLS Policies Not Working
- Make sure Row Level Security is enabled: `ALTER TABLE favorite_cities ENABLE ROW LEVEL SECURITY;`
- Verify policies are created correctly
- Check that users are authenticated when accessing the table

## Next Steps

Once the table is set up:

1. ✅ Your Weather App can now save favorite cities
2. ✅ Users can add/remove favorites from the UI
3. ✅ Favorite cities will sync across devices for logged-in users
4. ✅ Data is secure with Row Level Security policies

## Need Help?

If you encounter any issues:
1. Check the Supabase documentation: https://supabase.com/docs
2. Verify your SQL syntax is correct
3. Make sure you're in the correct project
4. Check the Supabase logs for error messages

---

**Note**: The SQL Editor method is recommended as it's faster and ensures all constraints are set correctly.

