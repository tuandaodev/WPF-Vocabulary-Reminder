CREATE TABLE IF NOT EXISTS Dictionary (
    Id INTEGER PRIMARY KEY AUTOINCREMENT, 
    Name NVARCHAR(2048) NULL, 
    Description NVARCHAR(2048) NULL, 
    Status INTEGER NULL DEFAULT 0);

CREATE TABLE IF NOT EXISTS "Vocabulary" (
    "Id" INTEGER,
    "Word" NVARCHAR(2048) NOT NULL,
    "WordId" NVARCHAR(2048),
    "Type" NVARCHAR(100),
    "Ipa" NVARCHAR(100),
    "Ipa2" NVARCHAR(100),
    "Translate" NVARCHAR(2048),
    "Define" NVARCHAR(2048),
    "Example" NVARCHAR(2048),
    "Example2" NVARCHAR(2048),
    "PlayURL" NVARCHAR(2048),
    "PlayURL2" NVARCHAR(2048),
    "Related" NVARCHAR(2048),
    "Status" INTEGER DEFAULT 1,
    "Data" TEXT,
    "ViewedDate" INTEGER,
    "LearnedDate" INTEGER,
    "CreatedDate" INTEGER,
    "NextReviewDate" INTEGER,
    "Interval" INTEGER,
    "ReviewCount" INTEGER,
    "LapseCount" INTEGER,
    "EaseFactor" REAL,
    PRIMARY KEY("Id" AUTOINCREMENT));

CREATE TABLE IF NOT EXISTS "VocabularyMappings" (
    "DictionaryId" INTEGER,
    "VocabularyId" INTEGER,
    PRIMARY KEY("DictionaryId","VocabularyId"));

INSERT OR IGNORE INTO Dictionary (Id, Name, Description, Status)
SELECT 1, 'Default', 'Default dictionary', 1
WHERE NOT EXISTS (SELECT 1 FROM Dictionary WHERE Id = 1);