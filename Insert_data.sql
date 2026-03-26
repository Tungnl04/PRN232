USE [FoodStoreDB];
GO

-- 1. CLEANUP
DELETE FROM [activity_log];
DELETE FROM [order_status_history];
DELETE FROM [notification];
DELETE FROM [order_item];
DELETE FROM [order];
DELETE FROM [combo_item];
DELETE FROM [combo];
DELETE FROM [product];
DELETE FROM [category];
DELETE FROM [order_table];
DELETE FROM [customer];
DELETE FROM [user];
GO

-- 2. INSERT TABLES (Lookup)
SET IDENTITY_INSERT [order_table] ON;
INSERT INTO [order_table] (id, table_number, capacity, status) VALUES 
(1, '1', 4, 'available'), (2, '2', 4, 'available'), (3, '3', 2, 'available'), (4, '4', 6, 'available'), 
(5, '5', 4, 'available'), (6, '6', 8, 'available'), (7, '7', 2, 'available'), (8, '8', 4, 'available');
SET IDENTITY_INSERT [order_table] OFF;

-- 3. INSERT CATEGORIES
SET IDENTITY_INSERT [category] ON;
INSERT INTO category (id, name, description) VALUES 
(1, N'GÀ - GÀ QUAY',              N'Các loại gà rán, gà quay'),
(2, N'BURGER - CƠM - MÌ Ý',       N'Các loại bánh burger, Cơm gà, mì ý'),
(3, N'THỨC ĂN NHẸ',               N'Các loại salad, khoai tây'),
(4, N'THỨC UỐNG - TRÁNG MIỆNG',   N'Các loại nước ngọt, bánh ngọt');
SET IDENTITY_INSERT [category] OFF;

-- 4. INSERT PRODUCTS
SET IDENTITY_INSERT [product] ON;
-- category 1: GÀ - GÀ QUAY (IDs 1-12)
INSERT INTO product (id, name, description, price, image_url, inventory, category_id, is_available) VALUES
(1, N'1 Miếng gà rán', N'1 Miếng Gà Giòn Cay/Gà Truyền Thống/Gà Giòn Không Cay + 1 Gói tương (cà/ ớt)', 35000, 'https://static.kfcvietnam.com.vn/images/items/lg/1-GA-XOT.jpg?v=gk7XPg', 20, 1, 1),
(2, N'2 Miếng gà rán', N'2 Miếng Gà Giòn Cay/Gà Truyền Thống/Gà Giòn Không Cay + 2 Gói tương (cà/ ớt)', 70000, 'https://static.kfcvietnam.com.vn/images/items/lg/2-GA-XOT.jpg?v=gk7XPg', 10, 1, 1),
(3, N'3 Miếng gà rán', N'3 Miếng Gà Giòn Cay/Gà Truyền Thống/Gà Giòn Không Cay + 3 Gói tương (cà/ ớt)', 104000, 'https://static.kfcvietnam.com.vn/images/items/lg/3-GA-XOT.jpg?v=gk7XPg', 10, 1, 1),
(4, N'6 Miếng gà rán', N'6 Miếng Gà Giòn Cay/Gà Truyền Thống/Gà Giòn Không Cay + 6 Gói tương (cà/ ớt)', 205000, 'https://static.kfcvietnam.com.vn/images/items/lg/6-GA-XOT.jpg?v=gk7XPg', 10, 1, 1),
(5, N'1 Miếng Phi-lê Gà Quay', N'1 Miếng Phi-lê Gà Quay Flava/Phi-lê Gà Quay Tiêu', 42000, 'https://static.kfcvietnam.com.vn/images/items/lg/PHILE-XOT.jpg?v=gk7XPg', 10, 1, 1),
(6, N'Gà Viên (Vừa)', N'Gà Viên (Vừa) + 1 Gói tương (cà/ ớt)', 38000, 'https://static.kfcvietnam.com.vn/images/items/lg/POPCORN-XOT.jpg?v=gk7XPg', 10, 1, 1),
(7, N'Gà Viên (Lớn)', N'Gà Viên (Lớn) + 2 Gói tương (cà/ ớt)', 64000, 'https://static.kfcvietnam.com.vn/images/items/lg/POP-L.jpg?v=gk7XPg', 10, 1, 1),
(8, N'3 Gà Miếng Nuggets', N'3 Gà Miếng Nuggets + 1 Gói tương (cà/ ớt)', 27000, 'https://static.kfcvietnam.com.vn/images/items/lg/3_Nuggests.jpg?v=gk7XPg', 10, 1, 1),
(9, N'5 Gà Miếng Nuggets', N'5 Gà Miếng Nuggets + 2 Gói tương (cà/ ớt)', 40000, 'https://static.kfcvietnam.com.vn/images/items/lg/5_Nuggests.jpg?v=gk7XPg', 10, 1, 1),
(10, N'10 Gà Miếng Nuggets', N'10 Gà Miếng Nuggets + 4 Gói tương (cà/ ớt)', 75000, 'https://static.kfcvietnam.com.vn/images/items/lg/10_Nuggests.jpg?v=gk7XPg', 10, 1, 1),
(11, N'3 Miếng Gà Rán Tender', N'3 Miếng Gà Rán Tender + 1 Gói tương (cà/ ớt)', 41000, 'https://static.kfcvietnam.com.vn/images/items/lg/TENDERS-3.jpg?v=gk7XPg', 10, 1, 1),
(12, N'5 Miếng Gà Rán Tender', N'5 Miếng Gà Rán Tender + 2 Gói tương (cà/ ớt)', 66000, 'https://static.kfcvietnam.com.vn/images/items/lg/TENDERS-5.jpg?v=gk7XPg', 10, 1, 1);

