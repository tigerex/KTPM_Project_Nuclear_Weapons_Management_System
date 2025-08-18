create database nuclear_weapon;

use nuclear_weapon;

-- Vũ khí
CREATE TABLE weapons (
    weapon_id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,			
    type VARCHAR(50) NOT NULL,			-- loại (ICBM, bắn từ tàu ngầm, bắn từ quỹ đạo,...)
    yield_megatons DECIMAL(6,2),       	-- độ boom boom
    range_km INT,                      	-- tầm bắn
    weight_kg INT,						-- cân nặng (no use for now)
    status ENUM('Active', 'Decommissioned', 'Prototype') DEFAULT 'Prototype',
    country_of_origin VARCHAR(100),		-- quốc gia xuất sứ
    year_craeted INT,					-- năm tạo ra
    notes TEXT							-- nếu có
);

-- Kho chứa
CREATE TABLE storages (
    storage_id INT AUTO_INCREMENT PRIMARY KEY,
    location_name VARCHAR(150) NOT NULL DEFAULT 'REDACTED',	-- tên cái chỗ đó
    latitude DECIMAL(9,6),		-- kinh độ (-90 90)
    longitude DECIMAL(9,6),		-- vĩ độ (-180 180)
    last_inspection DATE		-- này xàm xàm thôi kệ đi
);

-- Chỗ nào chứa cái gì
CREATE TABLE storage_inventory (
    inventory_id INT AUTO_INCREMENT PRIMARY KEY,
    storage_id INT NOT NULL,
    weapon_id INT NOT NULL,
    quantity INT DEFAULT 1,
    FOREIGN KEY (storage_id) REFERENCES storages(storage_id),
    FOREIGN KEY (weapon_id) REFERENCES weapons(weapon_id)
);


CREATE TABLE users (
    user_id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL UNIQUE,  -- mmã hash từ password
    full_name VARCHAR(100) NOT NULL DEFAULT 'REDACTED',	 -- tên
    role VARCHAR(100) NOT NULL DEFAULT 'REDACTED', -- chức danh
    country VARCHAR(100), -- quốc gia
    organization VARCHAR(150), -- tổ chức
    clearance_level ENUM('Low', 'Medium', 'High', 'Ultra-Secret') DEFAULT 'Low', -- quyền hạng
    is_admin BOOLEAN DEFAULT false, -- admin thì làm được nhiều thứ
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_login TIMESTAMP NULL
);

INSERT INTO weapons (name, type, yield_megatons, range_km, weight_kg, status, country_of_origin, year_craeted, notes)
VALUES
('Tsar Bomba', 'Gravity Bomb', 50.00, 0, 27000, 'Decommissioned', 'Soviet Union', 1961, 'Largest nuke ever detonated.'),
('Minuteman III', 'ICBM', 0.35, 13000, 36000, 'Active', 'USA', 1970, 'Current US land-based ICBM.'),
('DF-41', 'ICBM', 5.00, 15000, 55000, 'Active', 'China', 2017, 'Chinese road-mobile ICBM.'),
('Poseidon', 'Nuclear Torpedo', 2.00, 10000, 40000, 'Prototype', 'Russia', 2020, 'Underwater doomsday drone.'),
('Little Boy', 'Gravity Bomb', 0.015, 0, 4400, 'Decommissioned', 'USA', 1945, 'Dropped on Hiroshima.');

INSERT INTO storages (location_name, latitude, longitude, last_inspection)
VALUES
('Secret Volcano Base', 15.3910, 120.7730, '2025-01-01'),
('Arctic Submarine Dock', 78.2232, 15.6469, '2025-01-15');

-- Volcano Base stockpile
INSERT INTO storage_inventory (storage_id, weapon_id, quantity)
VALUES
(1, 1, 1),  -- 1 Tsar Bomba
(1, 2, 5);  -- 5 Minuteman III

-- Arctic Dock stockpile
INSERT INTO storage_inventory (storage_id, weapon_id, quantity)
VALUES
(2, 3, 3),  -- 3 DF-41
(2, 4, 2),  -- 2 Poseidon
(2, 5, 1);  -- 1 Little Boy

-- test querry
SELECT 
    s.storage_id,
    s.location_name,
    s.latitude,
    s.longitude,
    w.weapon_id,
    w.name AS weapon_name,
    w.type,
    si.quantity,
    s.last_inspection
FROM storage_inventory si
JOIN storages s ON si.storage_id = s.storage_id
JOIN weapons w ON si.weapon_id = w.weapon_id
ORDER BY s.location_name, w.name;


SELECT * from weapons;
SELECT * from storages;
SELECt * from storage_inventory;


show tables;

drop table weapons;
drop table storage;
drop table users;


