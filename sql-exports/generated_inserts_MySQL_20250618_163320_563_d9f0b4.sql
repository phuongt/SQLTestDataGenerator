-- Generated SQL INSERT statements (WITH ID COLUMNS)
-- Database Type: MySQL
-- Generated at: 2025-06-18 16:33:20
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

INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_001_mdy', 'CODE_49857_001', '73807 Brekke Plain', '344-971-6714', 'Raina.Hauck9@yahoo.com', 'http://louisa.name', 'Centralized local benchmark', 650, 0, 1);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_002_9rh', 'CODE_68499_002', '9283 Conn Mills', '978-389-0359', 'Laron_Rice74@gmail.com', 'https://clementina.com', 'Exclusive client-server secured line', 436, 0, 2);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_003_urh', 'CODE_86552_003', '2514 Gottlieb Harbors', '217-430-1793', 'Timmy.Cormier53@hotmail.com', 'http://kolby.name', 'Stand-alone foreground portal', 761, 0, 3);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Virginia Sauer', 'member_001_jsn', 'Occaecati ipsam velit.', '["read"]', 2, 1, 1);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Trinity Abshire', 'member_002_ons', 'Modi voluptatem facere.', '["create","read","edit"]', 2, 1, 2);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Geovanni Gislason', 'member_003_mc9', 'Perferendis est magnam.', '["update"]', 8, 0, 3);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Ashly.Stark50', 'Krystel_Effertz78@gmail.com', 'non_1', 'Phương_001_6jp', 'Phương_001_xk1', '371-020-2523', '4964 Jonas Ridges', '1989-02-14 00:00:00', 'Female', 'https://cordia.com', 2, 2, 42741.06, '2016-04-29 20:39:21', 'Shoes', 0, '2024-07-23 01:47:12', 1);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Shyann14', 'Marina.Cole94@gmail.com', 'recusandae_2', 'Phương_002_2e0', 'Phương_002_wp8', '402-756-5737', '475 Considine Roads', '1989-12-22 00:00:00', 'Other', 'http://carli.org', 3, 3, 153503.02, '2016-08-01 14:05:02', 'Automotive & Computers', 0, '2024-12-03 10:59:47', 2);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Steve.Bashirian42', 'Nova.Kulas54@gmail.com', 'ducimus_3', 'Phương_003_srw', 'Phương_003_rm4', '165-810-3299', '28362 Hane Estate', '1989-05-26 00:00:00', 'Male', 'http://brooke.name', 1, 1, 192391.37, '2025-02-28 00:11:56', 'Computers, Computers & Clothing', 0, '2024-07-03 03:52:08', 3);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (2, 2, 2, '2025-03-18 09:52:21', 0, 1);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (3, 3, 3, '2024-08-10 17:25:34', 0, 2);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (1, 1, 1, '2025-05-18 17:34:21', 0, 3);

-- Re-enable foreign key checks
SET FOREIGN_KEY_CHECKS = 1;

-- COMMIT;
-- End of generated SQL WITH IDs (12 statements)
