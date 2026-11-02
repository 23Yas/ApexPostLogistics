USE ApexPostDb;
GO

-- Baseline for the four workloads in PERFORMANCE.md. Run each block separately with
-- Include Actual Execution Plan on, and record the STATISTICS output.
-- Date ranges cover the seeded and generated history (roughly the last 12 months).

SET STATISTICS IO, TIME ON;

-- W1: low-stock report by branch
SELECT BranchCode, ProductSku, QuantityOnHand
FROM BranchStock
WHERE BranchCode = N'BR-001' AND QuantityOnHand <= 5
ORDER BY ProductSku;

-- W2: daily sales by branch and date range
SELECT BranchCode, COUNT(*) AS Transactions, SUM(Total) AS Total
FROM CounterTransactions
WHERE BranchCode = N'BR-001'
  AND CreatedAtUtc >= '2025-11-01' AND CreatedAtUtc < '2026-11-03'
GROUP BY BranchCode;

-- W3: shipment search by zone and service level
SELECT ZoneId, ServiceLevel, COUNT(*) AS Shipments, SUM(Price) AS Value
FROM Shipments
WHERE ZoneId = 1 AND ServiceLevel = N'Express'
GROUP BY ZoneId, ServiceLevel;

-- W4: money-order search for auditors
SELECT Id, Amount, Sender, Status
FROM MoneyOrders
WHERE BranchCode = N'BR-002'
  AND CreatedAtUtc >= '2025-11-01' AND CreatedAtUtc < '2026-11-03';

SET STATISTICS IO, TIME OFF;
GO