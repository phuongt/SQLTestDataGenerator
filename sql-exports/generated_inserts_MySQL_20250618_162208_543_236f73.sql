-- Generated SQL INSERT statements (WITH ID COLUMNS)
-- Database Type: MySQL
-- Generated at: 2025-06-18 16:22:08
-- Total statements: 40
-- Note: ID columns included with sequential values (1,2,3...)

-- Execute all statements in a transaction:
-- BEGIN TRANSACTION;

-- Disable foreign key checks temporarily
SET FOREIGN_KEY_CHECKS = 0;

-- Reset auto_increment to start from 1 for all tables
ALTER TABLE `companies` AUTO_INCREMENT = 1;
ALTER TABLE `roles` AUTO_INCREMENT = 1;
ALTER TABLE `users` AUTO_INCREMENT = 1;
ALTER TABLE `user_roles` AUTO_INCREMENT = 1;

INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_001_n3e', 'CODE_55604_001', '757 Yoshiko Court', '156-829-8759', 'Wilber.Schulist@hotmail.com', 'http://marjorie.info', 'Multi-layered optimizing focus group', 801, 1, 1);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_002_nef', 'CODE_2772_002', '4117 Keeling Hill', '541-369-2205', 'Sierra.Rosenbaum49@hotmail.com', 'https://annalise.org', 'User-centric interactive open system', 12, 1, 2);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_003_432', 'CODE_48598_003', '8028 Kaylee Mountain', '663-883-7890', 'Augustus32@yahoo.com', 'https://johnson.name', 'Up-sized eco-centric paradigm', 806, 1, 3);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_004_oyw', 'CODE_95339_004', '09192 Kuhn Circles', '152-982-8670', 'Eula.Feil@gmail.com', 'https://krystal.name', 'Right-sized executive throughput', 667, 1, 4);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_005_nu4', 'CODE_22901_005', '826 Haley Squares', '678-733-1516', 'Jerad_Heidenreich48@yahoo.com', 'https://britney.net', 'Compatible interactive info-mediaries', 740, 0, 5);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_006_tou', 'CODE_49623_006', '79179 Aufderhar Hill', '783-872-9066', 'Federico84@gmail.com', 'http://pierce.info', 'Quality-focused background ability', 117, 1, 6);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_007_3ey', 'CODE_96894_007', '337 Little Street', '002-754-8436', 'Antonia_Beatty33@gmail.com', 'https://travon.info', 'Cross-platform multimedia circuit', 976, 1, 7);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_008_64n', 'CODE_40633_008', '48611 Ward Garden', '291-041-1724', 'Penelope_Emmerich75@hotmail.com', 'http://scot.name', 'Front-line clear-thinking flexibility', 154, 1, 8);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_009_3qh', 'CODE_88462_009', '491 Christophe Prairie', '496-502-6894', 'Kieran.Klein@yahoo.com', 'https://marian.com', 'Grass-roots human-resource initiative', 220, 1, 9);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_010_luz', 'CODE_12117_010', '4910 Barney Throughway', '088-553-8381', 'Samara_Boyer37@yahoo.com', 'https://rosemary.com', 'Function-based fresh-thinking migration', 729, 1, 10);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Russel Kozey', 'member_001_9cn', 'Ea ipsum enim.', '["write","edit"]', 9, 1, 1);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Ada Denesik', 'member_002_x3z', 'Labore labore libero.', '["create","write","view"]', 3, 1, 2);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Jamir Smitham', 'member_003_lqo', 'Omnis accusamus cupiditate.', '["admin","write","update"]', 2, 1, 3);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Theron Metz', 'member_004_n2b', 'Eveniet occaecati est.', '["create","read"]', 6, 1, 4);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Maurice Luettgen', 'member_005_htu', 'Vitae quo quae.', '["write","view"]', 3, 1, 5);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Loraine Konopelski', 'member_006_hxq', 'In eligendi ut.', '["create","read","edit"]', 9, 1, 6);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Abigail Gulgowski', 'member_007_5nc', 'Ut fugit dicta.', '["admin"]', 4, 0, 7);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Bertrand OConner', 'member_008_0hm', 'Nulla dolorem et.', '["view","write","read"]', 8, 1, 8);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Nedra Hessel', 'member_009_f9j', 'Dolor unde voluptas.', '["update"]', 3, 1, 9);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Addison VonRueden', 'member_010_cbo', 'Voluptates quidem voluptas.', '["delete","read","edit"]', 7, 1, 10);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Delaney.Kerluke', 'Forest44@hotmail.com', 'voluptas_1', 'Phương_001_910', 'Phương_001_hfi', '389-711-1447', '184 Darrion Springs', '1989-01-16 00:00:00', 'Male', 'http://julie.org', 2, 2, 165636.30, '2019-03-31 02:54:39', 'Jewelery', 0, '2024-10-28 02:26:12', 1);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Hilario_Graham', 'Johann.Kessler@gmail.com', 'est_2', 'Phương_002_toe', 'Phương_002_w54', '240-385-9384', '33843 Shanie Ports', '1989-10-01 00:00:00', 'Male', 'https://jailyn.org', 3, 3, 35193.73, '2017-03-12 04:23:37', 'Clothing & Tools', 0, '2024-07-01 03:07:03', 2);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Trever_Gleichner69', 'Everett_Schimmel@gmail.com', 'doloribus_3', 'Phương_003_s70', 'Phương_003_vrq', '269-752-2947', '570 Murazik Cape', '1989-03-18 00:00:00', 'Other', 'http://nash.org', 4, 4, 158844.78, '2024-06-20 10:26:10', 'Sports, Grocery & Garden', 0, '2025-02-05 22:21:48', 3);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Nico.Nikolaus89', 'Jennyfer87@hotmail.com', 'nesciunt_4', 'Phương_004_lym', 'Phương_004_jvy', '581-327-2836', '430 Sipes Mission', '1989-10-09 00:00:00', 'Female', 'https://kamille.org', 5, 5, 118778.31, '2022-09-03 01:15:07', 'Electronics & Automotive', 0, '2025-06-03 04:55:34', 4);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Roxanne9', 'Terry52@hotmail.com', 'ullam_5', 'Phương_005_64k', 'Phương_005_9xw', '854-125-9029', '7904 Stamm Spurs', '1989-10-28 00:00:00', 'Female', 'https://alan.name', 6, 6, 101352.18, '2015-08-29 02:10:23', 'Grocery, Toys & Tools', 0, '2025-01-19 18:30:00', 5);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Modesto0', 'Chyna57@yahoo.com', 'adipisci_6', 'Phương_006_o7j', 'Phương_006_wfh', '123-647-0816', '75960 Heller Isle', '1989-03-16 00:00:00', 'Female', 'https://ethelyn.name', 7, 7, 59256.51, '2023-02-17 12:12:11', 'Automotive', 0, '2024-10-18 00:05:00', 6);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Audra_OHara13', 'Wilmer.Rodriguez5@yahoo.com', 'aut_7', 'Phương_007_e8v', 'Phương_007_a4q', '871-604-5551', '192 Hirthe Isle', '1989-11-17 00:00:00', 'Male', 'http://candelario.net', 8, 8, 175831.82, '2015-08-21 18:55:41', 'Toys', 0, '2024-10-30 06:55:14', 7);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Beth.Roob37', 'Sammy.Ortiz@hotmail.com', 'molestias_8', 'Phương_008_bac', 'Phương_008_rds', '545-732-3404', '2688 Ryley Forks', '1989-12-04 00:00:00', 'Male', 'https://brandon.biz', 9, 9, 180247.87, '2019-07-06 02:03:45', 'Outdoors', 0, '2025-02-06 08:56:01', 8);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Dakota.Donnelly', 'Bonnie32@hotmail.com', 'expedita_9', 'Phương_009_pwu', 'Phương_009_ebn', '829-333-1929', '2230 Alexandro Springs', '1989-11-02 00:00:00', 'Other', 'https://rhianna.org', 10, 10, 138904.35, '2020-10-25 21:04:24', 'Baby', 0, '2025-05-02 14:15:18', 9);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Odie_Daugherty48', 'Sean_Hand45@hotmail.com', 'possimus_10', 'Phương_010_s3n', 'Phương_010_9xi', '470-242-6768', '45413 Willms River', '1989-02-10 00:00:00', 'Female', 'https://leora.biz', 1, 1, 178542.79, '2020-05-29 22:17:25', 'Health', 0, '2024-11-03 01:12:25', 10);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (2, 2, 2, '2024-11-14 07:54:53', 0, 1);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (3, 3, 3, '2025-05-02 01:36:01', 0, 2);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (4, 4, 4, '2024-10-08 12:26:53', 0, 3);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (5, 5, 5, '2025-04-07 09:04:22', 0, 4);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (6, 6, 6, '2024-08-13 21:09:44', 0, 5);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (7, 7, 7, '2024-08-08 15:22:36', 0, 6);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (8, 8, 8, '2025-02-10 23:36:24', 0, 7);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (9, 9, 9, '2024-08-01 00:49:38', 0, 8);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (10, 10, 10, '2025-01-12 21:00:21', 0, 9);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (1, 1, 1, '2025-04-24 00:14:51', 0, 10);

-- Re-enable foreign key checks
SET FOREIGN_KEY_CHECKS = 1;

-- COMMIT;
-- End of generated SQL WITH IDs (40 statements)