-- category 2: BURGER - CƠM - MÌ Ý (IDs 13-22)
INSERT INTO product (id, name, description, price, image_url, inventory, category_id, is_available) VALUES
(13, N'Burger Zinger', N'1 Burger Zinger + 1 Gói tương (cà/ ớt)', 54000, 'https://static.kfcvietnam.com.vn/images/items/lg/Burger-Zinger.jpg?v=gk7XPg', 10, 2, 1),
(14, N'Burger Tôm', N'1 Burger Tôm + 1 Gói tương (cà/ ớt)', 45000, 'https://static.kfcvietnam.com.vn/images/items/lg/Burger-Shrimp.jpg?v=gk7XPg', 10, 2, 1),
(15, N'Burger Gà Quay Flava', N'1 Burger Gà Quay Flava + 1 Gói tương (cà/ ớt)', 54000, 'https://static.kfcvietnam.com.vn/images/items/lg/Burger-Flava.jpg?v=gk7XPg', 10, 2, 1),
(16, N'Cơm Gà Teriyaki', N'1 Cơm Gà Teriyaki + 1 Gói tương (cà/ ớt)', 45000, 'https://static.kfcvietnam.com.vn/images/items/lg/Rice-Teriyaki.jpg?v=gk7XPg', 10, 2, 1),
(17, N'Cơm Gà Rán', N'1 Cơm Gà Rán + 1 Gói tương (cà/ ớt)', 49000, 'https://static.kfcvietnam.com.vn/images/items/lg/Rice-F.Chicken.jpg?v=gk7XPg', 10, 2, 1),
(18, N'Cơm Phi-lê Gà Quay', N'1 Cơm Phi-lê Gà Quay + 1 Gói tương (cà/ ớt)', 49000, 'https://static.kfcvietnam.com.vn/images/items/lg/Rice-Flava.jpg?v=gk7XPg', 10, 2, 1),
(19, N'Cơm', N'1 Cơm', 12000, 'https://static.kfcvietnam.com.vn/images/items/lg/Rice.jpg?v=gk7XPg', 10, 2, 1),
(20, N'Mì Ý Gà Viên', N'1 Mì Ý Gà Viên + 1 Gói tương (cà/ ớt)', 40000, 'https://static.kfcvietnam.com.vn/images/items/lg/MI-Y-GA-VIEN.jpg?v=gk7XPg', 10, 2, 1),
(21, N'Mì Ý Gà Rán', N'1 Mì Ý Gà Rán + 1 Gói tương (cà/ ớt)', 64000, 'https://static.kfcvietnam.com.vn/images/items/lg/MI-Y-GA-RAN.jpg?v=gk7XPg', 10, 2, 1),
(22, N'Cơm Gà Viên Nanban', N'1 Cơm Gà Viên Nanban + 1 Gói tương (cà/ ớt)', 12000, 'https://static.kfcvietnam.com.vn/images/items/lg/NANBAN.jpg?v=gk7XPg', 10, 2, 1);

