USE ApexPostDb;
GO

SELECT TOP (10)
	b.Region,
	l.ProductSku,
	SUM(l.Quantity * l.UnitPrice) AS Revenue
FROM CounterTransactions t
INNER JOIN TransactionLines l ON l.CounterTransactionId = t.Id
INNER JOIN Branches b ON b.Code = t.BranchCode
WHERE l.ProductSku IS NOT NULL
GROUP BY b.Region, l.ProductSku
ORDER BY Revenue DESC, b.Region, l.ProductSku;
GO
