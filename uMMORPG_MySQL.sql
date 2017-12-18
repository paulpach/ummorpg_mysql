-- For use in a tool like MySQL Workbench to create the database and tables

-- Uncomment DROP TABLE to delete the table if it exists and needs to be replaced

-- DROP TABLE accounts;

CREATE TABLE IF NOT EXISTS accounts (
	name VARCHAR(16) NOT NULL,
	password CHAR(40) NOT NULL,
	banned TINYINT UNSIGNED NOT NULL,
  PRIMARY KEY(name)
);

-- DROP TABLE characters;    

CREATE TABLE IF NOT EXISTS characters (
	name VARCHAR(16) NOT NULL,
	account VARCHAR(16) NOT NULL,
	class VARCHAR(16) NOT NULL,
	x FLOAT NOT NULL,
	y FLOAT NOT NULL,
	z FLOAT NOT NULL,
	level INT NOT NULL,
	health INT NOT NULL,
	mana INT NOT NULL,
	strength INT NOT NULL,
	intelligence INT NOT NULL,
	experience BIGINT NOT NULL,
	skillExperience BIGINT NOT NULL,
	gold BIGINT NOT NULL,
	coins BIGINT NOT NULL,
	online VARCHAR(16)
	deleted TINYINT UNSIGNED NOT NULL,

	PRIMARY KEY name,
    INDEX(account),
	FOREIGN KEY (account)
        REFERENCES accounts(name)
        ON DELETE CASCADE
);
    
-- DROP TABLE character_inventory;

CREATE TABLE IF NOT EXISTS character_inventory (
	character VARCHAR(16) NOT NULL,
	slot INT NOT NULL,
	name VARCHAR(50) NOT NULL,
	valid TINYINT UNSIGNED NOT NULL,
	amount INT NOT NULL,
	petHealth INT NOT NULL,
	petLevel INT NOT NULL,
    petExperience INT NOT NULL,
    
	primary key (character, slot),
	FOREIGN KEY (characters)
        REFERENCES characters(name)
        ON DELETE CASCADE
);
    
-- DROP TABLE character_equipment;

CREATE TABLE IF NOT EXISTS character_equipment (
	character VARCHAR(16) NOT NULL,
	slot INT NOT NULL,
	name VARCHAR(50) NOT NULL,
	valid TINYINT UNSIGNED NOT NULL,
	amount INT NOT NULL,

    primary key (character, slot),
	FOREIGN KEY (characters)
        REFERENCES characters(name)
        ON DELETE CASCADE
);

-- DROP TABLE character_skills;

CREATE TABLE IF NOT EXISTS character_skills (
	character VARCHAR(16) NOT NULL,
	name VARCHAR(50) NOT NULL,
	learned TINYINT UNSIGNED NOT NULL,
	level INT NOT NULL,
	castTimeEnd FLOAT NOT NULL,
	cooldownEnd FLOAT NOT NULL,
	buffTimeEnd FLOAT NOT NULL,

    INDEX(character),
	FOREIGN KEY (characters)
        REFERENCES characters(name)
        ON DELETE CASCADE
);
    
-- DROP TABLE character_quests;

CREATE TABLE IF NOT EXISTS character_quests (
	character VARCHAR(16) NOT NULL,
	name VARCHAR(50) NOT NULL,
	killed INT NOT NULL,
	completed TINYINT UNSIGNED NOT NULL,

    INDEX(character),
	FOREIGN KEY (characters)
        REFERENCES characters(name)
        ON DELETE CASCADE
);

-- DROP TABLE character_orders;

CREATE TABLE IF NOT EXISTS character_orders (
	orderid BIGINT NOT NULL AUTO_INCREMENT,
    character VARCHAR(16) NOT NULL,
    coins BIGINT NOT NULL,
    processed BIGINT NOT NULL,
    
	PRIMARY KEY(orderid),
    INDEX(character),
	FOREIGN KEY (characters)
        REFERENCES characters(name)
        ON DELETE CASCADE
);	
	

-- DROP TABLE guild_info;

CREATE TABLE IF NOT EXISTS guild_info (
	name VARCHAR(16) NOT NULL,
	notice TEXT NOT NULL,
	PRIMARY KEY(name)
);

-- DROP TABLE guild_members;

CREATE TABLE IF NOT EXISTS guild_members (
	guild VARCHAR(16) NOT NULL,
	character VARCHAR(16) NOT NULL,
	rank INT NOT NULL,  
  	PRIMARY KEY(guild, character),
	FOREIGN KEY (characters)
        REFERENCES characters(name)
        ON DELETE CASCADE
	FOREIGN KEY (guild)
        REFERENCES guild_info(name)
        ON DELETE CASCADE
);
 
-- ---------------------------------------------------------------------------------------
