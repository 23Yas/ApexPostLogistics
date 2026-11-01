USE ApexPostDb;
GO

SET STATISTICS IO, TIME ON;

SELECT *
FROM BranchStock
WHERE QuantityOnHand <= 5
ORDER BY BranchCode, ProductSku;

SELECT *
FROM CounterTransactions
WHERE YEAR(CreatedAtUtc) = 2026
ORDER BY BranchCode, CreatedAtUtc;

SELECT *
FROM MoneyOrders
WHERE Sender LIKE '%123%'
ORDER BY BranchCode, CreatedAtUtc;
GO