-- category 3: THỨC ĂN NHẸ (IDs 23-39)
INSERT INTO product (id, name, description, price, image_url, inventory, category_id, is_available) VALUES
(23, N'Salad Hạt', N'1 Salad Hạt', 39000, 'https://static.kfcvietnam.com.vn/images/items/lg/SALAD-HAT.jpg?v=gk7XPg', 10, 3, 1),
(24, N'Salad Pop', N'1 Salad Hạt Gà Viên Popcorn', 45000, 'https://static.kfcvietnam.com.vn/images/items/lg/SALAD-HAT-GA-VIEN.jpg?v=gk7XPg', 10, 3, 1),
(25, N'3 Cá Thanh', N'3 Cá Thanh + 1 Gói tương (cà/ ớt)', 40000, 'https://static.kfcvietnam.com.vn/images/items/lg/3-FISH-STICK.jpg?v=gk7XPg', 10, 3, 1),
(26, N'4 Phô Mai Viên', N'4 Phô Mai Viên + 1 Gói tương (cà/ ớt)', 36000, 'https://static.kfcvietnam.com.vn/images/items/lg/4-Chewy-Cheese.jpg?v=gk7XPg', 10, 3, 1),
(27, N'6 Phô Mai Viên', N'6 Phô Mai Viên + 1 Gói tương (cà/ ớt)', 49000, 'https://static.kfcvietnam.com.vn/images/items/lg/6-Chewy-Cheese.jpg?v=gk7XPg', 10, 3, 1),
(28, N'Khoai Tây Chiên (Vừa)', N'Khoai Tây Chiên (Vừa) + 1 Gói tương (cà/ ớt)', 19000, 'https://static.kfcvietnam.com.vn/images/items/lg/FF-R.jpg?v=gk7XPg', 10, 3, 1),
(29, N'Khoai Tây Chiên (Lớn)', N'Khoai Tây Chiên (Lớn) + 1 Gói tương (cà/ ớt)', 29000, 'https://static.kfcvietnam.com.vn/images/items/lg/FF-L.jpg?v=gk7XPg', 10, 3, 1),
(30, N'Khoai Tây Chiên (Đại)', N'Khoai Tây Chiên (Đại) + 2 Gói tương (cà/ ớt)', 39000, 'https://static.kfcvietnam.com.vn/images/items/lg/FF-J.jpg?v=gk7XPg', 10, 3, 1),
(31, N'Khoai Tây Múi Cau (Vừa)', N'01 Khoai Tây Múi Cau (vừa) + 1 Gói tương (cà/ ớt)', 23000, 'https://static.kfcvietnam.com.vn/images/items/lg/khoai-mui-cau-R.jpg?v=gk7XPg', 10, 3, 1),
(32, N'Khoai Tây Múi Cau (Lớn)', N'01 Khoai Tây Múi Cau (lớn) + 1 Gói tương (cà/ ớt)', 43000, 'https://static.kfcvietnam.com.vn/images/items/lg/khoai-mui-cau-L.jpg?v=gk7XPg', 10, 3, 1),
(33, N'Khoai Tây Nghiền (Vừa)', N'Khoai Tây Nghiền (Vừa)', 12000, 'https://static.kfcvietnam.com.vn/images/items/lg/MP-(R)-new.jpg?v=gk7XPg', 10, 3, 1),
(34, N'Khoai Tây Nghiền (Lớn)', N'Khoai Tây Nghiền (Lớn)', 22000, 'https://static.kfcvietnam.com.vn/images/items/lg/MP-(L)-new.jpg?v=gk7XPg', 10, 3, 1),
(35, N'Khoai Tây Nghiền (Đại)', N'Khoai Tây Nghiền (Đại)', 31000, 'https://static.kfcvietnam.com.vn/images/items/lg/MP-(J)-new.jpg?v=gk7XPg', 10, 3, 1),
(36, N'Bắp Cải Trộn (Vừa)', N'Bắp Cải Trộn (Vừa)', 12000, 'https://static.kfcvietnam.com.vn/images/items/lg/CL-(R)-new.jpg?v=gk7XPg', 10, 3, 1),
(37, N'Bắp Cải Trộn (Lớn)', N'Bắp Cải Trộn (Lớn)', 22000, 'https://static.kfcvietnam.com.vn/images/items/lg/CL-(L)-new.jpg?v=gk7XPg', 10, 3, 1),
(38, N'Bắp Cải Trộn (Đại)', N'Bắp Cải Trộn (Đại)', 31000, 'https://static.kfcvietnam.com.vn/images/items/lg/CL-(J)-new.jpg?v=gk7XPg', 10, 3, 1),
(39, N'Súp Rong Biển', N'Súp Rong Biển', 19000, 'https://static.kfcvietnam.com.vn/images/items/lg/Soup-Rong-Bien.jpg?v=gk7XPg', 10, 3, 1);

