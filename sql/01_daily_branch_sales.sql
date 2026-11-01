USE ApexPostDb;
GO

SELECT
	t.BranchCode,
	CAST(t.CreatedAtUtc AS date) AS SaleDate,
	SUM(CASE WHEN l.ProductSku IS NOT NULL THEN l.Quantity * l.UnitPrice ELSE 0 END) AS RetailSales,
	SUM(CASE WHEN l.ServiceCode IS NOT NULL THEN l.Quantity * l.UnitPrice ELSE 0 END) AS ServiceSales
FROM CounterTransactions t
INNER JOIN TransactionLines l ON l.CounterTransactionId = t.Id
GROUP BY t.BranchCode, CAST(t.CreatedAtUtc AS date)
ORDER BY SaleDate, t.BranchCode;
GO
