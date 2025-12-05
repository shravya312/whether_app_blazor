# Fix: Table Already Exists Error

If you see the error: `relation "favorite_cities" already exists`, use one of these solutions:

## Solution 1: Drop and Recreate (If table is empty or test data)

Run this SQL to remove the existing table and create it fresh:

```sql
-- Drop the table if it exists (WARNING: This deletes all data!)
DROP TABLE IF EXISTS favorite_cities CASCADE;

-- Now create the table
CREATE TABLE favorite_cities (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES auth.users(id),
  city TEXT NOT NULL,
  country TEXT NOT NULL,
  added_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  UNIQUE(user_id, city, country)
);

CREATE INDEX idx_favorite_cities_user_id ON favorite_cities(user_id);

-- Enable Row Level Security
ALTER TABLE favorite_cities ENABLE ROW LEVEL SECURITY;

-- Create security policies
CREATE POLICY "Users can view their own favorite cities"
  ON favorite_cities FOR SELECT
  USING (auth.uid() = user_id);

CREATE POLICY "Users can insert their own favorite cities"
  ON favorite_cities FOR INSERT
  WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can delete their own favorite cities"
  ON favorite_cities FOR DELETE
  USING (auth.uid() = user_id);

CREATE POLICY "Users can update their own favorite cities"
  ON favorite_cities FOR UPDATE
  USING (auth.uid() = user_id);
```

## Solution 2: Skip CREATE TABLE (If table already exists correctly)

If the table structure is already correct, just run the missing parts:

```sql
-- Create index if it doesn't exist
CREATE INDEX IF NOT EXISTS idx_favorite_cities_user_id ON favorite_cities(user_id);

-- Enable Row Level Security (safe to run multiple times)
ALTER TABLE favorite_cities ENABLE ROW LEVEL SECURITY;

-- Drop existing policies if they exist, then recreate
DROP POLICY IF EXISTS "Users can view their own favorite cities" ON favorite_cities;
DROP POLICY IF EXISTS "Users can insert their own favorite cities" ON favorite_cities;
DROP POLICY IF EXISTS "Users can delete their own favorite cities" ON favorite_cities;
DROP POLICY IF EXISTS "Users can update their own favorite cities" ON favorite_cities;

-- Create security policies
CREATE POLICY "Users can view their own favorite cities"
  ON favorite_cities FOR SELECT
  USING (auth.uid() = user_id);

CREATE POLICY "Users can insert their own favorite cities"
  ON favorite_cities FOR INSERT
  WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can delete their own favorite cities"
  ON favorite_cities FOR DELETE
  USING (auth.uid() = user_id);

CREATE POLICY "Users can update their own favorite cities"
  ON favorite_cities FOR UPDATE
  USING (auth.uid() = user_id);
```

## Solution 3: Check Table Structure First

Verify if your existing table has the correct structure:

```sql
-- Check table structure
SELECT 
    column_name, 
    data_type, 
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name = 'favorite_cities'
ORDER BY ordinal_position;
```

If the structure looks correct, use Solution 2. If not, use Solution 1.

## Recommended: Use Solution 2

Since you already ran it once, **Solution 2 is recommended** - it will:
- ✅ Keep any existing data
- ✅ Add missing indexes
- ✅ Set up security policies
- ✅ Not cause errors if things already exist

Just copy and run Solution 2's SQL code!

