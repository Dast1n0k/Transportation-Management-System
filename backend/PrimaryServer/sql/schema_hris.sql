CREATE TABLE users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    username TEXT UNIQUE NOT NULL,
    password TEXT NOT NULL,
    role TEXT NOT NULL CHECK (role IN ('admin', 'manager', 'courier')),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TRIGGER users_updated_at_trigger
BEFORE UPDATE ON users 
FOR EACH ROW 
BEGIN 
    UPDATE users 
    SET updated_at = CURRENT_TIMESTAMP 
    WHERE id = NEW.id;
END;

CREATE INDEX idx_users_username ON users (username);

CREATE TABLE admin_profiles (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL UNIQUE,
    name TEXT,
    surname TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TRIGGER admin_profiles_updated_at_trigger
BEFORE UPDATE ON admin_profiles 
FOR EACH ROW 
BEGIN 
    UPDATE admin_profiles 
    SET updated_at = CURRENT_TIMESTAMP 
    WHERE id = NEW.id;
END;

CREATE INDEX idx_admin_user_id ON admin_profiles (user_id);

CREATE TABLE manager_profiles (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL UNIQUE,
    name TEXT,
    surname TEXT,
    notes TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TRIGGER manager_profiles_updated_at_trigger
BEFORE UPDATE ON manager_profiles 
FOR EACH ROW 
BEGIN 
    UPDATE manager_profiles 
    SET updated_at = CURRENT_TIMESTAMP 
    WHERE id = NEW.id;
END;

CREATE INDEX idx_manager_user_id ON manager_profiles (user_id);

CREATE TABLE courier_profiles (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL UNIQUE,
    name TEXT,
    surname TEXT,
    phone TEXT UNIQUE NOT NULL CHECK (
        phone GLOB '+[0-9]*'
    ),
    dimensions TEXT NOT NULL CHECK (
        dimensions GLOB '[0-9]*\*[0-9]*\*[0-9]*'
    ),
    vehicle_type TEXT NOT NULL CHECK (
        vehicle_type IN ('sprinter', 'straight_small', 'straight_large')
    ),
    zipcode TEXT NOT NULL,
    latitude REAL NOT NULL,
    longitude REAL NOT NULL,
    capacity INTEGER NOT NULL,
    available_since TIMESTAMP,
    is_available BOOLEAN DEFAULT 0,
    notes TEXT,
    location TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TRIGGER courier_profiles_updated_at_trigger
BEFORE UPDATE ON courier_profiles 
FOR EACH ROW 
BEGIN 
    UPDATE courier_profiles 
    SET updated_at = CURRENT_TIMESTAMP 
    WHERE id = NEW.id;
END;

CREATE TRIGGER courier_available_since_trigger
BEFORE UPDATE ON courier_profiles 
FOR EACH ROW 
WHEN NEW.is_available = 1 AND OLD.is_available = 0 
BEGIN 
    UPDATE courier_profiles 
    SET available_since = CURRENT_TIMESTAMP 
    WHERE id = NEW.id;
END;

CREATE INDEX idx_courier_user_id ON courier_profiles (user_id);
CREATE INDEX idx_courier_zipcode ON courier_profiles (zipcode);
CREATE INDEX idx_courier_availability ON courier_profiles (is_available);
