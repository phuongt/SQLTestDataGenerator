-- Generated SQL INSERT statements (WITH ID COLUMNS)
-- Database Type: MySQL
-- Generated at: 2025-06-18 16:33:21
-- Total statements: 12
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

INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_001_wp8', 'CODE_84525_001', '30135 Murazik Station', '727-964-5607', 'Erna32@yahoo.com', 'http://marcellus.org', 'Customer-focused disintermediate workforce', 336, 0, 1);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_002_3ut', 'CODE_3343_002', '547 Torp Keys', '699-764-7656', 'Dale_Keeling@yahoo.com', 'https://aimee.com', 'Diverse composite superstructure', 748, 1, 2);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_003_fqn', 'CODE_21718_003', '363 Arnoldo Mission', '559-155-7697', 'Verlie37@gmail.com', 'http://abel.com', 'Stand-alone 3rd generation attitude', 19, 0, 3);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Lavada Dickens', 'member_001_43c', 'Reiciendis sapiente voluptas.', '["edit"]', 3, 0, 1);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Aleen Runte', 'member_002_v7u', 'Saepe repudiandae voluptates.', '["write","admin"]', 2, 1, 2);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Harley Hilll', 'member_003_zp1', 'Nulla totam sequi.', '["update","read","write","admin"]', 1, 1, 3);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Alessandro.Harvey47', 'Jeromy_Rippin@yahoo.com', 'sunt_1', 'Phương_001_i92', 'Phương_001_1vw', '885-761-4131', '7352 Lavina Islands', '1989-09-21 00:00:00', 'Male', 'http://ebony.com', 2, 2, 133727.47, '2015-12-30 10:38:14', 'Movies', 0, '2025-01-11 07:36:55', 1);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Nola50', 'Paris_Hahn@hotmail.com', 'quaerat_2', 'Phương_002_skz', 'Phương_002_6ot', '908-914-2694', '014 Dietrich Throughway', '1989-01-10 00:00:00', 'Male', 'https://jake.info', 3, 3, 92945.93, '2015-12-30 19:14:54', 'Garden & Movies', 0, '2024-09-08 18:32:56', 2);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Viola19', 'Denis59@hotmail.com', 'vel_3', 'Phương_003_juf', 'Phương_003_xqj', '808-064-7818', '34104 Montana Lodge', '1989-01-19 00:00:00', 'Other', 'https://penelope.biz', 1, 1, 63186.65, '2019-10-03 23:06:14', 'Health, Tools & Industrial', 0, '2025-01-11 22:32:38', 3);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (2, 2, 2, '2024-07-12 20:33:17', 0, 1);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (3, 3, 3, '2024-09-06 19:53:16', 0, 2);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (1, 1, 1, '2025-04-03 23:35:33', 0, 3);

-- Re-enable foreign key checks
SET FOREIGN_KEY_CHECKS = 1;

-- COMMIT;
-- End of generated SQL WITH IDs (12 statements)
