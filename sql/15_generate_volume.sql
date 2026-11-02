USE ApexPostDb;
GO
SET NOCOUNT ON;

-- Adds 50,000 historical transactions, each with 2 lines, 1 payment and 1 shipment, spread over
-- the last 365 days. Fictional data only. Run ONCE; running it again adds another 50,000.
-- All generated sales use the seeded clerk Ava Reed (the clerk id only needs to exist).

IF OBJECT_ID('tempdb..#Tx') IS NOT NULL DROP TABLE #Tx;
IF OBJECT_ID('tempdb..#Plan') IS NOT NULL DROP TABLE #Plan;

SELECT TOP (50000)
	ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n,
	NEWID() AS Id
INTO #Tx
FROM sys.all_objects a
CROSS JOIN sys.all_objects b;

SELECT
	t.n,
	t.Id,
	CHOOSE((t.n % 5) + 1, N'BR-001', N'BR-002', N'BR-003', N'BR-004', N'BR-005') AS BranchCode,
	CONCAT(N'C', (t.n % 4) + 1) AS CounterId,
	CHOOSE((t.n % 4) + 1, N'Cash', N'Card', N'Mobile', N'MoneyOrder') AS PaymentMethod,
	CAST(35 + ((t.n * 37) % 240) AS decimal(18,2)) AS SubTotal,
	CHOOSE((t.n % 3) + 1, N'PARCEL-STANDARD', N'PARCEL-EXPRESS', N'LETTER-REGISTERED') AS ServiceCode,
	CHOOSE((t.n % 3) + 1, N'Standard', N'Express', N'Registered') AS ServiceLevel,
	DATEADD(MINUTE, -(t.n % 525600), SYSUTCDATETIME()) AS CreatedAtUtc
INTO #Plan
FROM #Tx t;

INSERT INTO CounterTransactions (Id, BranchCode, CounterId, ClerkId, PaymentMethod, Status, SubTotal, TaxTotal, Total, CreatedAtUtc, ClosedAtUtc)
SELECT p.Id, p.BranchCode, p.CounterId, '11111111-1111-1111-1111-111111111111', p.PaymentMethod, 'Paid',
       p.SubTotal, ROUND(p.SubTotal * 0.15, 2), ROUND(p.SubTotal * 1.15, 2), p.CreatedAtUtc, p.CreatedAtUtc
FROM #Plan p;

INSERT INTO TransactionLines (Id, CounterTransactionId, Description, ProductSku, ServiceCode, Quantity, UnitPrice, TaxCategory, CreatedAtUtc)
SELECT NEWID(), p.Id, N'Retail item', CONCAT(N'SKU-', 1001 + (p.n % 24)), NULL, 1 + (p.n % 3), ROUND(p.SubTotal / 3, 2), N'Standard', p.CreatedAtUtc
FROM #Plan p;

INSERT INTO TransactionLines (Id, CounterTransactionId, Description, ProductSku, ServiceCode, Quantity, UnitPrice, TaxCategory, CreatedAtUtc)
SELECT NEWID(), p.Id, N'Shipping service', NULL, p.ServiceCode, 1, 12.50, N'Standard', p.CreatedAtUtc
FROM #Plan p;

INSERT INTO Payments (Id, CounterTransactionId, Method, Amount, Status, CreatedAtUtc)
SELECT NEWID(), p.Id, p.PaymentMethod, ROUND(p.SubTotal * 1.15, 2), 'Captured', p.CreatedAtUtc
FROM #Plan p;

INSERT INTO Shipments (Id, CounterTransactionId, BranchCode, ZoneId, ServiceLevel, WeightKg, Price, TrackingReference, Status, CreatedAtUtc)
SELECT NEWID(), p.Id, p.BranchCode, 1 + (p.n % 10), p.ServiceLevel, 1.00, 12.50,
       CONCAT(N'APXV', RIGHT(CONCAT(N'00000000', p.n), 8)), 'Delivered', p.CreatedAtUtc
FROM #Plan p;

DROP TABLE #Plan;
DROP TABLE #Tx;

SELECT COUNT(*) AS CounterTransactions FROM CounterTransactions;
GO