-- Generated SQL INSERT statements (WITH ID COLUMNS)
-- Database Type: MySQL
-- Generated at: 2025-06-18 16:22:10
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

INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_001_kzg', 'CODE_8765_001', '707 Lulu Circle', '882-429-5107', 'Clementina_Koepp@gmail.com', 'http://abel.com', 'Cross-platform actuating data-warehouse', 325, 1, 1);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_002_ev1', 'CODE_29735_002', '627 Senger Via', '064-923-0777', 'Marion63@gmail.com', 'https://andre.name', 'Ergonomic optimal concept', 928, 1, 2);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_003_8ic', 'CODE_48944_003', '639 Marcella Knolls', '214-342-6891', 'Candido_Fisher@hotmail.com', 'http://merle.com', 'Enterprise-wide incremental groupware', 327, 1, 3);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_004_rgf', 'CODE_67501_004', '302 Bednar Creek', '336-089-9848', 'Sandra_Torphy@gmail.com', 'http://dolores.com', 'Focused 4th generation Graphic Interface', 524, 0, 4);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_005_dr5', 'CODE_85700_005', '496 Alessandro Streets', '415-740-2199', 'Prince_Tillman46@yahoo.com', 'http://adela.info', 'Cross-platform web-enabled data-warehouse', 617, 1, 5);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_006_dgs', 'CODE_7681_006', '25880 Kling Walks', '833-960-4478', 'Lura.Mraz@gmail.com', 'https://nicholaus.name', 'Adaptive radical website', 283, 0, 6);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_007_yew', 'CODE_37907_007', '631 Jakayla Green', '107-272-8988', 'Velva10@gmail.com', 'http://arvid.org', 'Implemented executive task-force', 206, 1, 7);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_008_870', 'CODE_50284_008', '233 Guy Village', '355-786-7637', 'Rodger.Kassulke@yahoo.com', 'https://edmund.com', 'Triple-buffered fresh-thinking hub', 115, 1, 8);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_009_20p', 'CODE_59881_009', '45202 Nader Points', '320-988-1113', 'Coleman.Ryan63@gmail.com', 'http://cindy.org', 'Integrated impactful paradigm', 687, 1, 9);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_010_wu1', 'CODE_68982_010', '4003 Hettinger Junctions', '187-520-2454', 'Alycia.Morar@gmail.com', 'https://lois.name', 'Quality-focused attitude-oriented knowledge base', 83, 1, 10);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Kylee Ebert', 'member_001_tkl', 'Quod est voluptatum.', '["delete","read"]', 3, 0, 1);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Brian Schmidt', 'member_002_zin', 'Cum iusto eaque.', '["admin","create","view"]', 6, 1, 2);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Carolyne Toy', 'member_003_fq0', 'Qui molestias ut.', '["update","delete","write"]', 6, 1, 3);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Dashawn Rosenbaum', 'member_004_ne5', 'Illo maiores qui.', '["edit","write","update"]', 6, 1, 4);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Nelda Howe', 'member_005_1z1', 'Nesciunt ab iure.', '["view"]', 4, 1, 5);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Davion Kertzmann', 'member_006_xvb', 'Fuga sunt possimus.', '["create","read","admin"]', 5, 1, 6);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Leonard Langworth', 'member_007_60i', 'Saepe rem officia.', '["edit","view","create"]', 6, 1, 7);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Naomi Gaylord', 'member_008_1tf', 'Omnis culpa laborum.', '["view","edit"]', 1, 1, 8);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Chelsea Kuhic', 'member_009_k8x', 'In totam vel.', '["view","admin","read"]', 8, 1, 9);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Karl Anderson', 'member_010_n7z', 'Mollitia totam est.', '["admin","edit"]', 8, 0, 10);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Joey_Howe', 'Isidro3@gmail.com', 'sequi_1', 'Phương_001_ayd', 'Phương_001_qg3', '732-119-5565', '32655 Jordane Point', '1989-06-05 00:00:00', 'Male', 'https://bernardo.net', 2, 2, 127585.93, '2022-02-28 01:55:23', 'Tools, Grocery & Books', 0, '2025-02-14 20:01:27', 1);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Gage_Olson', 'Marcelino_Morissette@hotmail.com', 'consequatur_2', 'Phương_002_usx', 'Phương_002_jbc', '241-550-2924', '76927 Hills Centers', '1989-11-30 00:00:00', 'Other', 'http://richmond.info', 3, 3, 39450.45, '2018-05-05 09:01:51', 'Tools, Home & Home', 0, '2024-07-22 13:05:23', 2);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Kaleigh_Watsica89', 'Bell19@yahoo.com', 'cumque_3', 'Phương_003_gs1', 'Phương_003_kt4', '244-413-2185', '23654 Thurman Common', '1989-04-07 00:00:00', 'Male', 'https://breanne.info', 4, 4, 124429.04, '2017-03-30 20:04:08', 'Games & Health', 0, '2024-09-19 02:12:04', 3);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Dasia20', 'Quinten.Hand37@gmail.com', 'consequatur_4', 'Phương_004_t1h', 'Phương_004_7ub', '412-022-4024', '9319 Ignacio Hills', '1989-04-04 00:00:00', 'Other', 'http://elvis.info', 5, 5, 143450.00, '2018-10-14 23:17:32', 'Outdoors & Computers', 0, '2024-12-24 01:33:00', 4);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Sabrina.Jast', 'Laurie_Grant66@hotmail.com', 'inventore_5', 'Phương_005_8fx', 'Phương_005_0eb', '790-652-2311', '5564 Lehner Trail', '1989-09-24 00:00:00', 'Other', 'http://alfreda.biz', 6, 6, 145867.27, '2019-01-18 10:57:50', 'Movies & Music', 0, '2024-08-15 17:09:52', 5);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Garnett82', 'Florida.Friesen19@gmail.com', 'harum_6', 'Phương_006_lhb', 'Phương_006_d1b', '710-618-6133', '204 Becker Mission', '1989-02-19 00:00:00', 'Female', 'http://jarred.name', 7, 7, 31517.02, '2019-03-28 22:19:32', 'Grocery & Industrial', 0, '2025-05-12 23:22:59', 6);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Kayleigh26', 'Blair_Ledner98@yahoo.com', 'mollitia_7', 'Phương_007_72b', 'Phương_007_78k', '252-202-3072', '9389 Marlin Landing', '1989-02-26 00:00:00', 'Male', 'http://mariam.name', 8, 8, 44460.37, '2021-09-12 13:02:38', 'Automotive & Shoes', 0, '2025-06-01 01:35:49', 7);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Hosea.Nienow', 'Aubrey.Waters67@yahoo.com', 'inventore_8', 'Phương_008_z93', 'Phương_008_jye', '395-563-0349', '303 Mack Forges', '1989-09-09 00:00:00', 'Other', 'https://gaetano.info', 9, 9, 75656.55, '2017-05-13 19:23:25', 'Kids, Computers & Toys', 0, '2025-05-31 22:18:14', 8);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Julianne55', 'Sasha_Graham81@hotmail.com', 'ut_9', 'Phương_009_ne9', 'Phương_009_34s', '843-123-7887', '9576 Deon Mall', '1989-05-02 00:00:00', 'Other', 'https://oliver.org', 10, 10, 154981.97, '2019-12-14 11:37:52', 'Automotive & Home', 0, '2024-12-13 07:38:09', 9);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Ignatius.Waters', 'Eloisa.Turcotte@hotmail.com', 'earum_10', 'Phương_010_au2', 'Phương_010_vmt', '888-454-9089', '61601 Orn Junction', '1989-12-08 00:00:00', 'Other', 'http://elvis.org', 1, 1, 173592.63, '2024-01-10 04:02:31', 'Electronics, Shoes & Clothing', 0, '2025-02-17 04:36:54', 10);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (2, 2, 2, '2024-08-02 08:35:06', 0, 1);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (3, 3, 3, '2024-07-29 06:14:22', 0, 2);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (4, 4, 4, '2025-06-16 06:47:54', 0, 3);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (5, 5, 5, '2024-06-29 18:30:40', 0, 4);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (6, 6, 6, '2025-05-15 05:50:12', 0, 5);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (7, 7, 7, '2025-03-18 13:05:41', 0, 6);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (8, 8, 8, '2024-12-10 10:14:40', 0, 7);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (9, 9, 9, '2024-09-27 09:06:13', 0, 8);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (10, 10, 10, '2024-07-10 15:48:22', 0, 9);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (1, 1, 1, '2024-11-13 07:49:40', 0, 10);

-- Re-enable foreign key checks
SET FOREIGN_KEY_CHECKS = 1;

-- COMMIT;
-- End of generated SQL WITH IDs (40 statements)