-- category 4: THỨC UỐNG - TRÁNG MIỆNG (IDs 40-63)
INSERT INTO product (id, name, description, price, image_url, inventory, category_id, is_available) VALUES
(40, N'1 Bánh Trứng', N'1 Bánh Trứng', 18000, 'https://static.kfcvietnam.com.vn/images/items/lg/EGGTART-1.jpg?v=gk7XPg', 10, 4, 1),
(41, N'4 Bánh Trứng', N'4 Bánh Trứng', 64000, 'https://static.kfcvietnam.com.vn/images/items/lg/EGGTART-4.jpg?v=gk7XPg', 10, 4, 1),
(42, N'2 Viên Khoai Môn Kim Sa', N'2 Viên Khoai Môn Kim Sa', 26000, 'https://static.kfcvietnam.com.vn/images/items/lg/2-taro.jpg?v=gk7XPg', 10, 4, 1),
(43, N'3 Viên Khoai Môn Kim Sa', N'3 Viên Khoai Môn Kim Sa', 34000, 'https://static.kfcvietnam.com.vn/images/items/lg/3-taro.jpg?v=gk7XPg', 10, 4, 1),
(44, N'5 Viên Khoai Môn Kim Sa', N'5 Viên Khoai Môn Kim Sa', 54000, 'https://static.kfcvietnam.com.vn/images/items/lg/5-taro.jpg?v=gk7XPg', 10, 4, 1),
(45, N'Pepsi Lon', N'Pepsi Lon', 19000, 'https://static.kfcvietnam.com.vn/images/items/lg/PEPSI_CAN.jpg?v=gk7XPg', 10, 4, 1),
(46, N'7Up Lon', N'7Up Lon', 19000, 'https://static.kfcvietnam.com.vn/images/items/lg/7UP_CAN.jpg?v=gk7XPg', 10, 4, 1),
(47, N'Aquafina 500ml', N'Aquafina 500ml', 15000, 'https://static.kfcvietnam.com.vn/images/items/lg/AQUAFINA.jpg?v=gk7XPg', 10, 4, 1),
(48, N'Pepsi Không Calo Lon', N'Pepsi Không Calo Lon', 19000, 'https://static.kfcvietnam.com.vn/images/items/lg/pepsi-zero.jpg?v=gk7XPg', 10, 4, 1),
(49, N'Lon Sting', N'Lon Sting', 19000, 'https://static.kfcvietnam.com.vn/images/items/lg/Sting.jpg?v=gk7XPg', 10, 4, 1),
(50, N'Pepsi (Tiêu Chuẩn)', N'1 Ly Pepsi (Tiêu Chuẩn)', 12000, 'https://static.kfcvietnam.com.vn/images/items/lg/PEPSI-STD.jpg?v=gk7XPg', 10, 4, 1),
(51, N'Pepsi (Vừa)', N'1 Ly Pepsi (Vừa)', 15000, 'https://static.kfcvietnam.com.vn/images/items/lg/PEPSI-M.jpg?v=gk7XPg', 10, 4, 1),
(52, N'Pepsi (Đại)', N'1 Ly Pepsi (Đại)', 19000, 'https://static.kfcvietnam.com.vn/images/items/lg/PEPSI-J.jpg?v=gk7XPg', 10, 4, 1),
(53, N'7Up (Tiêu Chuẩn)', N'1 Ly 7Up (Tiêu Chuẩn)', 12000, 'https://static.kfcvietnam.com.vn/images/items/lg/7UP-STD.jpg?v=gk7XPg', 10, 4, 1),
(54, N'7Up (Vừa)', N'1 Ly 7Up (Vừa)', 15000, 'https://static.kfcvietnam.com.vn/images/items/lg/7UP-R.jpg?v=gk7XPg', 10, 4, 1),
(55, N'7Up (Đại)', N'1 Ly 7Up (Đại)', 19000, 'https://static.kfcvietnam.com.vn/images/items/lg/7UP-L.jpg?v=gk7XPg', 10, 4, 1),
(56, N'Lipton (Tiêu Chuẩn)', N'1 Ly Lipton (Tiêu Chuẩn)', 12000, 'https://static.kfcvietnam.com.vn/images/items/lg/LIPTON-STD.jpg?v=gk7XPg', 10, 4, 1),
(57, N'Lipton (Vừa)', N'1 Ly Lipton (Vừa)', 15000, 'https://static.kfcvietnam.com.vn/images/items/lg/LIPTON-M.jpg?v=gk7XPg', 10, 4, 1),
(58, N'Lipton (Đại)', N'1 Ly Lipton (Đại)', 19000, 'https://static.kfcvietnam.com.vn/images/items/lg/LIPTON-J.jpg?v=gk7XPg', 10, 4, 1),
(59, N'Pepsi Không Đường (Tiêu Chuẩn)', N'1 Ly Pepsi Không Đường (Tiêu Chuẩn)', 12000, 'https://static.kfcvietnam.com.vn/images/items/lg/PEPSI-ZERO-STD.jpg?v=gk7XPg', 10, 4, 1),
(60, N'Pepsi Không Đường (Vừa)', N'1 Ly Pepsi Không Đường (Vừa)', 15000, 'https://static.kfcvietnam.com.vn/images/items/lg/PEPSI-ZERO-M.jpg?v=gk7XPg', 10, 4, 1),
(61, N'Pepsi Không Đường (Đại)', N'1 Ly Pepsi Không Đường (Đại)', 19000, 'https://static.kfcvietnam.com.vn/images/items/lg/PEPSI-ZERO-J.jpg?v=gk7XPg', 10, 4, 1),
(62, N'Sô-cô-la Sữa Đá', N'1 Ly Sô-cô-la Sữa Đá', 20000, 'https://static.kfcvietnam.com.vn/images/items/lg/CHOCO-MILK-STD.jpg?v=gk7XPg', 10, 4, 1),
(63, N'Sô-cô-la Sữa Nóng', N'1 Ly Sô-cô-la Sữa Nóng', 20000, 'https://static.kfcvietnam.com.vn/images/items/lg/ChoCo_Hot.jpg?v=gk7XPg', 10, 4, 1);
SET IDENTITY_INSERT [product] OFF;

