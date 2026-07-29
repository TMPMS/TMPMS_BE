-- Drop tables if they exist (for clean setup)
DROP TABLE IF EXISTS supplier_medicines CASCADE;
DROP TABLE IF EXISTS inventory_transactions CASCADE;
DROP TABLE IF EXISTS inventory_stocks CASCADE;
DROP TABLE IF EXISTS warehouses CASCADE;
DROP TABLE IF EXISTS user_addresses CASCADE;
DROP TABLE IF EXISTS reviews CASCADE;
DROP TABLE IF EXISTS prescription_items CASCADE;
DROP TABLE IF EXISTS prescriptions CASCADE;
DROP TABLE IF EXISTS payments CASCADE;
DROP TABLE IF EXISTS order_items CASCADE;
DROP TABLE IF EXISTS orders CASCADE;
DROP TABLE IF EXISTS cart_items CASCADE;
DROP TABLE IF EXISTS carts CASCADE;
DROP TABLE IF EXISTS medicine_images CASCADE;
DROP TABLE IF EXISTS medicines CASCADE;
DROP TABLE IF EXISTS suppliers CASCADE;
DROP TABLE IF EXISTS categories CASCADE;
DROP TABLE IF EXISTS users CASCADE;
DROP TABLE IF EXISTS roles CASCADE;

-- 1. Roles table
CREATE TABLE roles (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL UNIQUE
);

-- 2. Users table
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(255) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    phone VARCHAR(50),
    role_id INT REFERENCES roles(id) ON DELETE SET NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 3. Categories table
CREATE TABLE categories (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT
);

-- 4. Suppliers table
CREATE TABLE suppliers (
    id SERIAL PRIMARY KEY,
    company_name VARCHAR(255) NOT NULL,
    contact_person VARCHAR(255),
    email VARCHAR(255),
    phone VARCHAR(50),
    address TEXT,
    tax_code VARCHAR(100),
    status VARCHAR(50)
);

-- 5. Medicines table
CREATE TABLE medicines (
    id SERIAL PRIMARY KEY,
    category_id INT REFERENCES categories(id) ON DELETE SET NULL,
    supplier_id INT REFERENCES suppliers(id) ON DELETE SET NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    price DECIMAL(18, 2) NOT NULL,
    old_price DECIMAL(18, 2),
    unit VARCHAR(50),
    discount INT,
    origin VARCHAR(100),
    packaging VARCHAR(100),
    stock_quantity INT NOT NULL DEFAULT 0,
    manufacture_date TIMESTAMP,
    expiry_date TIMESTAMP,
    requires_prescription BOOLEAN DEFAULT FALSE,
    image_url TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);


-- 6. Medicine Images table
CREATE TABLE medicine_images (
    id SERIAL PRIMARY KEY,
    medicine_id INT REFERENCES medicines(id) ON DELETE CASCADE,
    image_url TEXT NOT NULL
);

-- 7. Carts table
CREATE TABLE carts (
    id SERIAL PRIMARY KEY,
    user_id INT REFERENCES users(id) ON DELETE CASCADE UNIQUE
);

-- 8. Cart Items table
CREATE TABLE cart_items (
    id SERIAL PRIMARY KEY,
    cart_id INT REFERENCES carts(id) ON DELETE CASCADE,
    medicine_id INT REFERENCES medicines(id) ON DELETE CASCADE,
    quantity INT NOT NULL DEFAULT 1,
    UNIQUE (cart_id, medicine_id)
);

