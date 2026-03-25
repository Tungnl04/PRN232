USE master;
GO
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'FoodStoreDB')
BEGIN
    ALTER DATABASE FoodStoreDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE FoodStoreDB;
END
GO
CREATE DATABASE FoodStoreDB
GO
USE FoodStoreDB;
GO
-- ===== CATEGORY =====
CREATE TABLE category (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    description NVARCHAR(500)
);

-- ===== PRODUCT =====
CREATE TABLE product (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(150) NOT NULL,
    description NVARCHAR(1000),
    price DECIMAL(10,2) NOT NULL,
    image_url NVARCHAR(255),
    inventory INT DEFAULT 0,
    category_id INT,
    is_available BIT DEFAULT 1,

    CONSTRAINT fk_product_category 
        FOREIGN KEY (category_id) REFERENCES category(id)
);

-- ===== COMBO =====
CREATE TABLE combo (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(150) NOT NULL,
    description NVARCHAR(1000),
    price DECIMAL(10,2) NOT NULL,
    image_url NVARCHAR(255),
    available BIT DEFAULT 1
);

-- ===== COMBO ITEM =====
CREATE TABLE combo_item (
    combo_id INT,
    product_id INT,
    quantity INT DEFAULT 1,

    PRIMARY KEY (combo_id, product_id),

    CONSTRAINT fk_combo_item_combo 
        FOREIGN KEY (combo_id) REFERENCES combo(id),

    CONSTRAINT fk_combo_item_product 
        FOREIGN KEY (product_id) REFERENCES product(id)
);

-- ===== USER =====
CREATE TABLE [user] (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    username NVARCHAR(50) UNIQUE NOT NULL,
    password_hash NVARCHAR(255) NOT NULL,
    role NVARCHAR(20) CHECK (role IN ('staff', 'admin', 'kitchen')),
    active BIT DEFAULT 1
);

-- ===== ORDER TABLE =====
CREATE TABLE order_table (
    id INT IDENTITY(1,1) PRIMARY KEY,
    table_number NVARCHAR(10) NOT NULL UNIQUE,
    capacity INT NOT NULL,
    status NVARCHAR(20) DEFAULT 'available' 
        CHECK (status IN ('available', 'taken', 'reserved', 'cleaning')),
    qr_code_token NVARCHAR(100),
    location NVARCHAR(50),
    created_at DATETIME DEFAULT GETDATE()
);

-- ===== CUSTOMER =====
CREATE TABLE customer (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    email NVARCHAR(100),
    created_at DATETIME DEFAULT GETDATE()
);

-- ===== ORDER =====
CREATE TABLE [order] (
    id INT IDENTITY(1,1) PRIMARY KEY,
    order_code NVARCHAR(50) NOT NULL UNIQUE,
    customer_id INT,
    table_id INT,
    total_amount DECIMAL(10,2) DEFAULT 0,
    status NVARCHAR(20) DEFAULT 'pending'
        CHECK (status IN ('pending', 'processing', 'preparing', 'ready', 'served', 'paid', 'rejected', 'cancelled')),
    payment_status NVARCHAR(20) DEFAULT 'pending'
        CHECK (payment_status IN ('pending', 'success', 'failed', 'expired')),
    payment_method NVARCHAR(20), -- 'cash', 'vnpay', 'momo'
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME,

    CONSTRAINT fk_order_customer 
        FOREIGN KEY (customer_id) REFERENCES customer(id),

    CONSTRAINT fk_order_table 
        FOREIGN KEY (table_id) REFERENCES order_table(id)
);

-- ===== ORDER ITEM =====
CREATE TABLE order_item (
    id INT IDENTITY(1,1) PRIMARY KEY,
    order_id INT NOT NULL,
    product_id INT,
    combo_id INT,
    quantity INT DEFAULT 1,
    unit_price DECIMAL(10,2) NOT NULL,
    status NVARCHAR(20) DEFAULT 'pending'
        CHECK (status IN ('pending', 'preparing', 'ready', 'served', 'cancelled')),
    note NVARCHAR(255),
    rejection_reason NVARCHAR(255),

    CONSTRAINT fk_orderitem_order 
        FOREIGN KEY (order_id) REFERENCES [order](id),

    CONSTRAINT fk_orderitem_product 
        FOREIGN KEY (product_id) REFERENCES product(id),

    CONSTRAINT fk_orderitem_combo 
        FOREIGN KEY (combo_id) REFERENCES combo(id)
);

-- ===== NOTIFICATION =====
CREATE TABLE notification (
    id INT IDENTITY(1,1) PRIMARY KEY,
    message NVARCHAR(500) NOT NULL,
    type NVARCHAR(50), -- 'new_order', 'order_ready', 'order_rejected', 'payment_success', 'table_opened', 'table_closed', 'low_inventory', 'system'
    target_role NVARCHAR(20), -- 'staff', 'kitchen', 'admin'
    is_read BIT DEFAULT 0,
    created_at DATETIME DEFAULT GETDATE()
);

-- ===== ORDER STATUS HISTORY =====
CREATE TABLE order_status_history (
    id INT IDENTITY(1,1) PRIMARY KEY,
    order_id INT NOT NULL,
    old_status NVARCHAR(20),
    new_status NVARCHAR(20),
    changed_by INT, -- user_id if staff, null if guest
    note NVARCHAR(255),
    created_at DATETIME DEFAULT GETDATE(),

    CONSTRAINT fk_history_order FOREIGN KEY (order_id) REFERENCES [order](id)
);

-- ===== ACTIVITY LOG =====
CREATE TABLE activity_log (
    id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT, -- null if guest
    action NVARCHAR(100) NOT NULL, -- 'create_order', 'cancel_item', 'pay_order', etc.
    description NVARCHAR(500),
    created_at DATETIME DEFAULT GETDATE()
);