-- 5. INSERT COMBOS
SET IDENTITY_INSERT [combo] ON;
INSERT INTO combo (id, name, description, price, image_url, available) VALUES 
(1, N'Combo 1 Miếng Gà', N'1 Miếng Gà Rán + 1 Khoai Tây Chiên (Vừa)/ 1 Khoai Tây Nghiền (Vừa) & 1 Bắp Cải Trộn (Vừa) + 1 Pepsi (Tiêu chuẩn) + 2 Gói tương (cà/ ớt)', 58000, 'https://static.kfcvietnam.com.vn/images/items/lg/D-CHICKEN-1.jpg?v=gk7XPg', 1),
(2, N'Combo 2 Miếng Gà', N'2 Miếng Gà Rán + 1 Khoai Tây Chiên (Vừa)/1 Khoai Tây Nghiền (Vừa) & 1 Bắp Cải Trộn (Vừa) + 1 Ly Pepsi (Tiêu chuẩn) + 3 Gói tương (cà/ ớt)', 95000, 'https://static.kfcvietnam.com.vn/images/items/lg/D-CHICKEN-2.jpg?v=gk7XPg', 1),
(3, N'Combo Gà Rán Tender', N'3 Miếng Gà Rán Tenders + 1 Xà lách Hạt + 1 Pepsi Không Đường (Tiêu chuẩn) + 1 Gói tương (cà/ ớt)', 85000, 'https://static.kfcvietnam.com.vn/images/items/lg/D-TENDER.jpg?v=gk7XPg', 1),
(4, N'Combo Phi-lê Gà Quay', N'1 Miếng Phi-lê Gà Quay + 1 Bắp Cải Trộn (Lớn) + 1 Pepsi Không Đường (Tiêu chuẩn) + 1 Gói tương (cà/ ớt)', 69000, 'https://static.kfcvietnam.com.vn/images/items/lg/D-ROASTED.jpg?v=gk7XPg', 1),
(5, N'Combo Mì Ý Gà Viên', N'1 Mì Ý Gà Viên + 1 Pepsi (Tiêu chuẩn) + 1 Gói tương (cà/ ớt)', 45000, 'https://static.kfcvietnam.com.vn/images/items/lg/D-PASTA-POP.jpg?v=gk7XPg', 1),
(6, N'Combo Mì Ý Gà Rán', N'1 Mì Ý Gà Rán + 1 Ly Pepsi (Tiêu chuẩn) + 1 Gói tương (cà/ ớt)', 72000, 'https://static.kfcvietnam.com.vn/images/items/lg/D-PASTA-COB.jpg?v=gk7XPg', 1),
(7, N'Combo Mì Ý và Gà Tender', N'1 Mì Ý Gà Viên + 2 Miếng Gà Rán Tenders + 1 Khoai Tây Chiên (Vừa) + 1 Pepsi (Tiêu chuẩn) + 3 Gói tương (cà/ ớt)', 95000, 'https://static.kfcvietnam.com.vn/images/items/lg/D-PASTA-TENDERS.jpg?v=gk7XPg', 1),
(8, N'Combo Mì Ý và Salad Gà', N'1 Mì Ý Gà Viên + 1 Xà Lách Gà Viên + 1 Pepsi (Tiêu chuẩn) + 1 Gói tương (cà/ ớt)', 85000, 'https://static.kfcvietnam.com.vn/images/items/lg/D-PASTA-SALAD.jpg?v=gk7XPg', 1),
(9, N'Combo Cơm Gà Rán', N'1 Cơm Gà Rán + 1 Ly Pepsi (Tiêu chuẩn) + 1 Gói tương (cà/ ớt)', 59000, 'https://static.kfcvietnam.com.vn/images/items/lg/D-RICE-COB.jpg?v=gk7XPg', 1),
(10, N'Combo Cơm Gà Quay', N'1 Cơm Gà Flava + 1 Ly Pepsi (Tiêu chuẩn) + 1 Gói tương (cà/ ớt)', 59000, 'https://static.kfcvietnam.com.vn/images/items/lg/D-RICE-ROASTED.jpg?v=gk7XPg', 1),
(11, N'Combo Cơm Gà Nanban', N'1 Cơm Gà Nanban + 1 Ly Pepsi (Tiêu chuẩn) + 1 Gói tương (cà/ ớt)', 49000, 'https://static.kfcvietnam.com.vn/images/items/lg/D-RICE-NANBAN.jpg?v=gk7XPg', 1),
(12, N'Combo Cơm Gà Nanban và Súp Rong Biển', N'1 Cơm Gà Nanban + 1 Súp Rong Biển + 1 Ly Pepsi (Tiêu chuẩn) + 1 Gói tương (cà/ ớt)', 68000, 'https://static.kfcvietnam.com.vn/images/items/lg/D-RICE-NANBAN-SOUP.jpg?v=gk7XPg', 1),
(13, N'Combo Burger Tôm', N'1 Burger Tôm + 1 Ly Pepsi (Tiêu chuẩn) + 1 Gói tương (cà/ ớt)', 50000, 'https://static.kfcvietnam.com.vn/images/items/lg/D-SHRIMP.jpg?v=gk7XPg', 1),
(14, N'Combo Burger Gà Zinger và Khoai', N'1 Burger Zinger + 1 Khoai Tây Chiên (Vừa) + 1 Ly Pepsi (Tiêu chuẩn) + 2 Gói tương (cà/ ớt)', 77000, 'https://static.kfcvietnam.com.vn/images/items/lg/D-B.ZINGER-FF.jpg?v=gk7XPg', 1),
(15, N'Combo Burger Phi-lê Gà Quay và Khoai', N'1 Burger Gà Quay + 1 Khoai Tây Chiên (Vừa) + 1 Ly Pepsi (Tiêu chuẩn) + 2 Gói tương (cà/ ớt)', 77000, 'https://static.kfcvietnam.com.vn/images/items/lg/DB-ROASTED-FF.jpg?v=gk7XPg', 1),
(16, N'Combo Burger Tôm & Gà Rán', N'1 Burger Tôm + 1 Miếng Gà Rán + 1 Ly Pepsi (Tiêu chuẩn) + 2 Gói tương (cà/ ớt)', 87000, 'https://static.kfcvietnam.com.vn/images/items/lg/D-B.SHRIMP-COB.jpg?v=gk7XPg', 1),
(17, N'Combo Burger Phi-lê Gà Quay và Gà Rán', N'1 Burger Phi-lê Gà Quay + 1 Miếng Gà Rán + 1 Ly Pepsi (Tiêu chuẩn) + 2 Gói tương (cà/ ớt)', 95000, 'https://static.kfcvietnam.com.vn/images/items/lg/DB-ROASTED-CBO.jpg?v=gk7XPg', 1),
(18, N'Combo Burger Gà Zinger và Gà Rán', N'1 Burger Zinger + 1 Miếng Gà Rán + 1 Ly Pepsi (Tiêu chuẩn) + 2 Gói tương (cà/ ớt)', 95000, 'https://static.kfcvietnam.com.vn/images/items/lg/D-B.ZINGER-COB.jpg?v=gk7XPg', 1),
(19, N'Combo Burger và Gà Rán', N'1 Burger Gà Quay/Zinger + 1 Khoai Tây Chiên (Vừa) + 1 Miếng Gà Rán + 1 Ly Pepsi (Tiêu chuẩn) + 3 Gói tương (cà/ ớt)', 112000, 'https://static.kfcvietnam.com.vn/images/items/lg/D-BURGER-COB-FF.jpg?v=gk7XPg', 1),
(20, N'Combo Nhóm 2 Hoàn Hảo', N'2 Miếng Gà Rán + 1 Burger Zinger + 2 Ly Pepsi (Tiêu chuẩn) + 3 Gói tương (cà/ ớt)', 135000, 'https://static.kfcvietnam.com.vn/images/items/lg/DBUCKET1.jpg?v=gk7XPg', 1),
(21, N'Combo Nhóm 2 Tròn Vị', N'3 Miếng Gà Rán + 1 Mì Ý Gà Viên + 2 Ly Pepsi (Tiêu chuẩn) + 4 Gói tương (cà/ ớt)', 160000, 'https://static.kfcvietnam.com.vn/images/items/lg/DBUCKET5.jpg?v=gk7XPg', 1),
(22, N'Combo Nhóm 2 No Nê', N'4 Miếng Gà Rán + 1 Khoai Múi Cau (Vừa) + 2 Ly Pepsi (Tiêu chuẩn) + 5 Gói tương (cà/ ớt)', 179000, 'https://static.kfcvietnam.com.vn/images/items/lg/DBUCKET4.jpg?v=gk7XPg', 1),
(23, N'Combo Nhóm 3 Đủ Đầy', N'3 Miếng Gà Rán + 1 Mì Ý Gà Viên + 1 Burger Tôm + 1 Khoai Tây Chiên (Vừa) + 3 Ly Pepsi (Tiêu chuẩn) + 6 Gói tương (cà/ ớt)', 219000, 'https://static.kfcvietnam.com.vn/images/items/lg/DBUCKET2.jpg?v=gk7XPg', 1),
(24, N'Combo Nhóm 5 Hội Tụ', N'6 Miếng Gà Rán + 1 Mì Ý Gà Viên + 1 Khoai Múi Cau (Vừa) + 5 Ly Pepsi (Tiêu chuẩn) + 8 Gói tương (cà/ ớt)', 309000, 'https://static.kfcvietnam.com.vn/images/items/lg/DBUCKET3.jpg?v=gk7XPg', 1);
SET IDENTITY_INSERT [combo] OFF;

