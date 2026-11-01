USE ApexPostDb;
GO

-- Session A
BEGIN TRAN;
UPDATE BranchStock
SET QuantityOnHand = QuantityOnHand - 1
WHERE BranchCode = N'BR-001' AND ProductSku = N'SKU-1001';

-- Session B
SELECT *
FROM BranchStock
WHERE BranchCode = N'BR-001' AND ProductSku = N'SKU-1001';

-- Commit session A when ready.
COMMIT TRAN;
GO
