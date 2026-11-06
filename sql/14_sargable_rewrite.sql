USE ApexPostDb;
GO

SET STATISTICS IO, TIME ON;

-- Before: the function on the column prevents an index seek.
SELECT Id, BranchCode, Total
FROM CounterTransactions
WHERE YEAR(CreatedAtUtc) = 2026;

-- After: the same rows selected with a range on the bare column.
SELECT Id, BranchCode, Total
FROM CounterTransactions
WHERE CreatedAtUtc >= '2026-01-01' AND CreatedAtUtc < '2027-01-01';

SET STATISTICS IO, TIME OFF;
GO

-- Estimated-versus-actual investigation (PERFORMANCE.md section 6).
-- 1. Run the W2 query and note the estimated and actual rows on the scan or seek operator.
-- 2. Refresh statistics and run it again:
-- UPDATE STATISTICS dbo.CounterTransactions WITH FULLSCAN;
-- 3. Compare the estimates again and record the difference in PERFORMANCE.md.
GO


-- Implicit conversion on the column. BranchCode is varchar(32), but the workload uses N'...' literals,
-- so SQL Server converts the column for every row, which prevents an index seek.
-- Before: as in sql/08 (already measured with the W2 candidate index: 322 logical reads, index scan).
-- After: varchar literal, same rows.
SET STATISTICS IO, TIME ON;

SELECT BranchCode, COUNT(*) AS Transactions, SUM(Total) AS Total
FROM CounterTransactions
WHERE BranchCode = 'BR-001'
  AND CreatedAtUtc >= '2025-11-01' AND CreatedAtUtc < '2026-11-03'
GROUP BY BranchCode;

SET STATISTICS IO, TIME OFF;
GO



-- W1 after: plain varchar literal (before: sql/08, N'BR-001')
SET STATISTICS IO, TIME ON;

SELECT BranchCode, ProductSku, QuantityOnHand
FROM BranchStock
WHERE BranchCode = 'BR-001' AND QuantityOnHand <= 5
ORDER BY ProductSku;

SET STATISTICS IO, TIME OFF;
GO

-- W3 after: plain varchar literal (before: sql/08, N'Express')
SET STATISTICS IO, TIME ON;

SELECT ZoneId, ServiceLevel, COUNT(*) AS Shipments, SUM(Price) AS Value
FROM Shipments
WHERE ZoneId = 1 AND ServiceLevel = 'Express'
GROUP BY ZoneId, ServiceLevel;

SET STATISTICS IO, TIME OFF;
GO

-- W4 after: plain varchar literal (before: sql/08, N'BR-002')
SET STATISTICS IO, TIME ON;

SELECT Id, Amount, Sender, Status
FROM MoneyOrders
WHERE BranchCode = 'BR-002'
  AND CreatedAtUtc >= '2025-11-01' AND CreatedAtUtc < '2026-11-03';

SET STATISTICS IO, TIME OFF;
GO