-- Generated SQL INSERT statements (WITH ID COLUMNS)
-- Database Type: MySQL
-- Generated at: 2025-06-18 16:33:19
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

INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_001_sbm', 'CODE_62342_001', '78324 Isaiah Expressway', '522-361-9502', 'Tyree31@hotmail.com', 'https://nicolette.info', 'Organic stable knowledge user', 1, 0, 1);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_002_b08', 'CODE_81253_002', '20676 Mann Key', '803-726-8458', 'Jace_Metz8@gmail.com', 'http://phoebe.info', 'Team-oriented 6th generation flexibility', 930, 0, 2);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_003_q80', 'CODE_99416_003', '528 Robel Grove', '008-687-6396', 'Sheldon_Bogisich@gmail.com', 'http://brandon.com', 'Enhanced 24/7 open architecture', 995, 1, 3);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Coralie Lowe', 'member_001_iaz', 'Sit soluta sed.', '["delete","update"]', 10, 1, 1);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Ulises Borer', 'member_002_w3i', 'Cum et vitae.', '["delete","read","create"]', 4, 1, 2);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Garth Zboncak', 'member_003_kit', 'Possimus voluptatem autem.', '["admin","view"]', 10, 1, 3);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Elaina62', 'Maci71@gmail.com', 'earum_1', 'Phương_001_fgg', 'Phương_001_341', '492-773-1508', '501 Hayes Fork', '1989-11-22 00:00:00', 'Female', 'http://alphonso.info', 2, 2, 107194.19, '2019-11-20 22:20:25', 'Industrial & Jewelery', 0, '2024-09-05 13:07:15', 1);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Rex15', 'Lela.Schneider62@hotmail.com', 'illum_2', 'Phương_002_h8p', 'Phương_002_9hi', '899-659-9797', '401 Christelle Union', '1989-03-13 00:00:00', 'Female', 'https://irma.name', 3, 3, 180245.43, '2016-03-31 09:59:28', 'Grocery, Health & Clothing', 0, '2024-11-08 20:13:38', 2);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Vincenzo13', 'Hellen_Balistreri12@hotmail.com', 'nihil_3', 'Phương_003_rr0', 'Phương_003_jmm', '225-839-4206', '17754 Schuppe Court', '1989-01-22 00:00:00', 'Male', 'http://emely.org', 1, 1, 42626.03, '2017-10-14 21:21:29', 'Grocery, Beauty & Automotive', 0, '2025-02-21 18:42:11', 3);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (2, 2, 2, '2025-02-24 08:19:42', 0, 1);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (3, 3, 3, '2024-07-08 02:11:48', 0, 2);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (1, 1, 1, '2025-01-15 16:49:35', 0, 3);

-- Re-enable foreign key checks
SET FOREIGN_KEY_CHECKS = 1;

-- COMMIT;
-- End of generated SQL WITH IDs (12 statements)
