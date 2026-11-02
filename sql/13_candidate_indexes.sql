USE ApexPostDb;
GO

-- Candidate indexes, one per workload. Create ONE at a time, measure, keep or drop.
-- Keep only the indexes that the measurements justify. Each kept index must also be added to
-- the EF configuration and a migration, so the repository and the database stay in step.

-- W1: low-stock by branch. Replaces IX_BranchStock_BranchCode_QuantityOnHand (same keys, now covering).
-- DROP INDEX IF EXISTS IX_BranchStock_BranchCode_QuantityOnHand ON BranchStock;
-- CREATE NONCLUSTERED INDEX IX_BranchStock_LowStock
--   ON BranchStock (BranchCode, QuantityOnHand)
--   INCLUDE (ProductSku, ReorderThreshold);

-- W2: daily sales. Covering for the daily total.
-- CREATE NONCLUSTERED INDEX IX_CounterTransactions_BranchCode_CreatedAtUtc_Covering
--   ON CounterTransactions (BranchCode, CreatedAtUtc)
--   INCLUDE (Total, Status);

-- W3: shipments by zone and service level. New index.
-- CREATE NONCLUSTERED INDEX IX_Shipments_ZoneId_ServiceLevel
--   ON Shipments (ZoneId, ServiceLevel)
--   INCLUDE (Price);

-- W4: money orders for auditors. Covering version of the existing index.
-- CREATE NONCLUSTERED INDEX IX_MoneyOrders_BranchCode_CreatedAtUtc_Covering
--   ON MoneyOrders (BranchCode, CreatedAtUtc)
--   INCLUDE (Amount, Status);

-- To remove a candidate after measuring it:
-- DROP INDEX IF EXISTS <name> ON <table>;
GO