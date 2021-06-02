BEGIN TRANSACTION;
DROP TABLE IF EXISTS `RulesFw`;
CREATE TABLE IF NOT EXISTS `RulesFw` (
	`RuleName`	TEXT,
	`Direction`	INTEGER,
	`Protocole`	INTEGER,
	`IsModeManuel`	INTEGER,
	`IsEnableOnlyFileName`	INTEGER,
	`FilePath`	TEXT,
	`DateCreation`	TEXT,
	`DateLastUpdate`	TEXT,
	PRIMARY KEY(`RuleName`,`Direction`,`Protocole`)
);
COMMIT;