-- 9. Orders table
CREATE TABLE orders (
    id SERIAL PRIMARY KEY,
    user_id INT REFERENCES users(id) ON DELETE CASCADE,
    total_amount DECIMAL(18, 2) NOT NULL,
    status VARCHAR(50) DEFAULT 'Pending',
    shipping_address TEXT,
    payment_status VARCHAR(50) DEFAULT 'Unpaid',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 10. Order Items table
CREATE TABLE order_items (
    id SERIAL PRIMARY KEY,
    order_id INT REFERENCES orders(id) ON DELETE CASCADE,
    medicine_id INT REFERENCES medicines(id) ON DELETE SET NULL,
    quantity INT NOT NULL,
    price DECIMAL(18, 2) NOT NULL
);

-- 11. Payments table
CREATE TABLE payments (
    id SERIAL PRIMARY KEY,
    order_id INT REFERENCES orders(id) ON DELETE CASCADE,
    method VARCHAR(50) NOT NULL,
    transaction_code VARCHAR(100) UNIQUE,
    amount DECIMAL(18, 2) NOT NULL,
    status VARCHAR(50) DEFAULT 'Pending',
    paid_at TIMESTAMP
);

-- 12. Prescriptions table
CREATE TABLE prescriptions (
    id SERIAL PRIMARY KEY,
    user_id INT REFERENCES users(id) ON DELETE CASCADE,
    doctor_name VARCHAR(255),
    hospital VARCHAR(255),
    prescription_date TIMESTAMP,
    image_url TEXT,
    status VARCHAR(50) DEFAULT 'Pending'
);

-- 13. Prescription Items table
CREATE TABLE prescription_items (
    id SERIAL PRIMARY KEY,
    prescription_id INT REFERENCES prescriptions(id) ON DELETE CASCADE,
    medicine_id INT REFERENCES medicines(id) ON DELETE SET NULL,
    quantity INT NOT NULL
);

-- 14. Reviews table
CREATE TABLE reviews (
    id SERIAL PRIMARY KEY,
    user_id INT REFERENCES users(id) ON DELETE CASCADE,
    medicine_id INT REFERENCES medicines(id) ON DELETE CASCADE,
    rating INT CHECK (rating >= 1 AND rating <= 5),
    comment TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 15. User Addresses table
CREATE TABLE user_addresses (
    id SERIAL PRIMARY KEY,
    user_id INT REFERENCES users(id) ON DELETE CASCADE,
    address_line TEXT,
    city VARCHAR(100),
    district VARCHAR(100),
    ward VARCHAR(100),
    is_default BOOLEAN DEFAULT FALSE
);

-- 16. Warehouses table
CREATE TABLE warehouses (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    address TEXT
);

-- 17. Inventory Stocks table
CREATE TABLE inventory_stocks (
    medicine_id INT REFERENCES medicines(id) ON DELETE CASCADE,
    warehouse_id INT REFERENCES warehouses(id) ON DELETE CASCADE,
    quantity INT NOT NULL DEFAULT 0,
    PRIMARY KEY (medicine_id, warehouse_id)
);

-- 18. Inventory Transactions table
CREATE TABLE inventory_transactions (
    id SERIAL PRIMARY KEY,
    medicine_id INT REFERENCES medicines(id) ON DELETE SET NULL,
    warehouse_id INT REFERENCES warehouses(id) ON DELETE SET NULL,
    type VARCHAR(50) NOT NULL,
    quantity INT NOT NULL,
    reference_id VARCHAR(100),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 19. Supplier Medicines table
CREATE TABLE supplier_medicines (
    supplier_id INT REFERENCES suppliers(id) ON DELETE CASCADE,
    medicine_id INT REFERENCES medicines(id) ON DELETE CASCADE,
    PRIMARY KEY (supplier_id, medicine_id)
);


-- ==================== RPC FUNCTIONS FOR POSTGREST ====================

-- Function to register a new user
CREATE OR REPLACE FUNCTION register_user(
  p_username VARCHAR,
  p_email VARCHAR,
  p_password VARCHAR,
  p_phone VARCHAR,
  p_role_id INT
) RETURNS TABLE (
  id INT,
  username VARCHAR,
  email VARCHAR,
  phone VARCHAR,
  role_id INT,
  is_active BOOLEAN
) AS $$
DECLARE
  v_user_id INT;
BEGIN
  IF EXISTS (SELECT 1 FROM users WHERE users.username = p_username) THEN
    RAISE EXCEPTION 'Username already exists';
  END IF;

  IF EXISTS (SELECT 1 FROM users WHERE users.email = p_email) THEN
    RAISE EXCEPTION 'Email already exists';
  END IF;

  INSERT INTO users (username, email, password_hash, phone, role_id)
  VALUES (p_username, p_email, p_password, p_phone, p_role_id)
  RETURNING users.id INTO v_user_id;

  -- Automatically create a cart for the user
  INSERT INTO carts (user_id) VALUES (v_user_id);

  RETURN QUERY
  SELECT u.id, u.username, u.email, u.phone, u.role_id, u.is_active
  FROM users u
  WHERE u.id = v_user_id;
END;
$$ LANGUAGE plpgsql;

-- Function to login user
CREATE OR REPLACE FUNCTION login_user(
  p_username VARCHAR,
  p_password VARCHAR
) RETURNS TABLE (
  id INT,
  username VARCHAR,
  email VARCHAR,
  phone VARCHAR,
  role_id INT,
  is_active BOOLEAN,
  cart_id INT
) AS $$
BEGIN
  RETURN QUERY
  SELECT u.id, u.username, u.email, u.phone, u.role_id, u.is_active, c.id AS cart_id
  FROM users u
  LEFT JOIN carts c ON c.user_id = u.id
  WHERE u.username = p_username AND u.password_hash = p_password AND u.is_active = TRUE;
END;
$$ LANGUAGE plpgsql;

-- Function to synchronize local cart items
CREATE OR REPLACE FUNCTION sync_cart_items(
  p_user_id INT,
  p_items JSONB
) RETURNS VOID AS $$
DECLARE
  v_cart_id INT;
  v_item RECORD;
BEGIN
  -- Get or create cart for user
  SELECT id INTO v_cart_id FROM carts WHERE user_id = p_user_id;
  IF v_cart_id IS NULL THEN
    INSERT INTO carts (user_id) VALUES (p_user_id) RETURNING id INTO v_cart_id;
  END IF;

  -- Iterate through items and upsert
  FOR v_item IN SELECT * FROM jsonb_to_recordset(p_items) AS x(medicine_id INT, quantity INT)
  LOOP
    INSERT INTO cart_items (cart_id, medicine_id, quantity)
    VALUES (v_cart_id, v_item.medicine_id, v_item.quantity)
    ON CONFLICT (cart_id, medicine_id)
    DO UPDATE SET quantity = cart_items.quantity + EXCLUDED.quantity;
  END LOOP;
END;
$$ LANGUAGE plpgsql;
