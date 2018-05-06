﻿using SimpleMigrations;

namespace Netsphere.Database.Migration.Game
{
    [Migration(1)]
    public class Base : SimpleMigrations.Migration
    {
        protected override void Up()
        {
            Execute(@"CREATE TABLE `shop_version` (
              `Id` int(11) NOT NULL AUTO_INCREMENT,
              `Version` varchar(40) NOT NULL DEFAULT '',
              PRIMARY KEY (`Id`)
            );");
            
            Execute(@"CREATE TABLE `shop_effect_groups` (
              `Id` int(11) NOT NULL AUTO_INCREMENT,
              `Name` varchar(20) NOT NULL DEFAULT '',
              PRIMARY KEY (`Id`)
            );");
            
            Execute(@"CREATE TABLE `shop_effects` (
              `Id` int(11) NOT NULL AUTO_INCREMENT,
              `EffectGroupId` int(11) NOT NULL,
              `Effect` bigint(20) NOT NULL DEFAULT '0',
              PRIMARY KEY (`Id`),
              KEY `EffectGroupId` (`EffectGroupId`),
              CONSTRAINT `shop_effects_ibfk_1` FOREIGN KEY (`EffectGroupId`) REFERENCES `shop_effect_groups` (`Id`) ON DELETE CASCADE
            );");
            
            Execute(@"CREATE TABLE `shop_price_groups` (
              `Id` int(11) NOT NULL AUTO_INCREMENT,
              `Name` varchar(20) DEFAULT '',
              `PriceType` tinyint(3) unsigned NOT NULL DEFAULT '0',
              PRIMARY KEY (`Id`)
            );");
            
            Execute(@"CREATE TABLE `shop_prices` (
              `Id` int(11) NOT NULL AUTO_INCREMENT,
              `PriceGroupId` int(11) NOT NULL,
              `PeriodType` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `Period` int(11) NOT NULL DEFAULT '0',
              `Price` int(11) NOT NULL DEFAULT '0',
              `IsRefundable` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `Durability` int(11) NOT NULL DEFAULT '0',
              `IsEnabled` tinyint(3) unsigned NOT NULL DEFAULT '0',
              PRIMARY KEY (`Id`),
              KEY `PriceGroupId` (`PriceGroupId`),
              CONSTRAINT `shop_prices_ibfk_1` FOREIGN KEY (`PriceGroupId`) REFERENCES `shop_price_groups` (`Id`) ON DELETE CASCADE
            );");

            Execute(@"CREATE TABLE `shop_items` (
              `Id` int(10) unsigned NOT NULL,
              `RequiredGender` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `RequiredLicense` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `Colors` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `UniqueColors` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `RequiredLevel` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `LevelLimit` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `RequiredMasterLevel` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `IsOneTimeUse` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `IsDestroyable` tinyint(3) unsigned NOT NULL DEFAULT '0',
              PRIMARY KEY (`Id`)
            );");
            
            Execute(@"CREATE TABLE `shop_iteminfos` (
              `Id` int(11) NOT NULL AUTO_INCREMENT,
              `ShopItemId` int(11) unsigned NOT NULL,
              `PriceGroupId` int(11) NOT NULL,
              `EffectGroupId` int(11) NOT NULL,
              `DiscountPercentage` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `IsEnabled` tinyint(3) unsigned NOT NULL DEFAULT '0',
              PRIMARY KEY (`Id`),
              KEY `PriceGroupId` (`PriceGroupId`),
              KEY `EffectGroupId` (`EffectGroupId`),
              KEY `ShopItemId` (`ShopItemId`),
              CONSTRAINT `shop_iteminfos_ibfk_2` FOREIGN KEY (`PriceGroupId`) REFERENCES `shop_price_groups` (`Id`) ON DELETE CASCADE,
              CONSTRAINT `shop_iteminfos_ibfk_3` FOREIGN KEY (`EffectGroupId`) REFERENCES `shop_effect_groups` (`Id`) ON DELETE CASCADE,
              CONSTRAINT `shop_iteminfos_ibfk_4` FOREIGN KEY (`ShopItemId`) REFERENCES `shop_items` (`Id`) ON DELETE CASCADE
            );");
            
            Execute(@"CREATE TABLE `license_rewards` (
              `Id` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `ShopItemInfoId` int(11) NOT NULL,
              `ShopPriceId` int(11) NOT NULL,
              `Color` tinyint(3) unsigned NOT NULL DEFAULT '0',
              PRIMARY KEY (`Id`),
              KEY `ShopItemInfoId` (`ShopItemInfoId`),
              KEY `ShopPriceId` (`ShopPriceId`),
              CONSTRAINT `license_rewards_ibfk_1` FOREIGN KEY (`ShopItemInfoId`) REFERENCES `shop_iteminfos` (`Id`) ON DELETE CASCADE,
              CONSTRAINT `license_rewards_ibfk_2` FOREIGN KEY (`ShopPriceId`) REFERENCES `shop_prices` (`Id`) ON DELETE CASCADE
            );");
            
            Execute(@"CREATE TABLE `start_items` (
              `Id` int(11) NOT NULL AUTO_INCREMENT,
              `ShopItemInfoId` int(11) NOT NULL,
              `ShopPriceId` int(11) NOT NULL,
              `ShopEffectId` int(11) NOT NULL,
              `Color` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `Count` int(11) NOT NULL DEFAULT '0',
              `RequiredSecurityLevel` tinyint(3) unsigned NOT NULL DEFAULT '0',
              PRIMARY KEY (`Id`),
              KEY `ShopItemInfoId` (`ShopItemInfoId`),
              KEY `ShopPriceId` (`ShopPriceId`),
              KEY `ShopEffectId` (`ShopEffectId`),
              CONSTRAINT `start_items_ibfk_1` FOREIGN KEY (`ShopItemInfoId`) REFERENCES `shop_iteminfos` (`Id`) ON DELETE CASCADE,
              CONSTRAINT `start_items_ibfk_2` FOREIGN KEY (`ShopPriceId`) REFERENCES `shop_prices` (`Id`) ON DELETE CASCADE,
              CONSTRAINT `start_items_ibfk_3` FOREIGN KEY (`ShopEffectId`) REFERENCES `shop_effects` (`Id`) ON DELETE CASCADE
            );");

            Execute(@"CREATE TABLE `players` (
              `Id` int(11) NOT NULL,
              `TutorialState` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `Level` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `TotalExperience` int(11) NOT NULL DEFAULT '0',
              `PEN` int(11) NOT NULL DEFAULT '0',
              `AP` int(11) NOT NULL DEFAULT '0',
              `Coins1` int(11) NOT NULL DEFAULT '0',
              `Coins2` int(11) NOT NULL DEFAULT '0',
              `CurrentCharacterSlot` tinyint(3) unsigned NOT NULL DEFAULT '0',
              PRIMARY KEY (`Id`)
            );");
            
            Execute(@"CREATE TABLE `player_items` (
              `Id` int(11) NOT NULL,
              `PlayerId` int(11) NOT NULL,
              `ShopItemInfoId` int(11) NOT NULL,
              `ShopPriceId` int(11) NOT NULL,
              `Effect` int(11) NOT NULL DEFAULT '0',
              `Color` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `PurchaseDate` bigint(20) NOT NULL DEFAULT '0',
              `Durability` int(11) NOT NULL DEFAULT '0',
              `Count` int(11) NOT NULL DEFAULT '0',
              PRIMARY KEY (`Id`),
              KEY `PlayerId` (`PlayerId`),
              KEY `ShopItemInfoId` (`ShopItemInfoId`),
              KEY `ShopPriceId` (`ShopPriceId`),
              CONSTRAINT `player_items_ibfk_1` FOREIGN KEY (`PlayerId`) REFERENCES `players` (`Id`) ON DELETE CASCADE,
              CONSTRAINT `player_items_ibfk_2` FOREIGN KEY (`ShopItemInfoId`) REFERENCES `shop_iteminfos` (`Id`) ON DELETE CASCADE,
              CONSTRAINT `player_items_ibfk_3` FOREIGN KEY (`ShopPriceId`) REFERENCES `shop_prices` (`Id`) ON DELETE CASCADE
            );");
            
            Execute(@"CREATE TABLE `player_characters` (
              `Id` int(11) NOT NULL,
              `PlayerId` int(11) NOT NULL,
              `Slot` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `Gender` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `BasicHair` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `BasicFace` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `BasicShirt` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `BasicPants` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `Weapon1Id` int(11) DEFAULT NULL,
              `Weapon2Id` int(11) DEFAULT NULL,
              `Weapon3Id` int(11) DEFAULT NULL,
              `SkillId` int(11) DEFAULT NULL,
              `HairId` int(11) DEFAULT NULL,
              `FaceId` int(11) DEFAULT NULL,
              `ShirtId` int(11) DEFAULT NULL,
              `PantsId` int(11) DEFAULT NULL,
              `GlovesId` int(11) DEFAULT NULL,
              `ShoesId` int(11) DEFAULT NULL,
              `AccessoryId` int(11) DEFAULT NULL,
              PRIMARY KEY (`Id`),
              KEY `PlayerId` (`PlayerId`),
              KEY `Weapon1Id` (`Weapon1Id`),
              KEY `Weapon2Id` (`Weapon2Id`),
              KEY `Weapon3Id` (`Weapon3Id`),
              KEY `SkillId` (`SkillId`),
              KEY `HairId` (`HairId`),
              KEY `FaceId` (`FaceId`),
              KEY `ShirtId` (`ShirtId`),
              KEY `PantsId` (`PantsId`),
              KEY `GlovesId` (`GlovesId`),
              KEY `ShoesId` (`ShoesId`),
              KEY `AccessoryId` (`AccessoryId`),
              CONSTRAINT `player_characters_ibfk_1` FOREIGN KEY (`PlayerId`) REFERENCES `players` (`Id`) ON DELETE CASCADE,
              CONSTRAINT `player_characters_ibfk_10` FOREIGN KEY (`GlovesId`) REFERENCES `player_items` (`Id`) ON DELETE SET NULL,
              CONSTRAINT `player_characters_ibfk_11` FOREIGN KEY (`ShoesId`) REFERENCES `player_items` (`Id`) ON DELETE SET NULL,
              CONSTRAINT `player_characters_ibfk_12` FOREIGN KEY (`AccessoryId`) REFERENCES `player_items` (`Id`) ON DELETE SET NULL,
              CONSTRAINT `player_characters_ibfk_2` FOREIGN KEY (`Weapon1Id`) REFERENCES `player_items` (`Id`) ON DELETE SET NULL,
              CONSTRAINT `player_characters_ibfk_3` FOREIGN KEY (`Weapon2Id`) REFERENCES `player_items` (`Id`) ON DELETE SET NULL,
              CONSTRAINT `player_characters_ibfk_4` FOREIGN KEY (`Weapon3Id`) REFERENCES `player_items` (`Id`) ON DELETE SET NULL,
              CONSTRAINT `player_characters_ibfk_5` FOREIGN KEY (`SkillId`) REFERENCES `player_items` (`Id`) ON DELETE SET NULL,
              CONSTRAINT `player_characters_ibfk_6` FOREIGN KEY (`HairId`) REFERENCES `player_items` (`Id`) ON DELETE SET NULL,
              CONSTRAINT `player_characters_ibfk_7` FOREIGN KEY (`FaceId`) REFERENCES `player_items` (`Id`) ON DELETE SET NULL,
              CONSTRAINT `player_characters_ibfk_8` FOREIGN KEY (`ShirtId`) REFERENCES `player_items` (`Id`) ON DELETE SET NULL,
              CONSTRAINT `player_characters_ibfk_9` FOREIGN KEY (`PantsId`) REFERENCES `player_items` (`Id`) ON DELETE SET NULL
            );");
            
            Execute(@"CREATE TABLE `player_deny` (
              `Id` int(11) NOT NULL,
              `PlayerId` int(11) NOT NULL,
              `DenyPlayerId` int(11) NOT NULL,
              PRIMARY KEY (`Id`),
              KEY `PlayerId` (`PlayerId`),
              KEY `DenyPlayerId` (`DenyPlayerId`),
              CONSTRAINT `player_deny_ibfk_1` FOREIGN KEY (`PlayerId`) REFERENCES `players` (`Id`) ON DELETE CASCADE,
              CONSTRAINT `player_deny_ibfk_2` FOREIGN KEY (`DenyPlayerId`) REFERENCES `players` (`Id`) ON DELETE CASCADE
            );");

            Execute(@"CREATE TABLE `player_licenses` (
              `Id` int(11) NOT NULL,
              `PlayerId` int(11) NOT NULL,
              `License` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `FirstCompletedDate` bigint(20) NOT NULL DEFAULT '0',
              `CompletedCount` int(11) NOT NULL DEFAULT '0',
              PRIMARY KEY (`Id`),
              KEY `PlayerId` (`PlayerId`),
              CONSTRAINT `player_licenses_ibfk_1` FOREIGN KEY (`PlayerId`) REFERENCES `players` (`Id`) ON DELETE CASCADE
            );");
            
            Execute(@"CREATE TABLE `player_mails` (
              `Id` int(11) NOT NULL AUTO_INCREMENT,
              `PlayerId` int(11) NOT NULL,
              `SenderPlayerId` int(11) NOT NULL,
              `SentDate` bigint(20) NOT NULL DEFAULT '0',
              `Title` varchar(100) NOT NULL DEFAULT '',
              `Message` varchar(500) NOT NULL DEFAULT '',
              `IsMailNew` tinyint(3) unsigned NOT NULL DEFAULT '0',
              `IsMailDeleted` tinyint(3) unsigned NOT NULL DEFAULT '0',
              PRIMARY KEY (`Id`),
              KEY `PlayerId` (`PlayerId`),
              KEY `SenderPlayerId` (`SenderPlayerId`),
              CONSTRAINT `player_mails_ibfk_1` FOREIGN KEY (`PlayerId`) REFERENCES `players` (`Id`) ON DELETE CASCADE,
              CONSTRAINT `player_mails_ibfk_2` FOREIGN KEY (`SenderPlayerId`) REFERENCES `players` (`Id`) ON DELETE CASCADE
            );");
            
            Execute(@"CREATE TABLE `player_settings` (
              `Id` int(11) NOT NULL AUTO_INCREMENT,
              `PlayerId` int(11) NOT NULL,
              `Setting` varchar(512) NOT NULL DEFAULT '',
              `Value` varchar(512) NOT NULL DEFAULT '',
              PRIMARY KEY (`Id`),
              KEY `PlayerId` (`PlayerId`),
              CONSTRAINT `player_settings_ibfk_1` FOREIGN KEY (`PlayerId`) REFERENCES `players` (`Id`) ON DELETE CASCADE
            );");

            Execute(@"CREATE TABLE `player_chsinfos` (
  `PlayerId` int(11) NOT NULL,
  `ChasedWon` int(11) NOT NULL DEFAULT '0',
  `ChasedRounds` int(11) NOT NULL DEFAULT '0',
  `ChaserWon` int(11) NOT NULL DEFAULT '0',
  `ChaserRounds` int(11) NOT NULL DEFAULT '0',
  PRIMARY KEY (`PlayerId`),
  UNIQUE KEY `PlayerId` (`PlayerId`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;");

            Execute(@"CREATE TABLE `player_cptinfos` (
  `PlayerId` int(11) NOT NULL,
  `Won` int(11) NOT NULL DEFAULT '0',
  `Loss` int(11) NOT NULL DEFAULT '0',
  `CPTKilled` int(11) NOT NULL DEFAULT '0',
  `CPTCount` int(11) NOT NULL DEFAULT '0',
  PRIMARY KEY (`PlayerId`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;");

            Execute(@"CREATE TABLE `player_dminfos` (
  `PlayerId` int(11) NOT NULL,
  `Won` int(11) NOT NULL DEFAULT '0',
  `Loss` int(11) NOT NULL DEFAULT '0',
  `Kills` int(11) NOT NULL DEFAULT '0',
  `KillAssists` int(11) NOT NULL DEFAULT '0',
  `Deaths` int(11) NOT NULL DEFAULT '0',
  PRIMARY KEY (`PlayerId`),
  KEY `PlayerId` (`PlayerId`),
  KEY `PlayerId_2` (`PlayerId`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;");

            Execute(@"CREATE TABLE `player_tdinfos` (
  `PlayerId` int(11) NOT NULL,
  `Won` int(11) NOT NULL DEFAULT '0',
  `Loss` int(11) NOT NULL DEFAULT '0',
  `TD` int(11) NOT NULL DEFAULT '0',
  `TDAssist` int(11) NOT NULL DEFAULT '0',
  `Offense` int(11) NOT NULL DEFAULT '0',
  `OffenseAssist` int(11) NOT NULL DEFAULT '0',
  `Defense` int(11) NOT NULL DEFAULT '0',
  `DefenseAssist` int(11) NOT NULL DEFAULT '0',
  `Kill` int(11) NOT NULL DEFAULT '0',
  `KillAssist` int(11) NOT NULL DEFAULT '0',
  `TDHeal` int(11) NOT NULL DEFAULT '0',
  `Heal` int(11) NOT NULL DEFAULT '0',
  PRIMARY KEY (`PlayerId`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;");

            Execute(@"CREATE TABLE `player_missions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `PlayerId` int(11) NOT NULL,
  `TaskId` int(11) NOT NULL,
  `Progress` int(11) NOT NULL,
  `Date` bigint(20) NOT NULL,
  `RewardType` smallint(6) NOT NULL,
  `Reward` mediumint(9) NOT NULL,
  `Slot` smallint(6) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB  DEFAULT CHARSET=latin1;");
        }

        protected override void Down()
        {
            Execute("DROP TABLE IF EXISTS `player_settings`;");
            Execute("DROP TABLE IF EXISTS `player_mails`;");
            Execute("DROP TABLE IF EXISTS `player_licenses`;");
            Execute("DROP TABLE IF EXISTS `player_deny`;");
            Execute("DROP TABLE IF EXISTS `player_characters`;");
            Execute("DROP TABLE IF EXISTS `player_items`;");
            Execute("DROP TABLE IF EXISTS `players`;");
            Execute("DROP TABLE IF EXISTS `start_items`;");
            Execute("DROP TABLE IF EXISTS `license_rewards`;");
            Execute("DROP TABLE IF EXISTS `shop_iteminfos`;");
            Execute("DROP TABLE IF EXISTS `shop_items`;");
            Execute("DROP TABLE IF EXISTS `shop_prices`;");
            Execute("DROP TABLE IF EXISTS `shop_price_groups`;");
            Execute("DROP TABLE IF EXISTS `shop_effects`;");
            Execute("DROP TABLE IF EXISTS `shop_effect_groups`;");
            Execute("DROP TABLE IF EXISTS `shop_version`;");
        }
    }
}
