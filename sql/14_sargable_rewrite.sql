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