-- 6. INSERT COMBO ITEMS
INSERT INTO combo_item (combo_id, product_id, quantity) VALUES 
(1, 1, 1), (1, 28, 1), (1, 50, 1), (2, 2, 1), (2, 28, 1), (2, 50, 1), (3, 11, 1), (3, 23, 1), (3, 59, 1),
(4, 5, 1), (4, 37, 1), (4, 59, 1), (5, 20, 1), (5, 50, 1), (6, 21, 1), (6, 50, 1), (7, 20, 1), (7, 11, 1),
(7, 28, 1), (7, 50, 1), (8, 20, 1), (8, 24, 1), (8, 50, 1), (9, 17, 1), (9, 50, 1), (10, 18, 1), (10, 50, 1),
(11, 22, 1), (11, 50, 1), (12, 22, 1), (12, 39, 1), (12, 50, 1), (13, 14, 1), (13, 50, 1), (14, 13, 1),
(14, 28, 1), (14, 50, 1), (15, 15, 1), (15, 28, 1), (15, 50, 1), (16, 14, 1), (16, 1, 1), (16, 50, 1),
(17, 15, 1), (17, 1, 1), (17, 50, 1), (18, 13, 1), (18, 1, 1), (18, 50, 1), (19, 13, 1), (19, 28, 1),
(19, 1, 1), (19, 50, 1), (20, 2, 1), (20, 13, 1), (20, 50, 2), (21, 3, 1), (21, 20, 1), (21, 50, 2),
(22, 1, 4), (22, 31, 1), (22, 50, 2), (23, 3, 1), (23, 20, 1), (23, 14, 1), (23, 28, 1), (23, 50, 3),
(24, 4, 1), (24, 20, 1), (24, 31, 1), (24, 50, 5);

-- 7. INSERT STAFF USERS
SET IDENTITY_INSERT [user] ON;
INSERT INTO [user] (id, name, username, password_hash, role, active, must_change_password) VALUES
(1, N'Admin User', 'admin', '$2a$11$Y8iC0D.lCaFFEcGstxcJoOGe1z/Pl3fOQQjeZp4mcLBQBvmoK0BbK', 'admin', 1, 0),
(2, N'Staff User', 'staff', '$2a$11$Y8iC0D.lCaFFEcGstxcJoOGe1z/Pl3fOQQjeZp4mcLBQBvmoK0BbK', 'staff', 1, 0),
(3, N'Kitchen User', 'kitchen', '$2a$11$Y8iC0D.lCaFFEcGstxcJoOGe1z/Pl3fOQQjeZp4mcLBQBvmoK0BbK', 'kitchen', 1, 0);
SET IDENTITY_INSERT [user] OFF;
GO