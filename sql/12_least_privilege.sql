USE ApexPostDb;
GO

-- Least-privilege role for the application. Run once as an administrator.
-- The app logs in with a SQL or Windows login mapped to this database user and gets only this role.

IF DATABASE_PRINCIPAL_ID('ApexPostApp') IS NULL
	CREATE ROLE ApexPostApp;
GO

-- Read and write only the tables the application uses. No DDL, no schema changes.
GRANT SELECT, INSERT, UPDATE ON dbo.Branches              TO ApexPostApp;
GRANT SELECT, INSERT, UPDATE ON dbo.Counters              TO ApexPostApp;
GRANT SELECT, INSERT, UPDATE ON dbo.Staff                 TO ApexPostApp;
GRANT SELECT, INSERT, UPDATE ON dbo.Products              TO ApexPostApp;
GRANT SELECT, INSERT, UPDATE ON dbo.BranchStock           TO ApexPostApp;
GRANT SELECT, INSERT         ON dbo.StockAdjustments      TO ApexPostApp;
GRANT SELECT                 ON dbo.ShippingZones         TO ApexPostApp;
GRANT SELECT                 ON dbo.ShippingRates         TO ApexPostApp;
GRANT SELECT, INSERT, UPDATE ON dbo.Shipments             TO ApexPostApp;
GRANT SELECT, INSERT, UPDATE ON dbo.CounterTransactions   TO ApexPostApp;
GRANT SELECT, INSERT, UPDATE ON dbo.TransactionLines      TO ApexPostApp;
GRANT SELECT, INSERT         ON dbo.Payments              TO ApexPostApp;
GRANT SELECT, INSERT, UPDATE ON dbo.MoneyOrders           TO ApexPostApp;
GRANT SELECT, INSERT         ON dbo.IdentityVerifications TO ApexPostApp;
GRANT SELECT                 ON dbo.Roles                 TO ApexPostApp;
GRANT SELECT                 ON dbo.ProductCategories     TO ApexPostApp;
GRANT SELECT, INSERT         ON dbo.AuditEntries          TO ApexPostApp;

-- Stored procedures: execute only.
GRANT EXECUTE ON dbo.usp_DailyBranchSales TO ApexPostApp;
GRANT EXECUTE ON dbo.usp_LowStockReport   TO ApexPostApp;
GRANT EXECUTE ON dbo.usp_AdjustStock      TO ApexPostApp;

-- Append-only and no hard deletes: history is never removed by the application.
DENY UPDATE, DELETE ON dbo.AuditEntries          TO ApexPostApp;
DENY UPDATE, DELETE ON dbo.StockAdjustments      TO ApexPostApp;
DENY DELETE         ON dbo.CounterTransactions   TO ApexPostApp;
DENY DELETE         ON dbo.Payments              TO ApexPostApp;
GO

-- To attach a login (run in master, with your own password, never committed):
-- CREATE LOGIN ApexPostAppLogin WITH PASSWORD = '<set-in-user-secrets-or-vault>';
-- USE ApexPostDb; CREATE USER ApexPostAppLogin FOR LOGIN ApexPostAppLogin;
-- ALTER ROLE ApexPostApp ADD MEMBER ApexPostAppLogin;

-- Note: Program.cs calls Database.Migrate() at startup, which needs DDL rights.
-- In production, apply migrations with an administrator account, then run the app as ApexPostAppLogin.