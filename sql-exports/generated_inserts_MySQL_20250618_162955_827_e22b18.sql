-- Generated SQL INSERT statements (WITH ID COLUMNS)
-- Database Type: MySQL
-- Generated at: 2025-06-18 16:29:55
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

INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_001_jwb', 'CODE_94461_001', '33673 Will Ports', '818-395-7958', 'Zita_Parker@yahoo.com', 'http://wava.net', 'Synergized methodical analyzer', 497, 1, 1);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_002_wyh', 'CODE_14485_002', '428 Twila Crossing', '587-754-6570', 'Wallace_Nitzsche@yahoo.com', 'http://javier.biz', 'Self-enabling well-modulated application', 287, 0, 2);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_003_w3m', 'CODE_32548_003', '94975 Hilll Trail', '642-427-0102', 'Lawrence.Schiller@yahoo.com', 'http://martin.name', 'Phased 4th generation framework', 450, 1, 3);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_004_78m', 'CODE_47373_004', '2381 Heathcote Loop', '355-650-8332', 'Sarah_Friesen@hotmail.com', 'https://alena.info', 'Seamless local Graphic Interface', 967, 1, 4);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_005_8ul', 'CODE_60437_005', '98988 Abshire Roads', '874-065-5539', 'Amely.Rowe43@hotmail.com', 'https://sandrine.org', 'Multi-lateral transitional circuit', 449, 0, 5);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_006_u92', 'CODE_72082_006', '2981 Dawson Isle', '503-370-5633', 'Michale_Toy@gmail.com', 'http://myron.name', 'Extended disintermediate focus group', 899, 1, 6);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_007_lm1', 'CODE_81588_007', '9283 Smitham Forges', '590-236-8964', 'Minnie.Braun@yahoo.com', 'https://berry.biz', 'Grass-roots system-worthy portal', 655, 1, 7);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_008_1wu', 'CODE_93493_008', '751 Rodriguez Drives', '250-504-3205', 'Christelle33@gmail.com', 'http://albin.biz', 'Organic 5th generation strategy', 719, 1, 8);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_009_aip', 'CODE_15399_009', '890 Jenkins Plains', '482-326-3866', 'Alivia_Schinner39@gmail.com', 'https://carolanne.com', 'Networked context-sensitive ability', 594, 0, 9);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_010_d1g', 'CODE_35696_010', '71633 Ziemann Forest', '173-520-6180', 'Kiley.McCullough7@yahoo.com', 'https://lonie.com', 'Multi-lateral system-worthy neural-net', 328, 1, 10);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Nils Kunze', 'member_001_ss8', 'Explicabo quis sit.', '["create"]', 5, 1, 1);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Vivian Lesch', 'member_002_wwy', 'Dolorum repellat ratione.', '["read","write","edit"]', 8, 1, 2);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Rashawn Bahringer', 'member_003_54t', 'Quia modi voluptates.', '["admin","read","update"]', 4, 1, 3);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Hailee Mueller', 'member_004_77e', 'Autem delectus autem.', '["read","edit"]', 8, 1, 4);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Raegan Koepp', 'member_005_75d', 'Ratione sed fuga.', '["write","admin","delete"]', 1, 1, 5);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Jess Koelpin', 'member_006_8dt', 'Itaque nemo est.', '["view","read","delete","create"]', 1, 0, 6);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Rosa Tremblay', 'member_007_2ak', 'Non saepe odio.', '["read","delete","view","update"]', 3, 0, 7);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Chelsea Friesen', 'member_008_2a1', 'Rem laborum commodi.', '["update"]', 6, 1, 8);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Clinton Bartell', 'member_009_cnl', 'At harum rerum.', '["create","admin","write"]', 5, 1, 9);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Sabryna Kirlin', 'member_010_1pl', 'A earum labore.', '["edit","delete","read","view"]', 10, 1, 10);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Brando_Blanda', 'Cruz_Stanton@hotmail.com', 'vel_1', 'Phương_001_drv', 'Phương_001_u8m', '251-013-9173', '3438 Beahan Trafficway', '1989-02-24 00:00:00', 'Male', 'https://lyda.name', 2, 2, 55749.30, '2020-07-12 09:19:20', 'Industrial & Grocery', 0, '2024-10-13 06:54:38', 1);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Chandler_Mante86', 'Miles.Mertz34@yahoo.com', 'ratione_2', 'Phương_002_tl5', 'Phương_002_76g', '976-827-6594', '67555 Mayert Street', '1989-08-16 00:00:00', 'Other', 'https://lyric.net', 3, 3, 49709.04, '2020-02-29 07:27:57', 'Grocery', 0, '2025-03-13 04:50:12', 2);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Schuyler70', 'Nia94@hotmail.com', 'molestiae_3', 'Phương_003_1jw', 'Phương_003_42u', '529-391-3658', '56978 Mafalda Light', '1989-05-29 00:00:00', 'Other', 'https://roderick.com', 4, 4, 93886.54, '2022-08-20 04:36:23', 'Music & Clothing', 0, '2025-03-29 07:34:02', 3);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('David.Kovacek', 'Lavonne_Bailey0@hotmail.com', 'voluptatum_4', 'Phương_004_hfh', 'Phương_004_5g5', '853-566-7979', '537 Borer Heights', '1989-04-24 00:00:00', 'Female', 'https://kyleigh.org', 5, 5, 142365.51, '2019-10-30 16:13:11', 'Jewelery, Outdoors & Outdoors', 0, '2025-04-26 13:49:24', 4);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Isaac_Lubowitz17', 'Jude62@hotmail.com', 'et_5', 'Phương_005_5r4', 'Phương_005_xdd', '920-006-7208', '553 Juwan Forge', '1989-09-26 00:00:00', 'Other', 'http://darien.info', 6, 6, 33423.95, '2019-10-06 07:52:41', 'Music', 0, '2025-04-25 22:34:10', 5);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Flo.Crooks71', 'Vicenta.Pagac@yahoo.com', 'molestiae_6', 'Phương_006_b39', 'Phương_006_o47', '830-569-2851', '480 Helga Locks', '1989-08-04 00:00:00', 'Male', 'http://madonna.biz', 7, 7, 37430.18, '2016-11-04 21:34:35', 'Music', 0, '2025-05-18 17:24:36', 6);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Kelsi.Welch52', 'Yazmin_OReilly@yahoo.com', 'velit_7', 'Phương_007_rwe', 'Phương_007_9z8', '046-945-4300', '431 Quigley Harbor', '1989-10-23 00:00:00', 'Other', 'http://shyanne.net', 8, 8, 116915.48, '2025-01-22 11:53:18', 'Clothing', 0, '2024-12-01 18:58:09', 7);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('George50', 'Lincoln26@gmail.com', 'omnis_8', 'Phương_008_b1y', 'Phương_008_nzn', '007-402-7169', '72789 Thiel Parkways', '1989-05-01 00:00:00', 'Other', 'http://maureen.com', 9, 9, 127669.10, '2025-01-04 14:53:13', 'Computers & Toys', 0, '2024-07-25 23:50:51', 8);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Magali24', 'Margot_Lockman@gmail.com', 'optio_9', 'Phương_009_giz', 'Phương_009_d7h', '351-755-1271', '3742 Dach Creek', '1989-10-09 00:00:00', 'Other', 'http://darion.org', 10, 10, 186387.88, '2023-10-13 08:58:21', 'Books', 0, '2025-05-17 14:43:46', 9);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Trisha7', 'Uriah.Homenick@hotmail.com', 'quo_10', 'Phương_010_uc4', 'Phương_010_v56', '417-498-1107', '77380 Ursula Turnpike', '1989-11-01 00:00:00', 'Male', 'http://delilah.org', 1, 1, 101713.68, '2015-09-14 10:59:55', 'Baby & Movies', 0, '2024-09-29 21:50:16', 10);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (2, 2, 2, '2025-01-18 09:08:57', 0, 1);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (3, 3, 3, '2024-07-02 19:18:24', 0, 2);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (4, 4, 4, '2024-08-24 08:44:25', 0, 3);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (5, 5, 5, '2024-07-17 04:40:32', 0, 4);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (6, 6, 6, '2024-11-05 11:05:26', 0, 5);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (7, 7, 7, '2025-03-07 05:24:04', 0, 6);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (8, 8, 8, '2024-09-14 19:08:42', 0, 7);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (9, 9, 9, '2025-01-01 18:14:28', 0, 8);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (10, 10, 10, '2024-11-23 09:54:56', 0, 9);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (1, 1, 1, '2024-11-24 03:13:17', 0, 10);

-- Re-enable foreign key checks
SET FOREIGN_KEY_CHECKS = 1;

-- COMMIT;
-- End of generated SQL WITH IDs (40 statements)
