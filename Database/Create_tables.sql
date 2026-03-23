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
    inventory INT,
    category_id INT,

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
    role NVARCHAR(20) CHECK (role IN ('staff', 'admin')),
    active BIT DEFAULT 1
);

-- ===== ORDER TABLE =====
CREATE TABLE order_table (
    id INT IDENTITY(1,1) PRIMARY KEY,
    table_number NVARCHAR(10) NOT NULL UNIQUE,
    capacity INT NOT NULL,
    status NVARCHAR(20) DEFAULT 'available' 
        CHECK (status IN ('available', 'taken')),
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
    order_code NVARCHAR(20) NOT NULL,
    customer_id INT,
    table_id INT,
    total_amount DECIMAL(10,2) DEFAULT 0,
    status NVARCHAR(20) DEFAULT 'pending',
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
    unit_price DE	CIMAL(10,2) NOT NULL,

    CONSTRAINT fk_orderitem_order 
        FOREIGN KEY (order_id) REFERENCES [order](id),

    CONSTRAINT fk_orderitem_product 
        FOREIGN KEY (product_id) REFERENCES product(id),

    CONSTRAINT fk_orderitem_combo 
        FOREIGN KEY (combo_id) REFERENCES combo(id)
);