ALTER TABLE Users DROP CONSTRAINT CK__Users__Role__398D8EEE

-- 2️⃣ Creates new constraint sa Kitchen
ALTER TABLE Users
    ADD CONSTRAINT CK_Users_Role
        CHECK (Role IN ('Admin', 'Waiter', 'Bartender', 'Kitchen'))