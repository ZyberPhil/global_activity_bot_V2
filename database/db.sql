-- GlobalActivityBot Database Schema
-- MariaDB 11.4+
-- Level formula: FLOOR(SQRT(xp / 10))

CREATE DATABASE IF NOT EXISTS `discord_activity` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `discord_activity`;

-- ============================================================
-- Tables
-- ============================================================

CREATE TABLE IF NOT EXISTS `users` (
    `id`           INT UNSIGNED     NOT NULL AUTO_INCREMENT,
    `discord_id`   VARCHAR(20)      NOT NULL,
    `username`     VARCHAR(100)     NOT NULL,
    `global_xp`    INT UNSIGNED     NOT NULL DEFAULT 0,
    `global_level` INT UNSIGNED     NOT NULL DEFAULT 0,
    `created_at`   DATETIME         NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_users_discord_id` (`discord_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `guilds` (
    `id`         INT UNSIGNED  NOT NULL AUTO_INCREMENT,
    `discord_id` VARCHAR(20)   NOT NULL,
    `name`       VARCHAR(100)  NOT NULL,
    `created_at` DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_guilds_discord_id` (`discord_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `user_stats` (
    `id`              INT UNSIGNED  NOT NULL AUTO_INCREMENT,
    `user_id`         INT UNSIGNED  NOT NULL,
    `guild_id`        INT UNSIGNED  NOT NULL,
    `xp`              INT UNSIGNED  NOT NULL DEFAULT 0,
    `level`           INT UNSIGNED  NOT NULL DEFAULT 0,
    `message_count`   INT UNSIGNED  NOT NULL DEFAULT 0,
    `last_message_at` DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_user_stats_user_guild` (`user_id`, `guild_id`),
    CONSTRAINT `fk_user_stats_user`  FOREIGN KEY (`user_id`)  REFERENCES `users`  (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_user_stats_guild` FOREIGN KEY (`guild_id`) REFERENCES `guilds` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `channel_stats` (
    `id`                 INT UNSIGNED  NOT NULL AUTO_INCREMENT,
    `guild_id`           INT UNSIGNED  NOT NULL,
    `discord_channel_id` VARCHAR(20)   NOT NULL,
    `message_count`      INT UNSIGNED  NOT NULL DEFAULT 0,
    `last_message_at`    DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_channel_stats_guild_channel` (`guild_id`, `discord_channel_id`),
    CONSTRAINT `fk_channel_stats_guild` FOREIGN KEY (`guild_id`) REFERENCES `guilds` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `badges` (
    `id`          INT UNSIGNED  NOT NULL AUTO_INCREMENT,
    `name`        VARCHAR(50)   NOT NULL,
    `description` VARCHAR(255)  NOT NULL,
    `emoji`       VARCHAR(50)   NOT NULL DEFAULT '🏅',
    `created_at`  DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_badges_name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `user_badges` (
    `id`         INT UNSIGNED  NOT NULL AUTO_INCREMENT,
    `user_id`    INT UNSIGNED  NOT NULL,
    `badge_id`   INT UNSIGNED  NOT NULL,
    `awarded_at` DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_user_badges_user_badge` (`user_id`, `badge_id`),
    CONSTRAINT `fk_user_badges_user`  FOREIGN KEY (`user_id`)  REFERENCES `users`  (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_user_badges_badge` FOREIGN KEY (`badge_id`) REFERENCES `badges` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
-- Stored Procedures
-- ============================================================

DELIMITER $$

-- Get or create a user by Discord ID
CREATE PROCEDURE IF NOT EXISTS `sp_GetOrCreateUser`(
    IN  p_discord_id VARCHAR(20),
    IN  p_username   VARCHAR(100),
    OUT p_user_id    INT UNSIGNED
)
BEGIN
    SELECT `id` INTO p_user_id FROM `users` WHERE `discord_id` = p_discord_id LIMIT 1;

    IF p_user_id IS NULL THEN
        INSERT INTO `users` (`discord_id`, `username`)
        VALUES (p_discord_id, p_username);
        SET p_user_id = LAST_INSERT_ID();
    ELSE
        UPDATE `users` SET `username` = p_username WHERE `id` = p_user_id;
    END IF;
END$$

-- Get or create a guild by Discord ID
CREATE PROCEDURE IF NOT EXISTS `sp_GetOrCreateGuild`(
    IN  p_discord_id VARCHAR(20),
    IN  p_name       VARCHAR(100),
    OUT p_guild_id   INT UNSIGNED
)
BEGIN
    SELECT `id` INTO p_guild_id FROM `guilds` WHERE `discord_id` = p_discord_id LIMIT 1;

    IF p_guild_id IS NULL THEN
        INSERT INTO `guilds` (`discord_id`, `name`)
        VALUES (p_discord_id, p_name);
        SET p_guild_id = LAST_INSERT_ID();
    END IF;
END$$

-- Add XP to a user in a guild (get-or-create user stat)
CREATE PROCEDURE IF NOT EXISTS `sp_AddXp`(
    IN p_user_discord_id  VARCHAR(20),
    IN p_guild_discord_id VARCHAR(20),
    IN p_username         VARCHAR(100),
    IN p_guild_name       VARCHAR(100),
    IN p_xp_amount        INT UNSIGNED
)
BEGIN
    DECLARE v_user_id  INT UNSIGNED;
    DECLARE v_guild_id INT UNSIGNED;
    DECLARE v_new_xp   INT UNSIGNED;

    CALL sp_GetOrCreateUser(p_user_discord_id, p_username, v_user_id);
    CALL sp_GetOrCreateGuild(p_guild_discord_id, p_guild_name, v_guild_id);

    INSERT INTO `user_stats` (`user_id`, `guild_id`, `xp`, `level`, `message_count`, `last_message_at`)
    VALUES (v_user_id, v_guild_id, p_xp_amount, FLOOR(SQRT(p_xp_amount / 10)), 1, NOW())
    ON DUPLICATE KEY UPDATE
        `xp`              = `xp` + p_xp_amount,
        `level`           = FLOOR(SQRT((`xp` + p_xp_amount) / 10)),
        `message_count`   = `message_count` + 1,
        `last_message_at` = NOW();
END$$

-- Sync global XP cache (sum all guild XP per user)
CREATE PROCEDURE IF NOT EXISTS `sp_SyncGlobalXp`()
BEGIN
    UPDATE `users` u
    INNER JOIN (
        SELECT `user_id`, SUM(`xp`) AS total_xp
        FROM `user_stats`
        GROUP BY `user_id`
    ) agg ON u.`id` = agg.`user_id`
    SET
        u.`global_xp`    = agg.total_xp,
        u.`global_level` = FLOOR(SQRT(agg.total_xp / 10));
END$$

DELIMITER ;

-- ============================================================
-- Seed data (optional)
-- ============================================================

INSERT IGNORE INTO `badges` (`name`, `description`, `emoji`) VALUES
    ('early_adopter',   'One of the first members!',    '🌟'),
    ('active',          'Posted 1000+ messages',         '💬'),
    ('veteran',         'Been here for over a year',     '🏆'),
    ('helper',          'Helped many members',           '🤝');
