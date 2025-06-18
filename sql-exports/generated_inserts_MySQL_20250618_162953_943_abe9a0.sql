-- Generated SQL INSERT statements (WITH ID COLUMNS)
-- Database Type: MySQL
-- Generated at: 2025-06-18 16:29:53
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

INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_001_m58', 'CODE_56019_001', '2951 Krista Isle', '121-814-2111', 'Ocie.West48@yahoo.com', 'http://akeem.org', 'Configurable fault-tolerant internet solution', 75, 0, 1);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_002_42a', 'CODE_14707_002', '55264 Suzanne Prairie', '085-891-5459', 'Marisa.Schulist51@hotmail.com', 'http://arnaldo.name', 'Down-sized grid-enabled middleware', 704, 1, 2);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_003_jpq', 'CODE_68033_003', '9760 Rogahn Mount', '190-108-8596', 'Ashleigh10@gmail.com', 'http://terrance.name', 'Diverse radical conglomeration', 844, 1, 3);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_004_u2s', 'CODE_17610_004', '9480 Ward Landing', '569-051-9541', 'Tracey_Blanda@gmail.com', 'https://clay.info', 'Optional methodical software', 854, 1, 4);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_005_2sa', 'CODE_63458_005', '836 Frami Crescent', '135-376-0762', 'Sedrick.Greenholt@yahoo.com', 'http://breanna.com', 'Reactive full-range application', 334, 1, 5);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_006_f1h', 'CODE_15115_006', '3695 Lurline Union', '848-842-3225', 'Arnulfo_King3@yahoo.com', 'https://nicolas.net', 'Horizontal composite interface', 848, 1, 6);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_007_q7s', 'CODE_64647_007', '5672 Marguerite Trail', '199-655-5655', 'Jared.Terry@yahoo.com', 'https://sanford.biz', 'Cloned client-server Graphic Interface', 445, 1, 7);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_008_jy8', 'CODE_13834_008', '8187 Brigitte Isle', '228-481-6979', 'Thomas_Gleason41@hotmail.com', 'http://gudrun.info', 'Re-contextualized tertiary algorithm', 835, 0, 8);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_009_oyu', 'CODE_73372_009', '4992 Davis Parks', '063-560-5203', 'Elbert11@hotmail.com', 'https://nettie.com', 'Re-engineered motivating forecast', 711, 1, 9);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_010_xov', 'CODE_6894_010', '358 Harber Freeway', '133-711-9128', 'Ciara.Bins72@yahoo.com', 'http://hermann.com', 'Integrated actuating solution', 448, 1, 10);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Jackeline Bergnaum', 'member_001_8ov', 'Sint consequatur rerum.', '["update","create"]', 8, 1, 1);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Matteo Howell', 'member_002_rbc', 'Eos quia et.', '["view","read","delete"]', 4, 0, 2);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Furman Wisoky', 'member_003_34f', 'Officia in soluta.', '["view"]', 7, 1, 3);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Elian Lockman', 'member_004_7w4', 'Magnam eaque dolorem.', '["update","delete"]', 6, 1, 4);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Pink Krajcik', 'member_005_28t', 'Iste voluptas quasi.', '["admin","edit"]', 7, 1, 5);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Chance Keeling', 'member_006_oar', 'Quaerat quos iste.', '["write","update"]', 4, 1, 6);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Emmitt Borer', 'member_007_wd8', 'Amet et non.', '["admin","update","write","view"]', 2, 0, 7);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Kyra Hoppe', 'member_008_tvu', 'Hic quibusdam aut.', '["view","create","delete","update"]', 7, 1, 8);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Deon Morissette', 'member_009_dqw', 'Quo dignissimos labore.', '["read","edit","admin"]', 4, 0, 9);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Lavinia Kris', 'member_010_irr', 'Sed assumenda dolor.', '["write","admin","update"]', 5, 0, 10);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Ola_Hintz61', 'Tyson35@yahoo.com', 'harum_1', 'Phương_001_h6l', 'Phương_001_0lk', '942-202-9118', '020 Manuela Avenue', '1989-12-24 00:00:00', 'Male', 'https://aditya.info', 2, 2, 157987.49, '2017-06-04 23:38:03', 'Beauty', 0, '2025-06-01 06:35:38', 1);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Arely_Bayer10', 'Erwin36@yahoo.com', 'modi_2', 'Phương_002_yiq', 'Phương_002_f1s', '980-116-5402', '430 Hansen Walks', '1989-04-19 00:00:00', 'Male', 'https://kristoffer.net', 3, 3, 185944.60, '2023-07-12 04:02:07', 'Garden, Outdoors & Sports', 0, '2025-04-18 22:39:28', 2);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Hazle82', 'Trevion52@hotmail.com', 'labore_3', 'Phương_003_kr9', 'Phương_003_avr', '477-469-8134', '961 Lind Rest', '1989-04-10 00:00:00', 'Other', 'http://rossie.name', 4, 4, 70201.81, '2019-12-05 08:34:38', 'Health', 0, '2025-03-31 04:36:46', 3);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Craig28', 'Tyrese_Kutch94@hotmail.com', 'vel_4', 'Phương_004_48w', 'Phương_004_98r', '819-629-8903', '84768 Kariane Forges', '1989-08-09 00:00:00', 'Female', 'http://linnea.biz', 5, 5, 133864.98, '2023-01-21 07:01:31', 'Garden & Computers', 0, '2024-07-30 22:22:13', 4);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Jed_Kertzmann25', 'Alexane.Russel13@gmail.com', 'veniam_5', 'Phương_005_yob', 'Phương_005_u5n', '978-685-3405', '76947 Ruth Forge', '1989-10-22 00:00:00', 'Female', 'http://vickie.biz', 6, 6, 162262.05, '2018-02-11 03:59:27', 'Garden & Clothing', 0, '2025-05-11 16:05:24', 5);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Nicklaus.Pfannerstill32', 'Carolyn.Shanahan88@hotmail.com', 'rerum_6', 'Phương_006_gud', 'Phương_006_7ku', '630-593-3515', '4457 Adriel Shoal', '1989-06-15 00:00:00', 'Male', 'https://ashly.name', 7, 7, 158781.69, '2021-07-02 20:41:27', 'Grocery & Books', 0, '2024-11-11 17:05:55', 6);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Tamara_Yundt77', 'Eliseo_Kerluke43@gmail.com', 'facilis_7', 'Phương_007_ifd', 'Phương_007_weq', '937-004-9709', '7204 Kub Port', '1989-01-04 00:00:00', 'Other', 'http://leora.biz', 8, 8, 174155.38, '2024-07-03 16:22:50', 'Outdoors', 0, '2025-01-15 21:16:08', 7);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Carmela41', 'Norris80@yahoo.com', 'blanditiis_8', 'Phương_008_w69', 'Phương_008_o2q', '948-651-3940', '16543 Abdul Trafficway', '1989-10-14 00:00:00', 'Male', 'http://cletus.name', 9, 9, 195149.22, '2016-09-23 22:46:03', 'Health & Outdoors', 0, '2024-12-08 17:15:01', 8);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Aron8', 'Hortense77@yahoo.com', 'soluta_9', 'Phương_009_ean', 'Phương_009_y8m', '456-051-4310', '5759 Avery River', '1989-12-11 00:00:00', 'Male', 'https://lilly.net', 10, 10, 169400.13, '2018-10-13 11:07:47', 'Jewelery & Industrial', 0, '2025-05-19 09:36:31', 9);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Elroy63', 'Hoyt.Wisoky49@hotmail.com', 'esse_10', 'Phương_010_vl2', 'Phương_010_ayg', '460-647-5641', '207 Gislason Walk', '1989-02-28 00:00:00', 'Male', 'https://soledad.org', 1, 1, 126924.91, '2019-12-16 04:34:48', 'Automotive & Automotive', 0, '2024-09-11 02:37:20', 10);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (2, 2, 2, '2024-08-07 05:20:21', 0, 1);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (3, 3, 3, '2025-05-19 07:21:11', 0, 2);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (4, 4, 4, '2024-10-02 01:04:36', 0, 3);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (5, 5, 5, '2024-06-28 21:53:40', 0, 4);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (6, 6, 6, '2024-09-21 01:46:41', 0, 5);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (7, 7, 7, '2025-01-31 21:48:04', 0, 6);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (8, 8, 8, '2024-08-27 20:56:25', 0, 7);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (9, 9, 9, '2025-04-01 21:52:46', 0, 8);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (10, 10, 10, '2024-10-05 09:18:00', 0, 9);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (1, 1, 1, '2024-07-21 19:29:11', 0, 10);

-- Re-enable foreign key checks
SET FOREIGN_KEY_CHECKS = 1;

-- COMMIT;
-- End of generated SQL WITH IDs (40 statements)
