-- Generated SQL INSERT statements (WITH ID COLUMNS)
-- Database Type: MySQL
-- Generated at: 2025-06-18 16:29:57
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

INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_001_tha', 'CODE_88331_001', '9301 Turcotte Springs', '818-664-9467', 'Damien.Schmeler@hotmail.com', 'https://noemie.biz', 'Diverse logistical strategy', 899, 1, 1);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_002_b2k', 'CODE_9355_002', '264 Walsh Mall', '931-583-1669', 'Alana74@gmail.com', 'https://christy.net', 'Persistent modular model', 401, 0, 2);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_003_7d4', 'CODE_29525_003', '41747 Ward Ports', '782-371-0326', 'Miguel_Yost50@hotmail.com', 'https://nina.net', 'Decentralized holistic matrix', 682, 1, 3);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_004_fg6', 'CODE_46533_004', '17270 Torp Branch', '298-490-7389', 'Keara12@gmail.com', 'http://enola.net', 'Seamless 24/7 model', 734, 0, 4);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_005_q8w', 'CODE_68809_005', '41411 Irma Drives', '740-762-5105', 'Wilhelm.McGlynn95@hotmail.com', 'http://kaci.biz', 'Assimilated composite hierarchy', 339, 1, 5);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_006_a5e', 'CODE_7380_006', '444 Reginald Track', '264-762-5112', 'Mireille41@hotmail.com', 'https://andrew.name', 'Stand-alone interactive core', 172, 1, 6);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_007_lb8', 'CODE_28344_007', '23698 Wiza Ports', '453-803-6725', 'Gunner.Marquardt34@hotmail.com', 'https://evert.name', 'Sharable executive ability', 257, 1, 7);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_008_6lu', 'CODE_44930_008', '321 Justen Stravenue', '014-334-4357', 'Arthur_Schaefer19@gmail.com', 'http://damion.biz', 'Programmable zero defect conglomeration', 670, 1, 8);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_009_r0p', 'CODE_61177_009', '427 Giovanni Pines', '849-035-2043', 'Deondre_Considine@gmail.com', 'https://cyrus.info', 'Enterprise-wide real-time analyzer', 738, 1, 9);
INSERT INTO `companies` (`name`, `code`, `address`, `phone`, `email`, `website`, `industry`, `employee_count`, `is_active`, `id`) VALUES ('HOME_010_8i3', 'CODE_73800_010', '918 OConner Plaza', '075-030-2620', 'Monique6@yahoo.com', 'https://catherine.biz', 'Fully-configurable value-added array', 212, 0, 10);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Sherman Weber', 'member_001_166', 'Et quos recusandae.', '["admin","view","update"]', 2, 1, 1);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Steve Gusikowski', 'member_002_jsk', 'Libero ab unde.', '["update","view","admin"]', 9, 0, 2);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Edison OConner', 'member_003_8i7', 'Quia officia rerum.', '["create","admin","read","view"]', 5, 1, 3);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Berneice Rohan', 'member_004_o4i', 'Aperiam et voluptatum.', '["write","delete"]', 9, 1, 4);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Mabelle Daugherty', 'member_005_dy3', 'Maiores beatae soluta.', '["edit","admin","read","delete"]', 1, 1, 5);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Kiel Durgan', 'member_006_mpp', 'Velit a saepe.', '["write","admin"]', 9, 0, 6);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Tony Gleichner', 'member_007_2y3', 'Nesciunt cumque sit.', '["admin","write","edit","view"]', 4, 1, 7);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Russel Tromp', 'member_008_hj8', 'Officia qui laudantium.', '["update","admin","write","delete"]', 2, 1, 8);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Rowena McCullough', 'member_009_v6m', 'Vitae voluptatem ut.', '["update","admin"]', 3, 1, 9);
INSERT INTO `roles` (`name`, `code`, `description`, `permissions`, `level`, `is_active`, `id`) VALUES ('Alexa Hansen', 'member_010_183', 'Velit dolores iure.', '["view","admin","delete"]', 2, 1, 10);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Dennis85', 'Fiona_Labadie25@yahoo.com', 'aliquid_1', 'Phương_001_xnn', 'Phương_001_6l0', '858-055-1491', '5308 Mollie Skyway', '1989-04-26 00:00:00', 'Other', 'https://destany.net', 2, 2, 173361.00, '2023-05-14 17:53:47', 'Beauty & Outdoors', 0, '2024-08-01 23:07:44', 1);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Otis.Considine', 'Norval20@yahoo.com', 'iusto_2', 'Phương_002_fzi', 'Phương_002_ssf', '080-131-5003', '5334 Mayer Field', '1989-05-20 00:00:00', 'Male', 'http://keven.org', 3, 3, 192873.42, '2024-01-04 19:42:47', 'Games & Games', 0, '2024-07-06 14:27:14', 2);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Dashawn.Jakubowski', 'Bryon68@gmail.com', 'quas_3', 'Phương_003_2cf', 'Phương_003_vqi', '437-373-2005', '785 Gibson Divide', '1989-08-21 00:00:00', 'Female', 'https://wilfred.biz', 4, 4, 156271.47, '2019-08-08 05:42:31', 'Outdoors, Kids & Baby', 0, '2025-06-06 15:03:08', 3);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Edward.Mohr', 'Uriel_Schaefer18@yahoo.com', 'quam_4', 'Phương_004_lsv', 'Phương_004_00r', '070-317-6926', '9684 Gaylord Rapids', '1989-10-10 00:00:00', 'Female', 'https://elyssa.info', 5, 5, 84893.83, '2017-10-13 23:52:30', 'Jewelery & Computers', 0, '2025-03-14 04:44:21', 4);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Dakota1', 'Terrance.Wiegand83@hotmail.com', 'quia_5', 'Phương_005_zmu', 'Phương_005_aqc', '451-129-6726', '258 Halvorson Groves', '1989-09-02 00:00:00', 'Female', 'http://olen.com', 6, 6, 85806.27, '2016-08-27 15:20:11', 'Automotive', 0, '2024-10-05 23:44:02', 5);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Meaghan.Donnelly65', 'Lucienne_Dibbert@yahoo.com', 'sed_6', 'Phương_006_1ba', 'Phương_006_1n3', '182-412-0402', '976 Swift Mountains', '1989-06-03 00:00:00', 'Male', 'http://astrid.com', 7, 7, 127971.04, '2018-11-09 15:46:35', 'Toys', 0, '2025-05-31 16:05:20', 6);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Kameron79', 'Ubaldo54@yahoo.com', 'vitae_7', 'Phương_007_6z0', 'Phương_007_qcl', '388-385-1812', '46686 Volkman Manor', '1989-09-12 00:00:00', 'Male', 'https://ashton.info', 8, 8, 60418.92, '2022-11-02 18:53:29', 'Toys', 0, '2024-12-26 14:04:54', 7);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Bobby67', 'Ella6@gmail.com', 'ab_8', 'Phương_008_bks', 'Phương_008_cd5', '338-128-9179', '7810 Mozell Parks', '1989-09-25 00:00:00', 'Male', 'https://enid.net', 9, 9, 120936.36, '2017-02-21 05:22:25', 'Outdoors', 0, '2025-06-10 04:23:20', 8);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Judge64', 'Reginald.Koepp@hotmail.com', 'eveniet_9', 'Phương_009_2g0', 'Phương_009_2p2', '782-052-6885', '25081 Corbin Drives', '1989-10-17 00:00:00', 'Other', 'http://shawna.net', 10, 10, 156964.92, '2018-04-29 04:14:49', 'Books', 0, '2024-08-14 13:38:24', 9);
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, `is_active`, `last_login_at`, `id`) VALUES ('Enrico44', 'Khalil5@yahoo.com', 'ut_10', 'Phương_010_odc', 'Phương_010_9re', '929-408-1333', '617 Winifred Plains', '1989-12-05 00:00:00', 'Male', 'https://evans.org', 1, 1, 63781.76, '2016-05-02 10:59:42', 'Sports, Health & Baby', 0, '2025-05-14 11:38:37', 10);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (2, 2, 2, '2024-12-19 02:03:31', 0, 1);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (3, 3, 3, '2025-05-23 20:34:47', 0, 2);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (4, 4, 4, '2024-10-02 15:42:48', 0, 3);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (5, 5, 5, '2024-11-19 23:29:31', 0, 4);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (6, 6, 6, '2025-01-06 02:20:01', 0, 5);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (7, 7, 7, '2024-08-06 13:45:52', 0, 6);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (8, 8, 8, '2024-09-11 15:21:44', 0, 7);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (9, 9, 9, '2025-06-14 01:24:12', 0, 8);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (10, 10, 10, '2024-08-19 02:44:06', 0, 9);
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `expires_at`, `is_active`, `id`) VALUES (1, 1, 1, '2024-11-14 10:06:49', 0, 10);

-- Re-enable foreign key checks
SET FOREIGN_KEY_CHECKS = 1;

-- COMMIT;
-- End of generated SQL WITH IDs (40 statements)
