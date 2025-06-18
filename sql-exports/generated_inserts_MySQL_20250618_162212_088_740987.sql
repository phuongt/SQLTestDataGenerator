-- Generated SQL INSERT statements (WITH ID COLUMNS)
-- Database Type: MySQL
-- Generated at: 2025-06-18 16:22:12
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

INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_001_6in', 'CODE_34570_001', '59961 Walter Divide', '728-560-3463', 'Bridget_Quigley@hotmail.com', 'https://stuart.net', 'Devolved multi-state help-desk', 894, 1, 1);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_002_x6c', 'CODE_59060_002', '36460 Shields Mountains', '490-544-7516', 'Antwan72@yahoo.com', 'http://garfield.info', 'Universal eco-centric collaboration', 251, 0, 2);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_003_0aq', 'CODE_77763_003', '8146 Weber Springs', '377-019-5684', 'Kara.Spencer@gmail.com', 'https://jenifer.org', 'Devolved impactful function', 345, 1, 3);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_004_msa', 'CODE_96029_004', '9101 Reinger Lock', '652-319-2995', 'Richie7@yahoo.com', 'https://joanne.net', 'Fundamental foreground hierarchy', 474, 1, 4);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_005_s8q', 'CODE_16030_005', '812 Electa Loaf', '874-682-7701', 'Marilou_Wintheiser3@yahoo.com', 'https://gage.net', 'Team-oriented systemic algorithm', 976, 0, 5);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_006_avh', 'CODE_50602_006', '6712 Boyd Lane', '417-888-5076', 'Presley.Huel@yahoo.com', 'https://rebekah.info', 'Intuitive 24/7 attitude', 611, 1, 6);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_007_2t1', 'CODE_65193_007', '80908 Ruecker Land', '274-362-9994', 'Yasmeen43@yahoo.com', 'https://federico.info', 'User-centric 24 hour info-mediaries', 756, 1, 7);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_008_q2d', 'CODE_78821_008', '277 Douglas Club', '243-348-8640', 'Wilma51@gmail.com', 'https://tara.info', 'Automated uniform internet solution', 310, 0, 8);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_009_tt9', 'CODE_90185_009', '6541 Davin Knoll', '586-772-0893', 'Darrion_Aufderhar78@hotmail.com', 'https://travis.org', 'Realigned actuating paradigm', 218, 1, 9);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_010_8zq', 'CODE_60_010', '2542 Desiree Road', '116-315-2797', 'Sunny.Leffler@yahoo.com', 'https://otha.biz', 'Function-based uniform array', 815, 0, 10);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Lon Bednar', 'member_001_ajp', 'Est assumenda illum.', '["create","read"]', 7, 1, 1);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Eryn Gerlach', 'member_002_392', 'Non dignissimos dicta.', '["edit","view","read"]', 7, 1, 2);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Madie Davis', 'member_003_19m', 'Sint sint dignissimos.', '["update","read"]', 4, 1, 3);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Idella Miller', 'member_004_i3i', 'Sunt asperiores porro.', '["edit","create","update"]', 9, 0, 4);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Bobbie West', 'member_005_vvi', 'Sit aut vero.', '["read"]', 6, 1, 5);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Cortez Ankunding', 'member_006_1rm', 'Earum adipisci nam.', '["read","admin"]', 6, 1, 6);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Eldon DuBuque', 'member_007_xxi', 'Quia eveniet magni.', '["delete","admin"]', 4, 1, 7);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Zoey Bartoletti', 'member_008_88c', 'Sit officia architecto.', '["create","delete","update"]', 10, 1, 8);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Candice Feil', 'member_009_hzc', 'Doloribus consequatur perferendis.', '["edit","write","update"]', 6, 1, 9);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Chase Daniel', 'member_010_l3b', 'Similique nemo rerum.', '["view","admin"]', 6, 0, 10);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Marlin_Parisian', 'Brice.Stehr@hotmail.com', 'nulla_1', 'Phương_001_xp8', 'Phương_001_pn7', '568-685-7285', '003 Lavonne Bypass', '1989-04-10 00:00:00', 'Male', 'https://dorcas.org', 2, 2, 115837.70, '2016-10-01 17:17:31', 'Grocery, Outdoors & Outdoors', 0, '2025-02-01 13:26:34', 1);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Jaleel91', 'Braulio30@yahoo.com', 'dolorem_2', 'Phương_002_17h', 'Phương_002_m9j', '259-580-7320', '821 Nicolette Centers', '1989-12-03 00:00:00', 'Other', 'http://gaetano.name', 3, 3, 40889.30, '2016-01-12 06:22:27', 'Music, Music & Outdoors', 0, '2025-06-14 03:00:36', 2);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Freda_Pfeffer', 'Kiley42@gmail.com', 'quo_3', 'Phương_003_gt1', 'Phương_003_0ch', '911-216-8245', '68920 Kshlerin Trail', '1989-10-29 00:00:00', 'Other', 'http://ara.biz', 4, 4, 51195.21, '2018-12-31 13:50:56', 'Tools', 0, '2024-09-28 03:43:56', 3);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Dena18', 'Clint_Boehm46@gmail.com', 'ut_4', 'Phương_004_puv', 'Phương_004_k0o', '218-297-8578', '35216 Ortiz Club', '1989-07-10 00:00:00', 'Female', 'http://oran.name', 5, 5, 45738.70, '2016-09-06 14:00:39', 'Music', 0, '2024-11-01 04:49:17', 4);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Vella_Langworth30', 'Dee91@hotmail.com', 'et_5', 'Phương_005_grh', 'Phương_005_4ig', '995-206-9533', '04665 Dickens Club', '1989-03-24 00:00:00', 'Female', 'http://griffin.org', 6, 6, 110324.33, '2022-09-27 02:59:57', 'Baby & Movies', 0, '2025-05-17 04:30:50', 5);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Rubye77', 'Asha34@gmail.com', 'molestiae_6', 'Phương_006_y9v', 'Phương_006_fby', '413-306-5748', '291 McGlynn Valley', '1989-05-23 00:00:00', 'Male', 'https://amie.com', 7, 7, 143788.16, '2018-10-04 07:53:26', 'Sports, Clothing & Garden', 0, '2025-03-09 06:28:42', 6);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Annabell82', 'Johnnie.Jacobson72@hotmail.com', 'distinctio_7', 'Phương_007_5pf', 'Phương_007_j05', '038-274-6729', '7053 Gerard Knolls', '1989-02-25 00:00:00', 'Male', 'https://willy.com', 8, 8, 111388.11, '2022-10-12 15:18:29', 'Games, Kids & Home', 0, '2024-09-13 06:37:08', 7);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Jacey.Collier', 'Dalton86@hotmail.com', 'voluptatem_8', 'Phương_008_j71', 'Phương_008_qzz', '323-870-1905', '58541 Imani Summit', '1989-10-25 00:00:00', 'Male', 'http://domenick.info', 9, 9, 96390.10, '2024-05-30 21:22:57', 'Games, Outdoors & Garden', 0, '2025-05-21 08:47:32', 8);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Arlo_Abernathy23', 'Christiana_Orn@hotmail.com', 'aliquid_9', 'Phương_009_4td', 'Phương_009_r1t', '173-754-1178', '6984 Nikolaus Throughway', '1989-11-16 00:00:00', 'Female', 'https://christian.biz', 10, 10, 53715.89, '2025-01-13 07:04:19', 'Industrial & Movies', 0, '2025-05-20 22:12:08', 9);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Torrance.Denesik', 'Lorenza_Wunsch@hotmail.com', 'nam_10', 'Phương_010_o7d', 'Phương_010_ldm', '500-464-2860', '25129 Miller Islands', '1989-09-21 00:00:00', 'Male', 'http://cloyd.info', 1, 1, 32650.98, '2019-08-23 07:13:07', 'Industrial, Sports & Sports', 0, '2024-11-17 10:02:20', 10);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (2, 2, 2, '2025-03-28 17:17:08', 0, 1);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (3, 3, 3, '2025-05-24 21:10:30', 0, 2);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (4, 4, 4, '2025-04-29 10:29:57', 0, 3);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (5, 5, 5, '2025-03-22 21:53:20', 0, 4);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (6, 6, 6, '2024-08-30 20:20:34', 0, 5);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (7, 7, 7, '2025-02-19 11:46:37', 0, 6);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (8, 8, 8, '2024-10-02 18:47:22', 0, 7);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (9, 9, 9, '2025-03-13 11:30:46', 0, 8);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (10, 10, 10, '2024-07-17 10:30:44', 0, 9);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (1, 1, 1, '2025-06-16 16:58:32', 0, 10);

-- Re-enable foreign key checks
SET FOREIGN_KEY_CHECKS = 1;

-- COMMIT;
-- End of generated SQL WITH IDs (40 statements)
