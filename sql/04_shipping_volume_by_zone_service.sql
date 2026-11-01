USE ApexPostDb;
GO

SELECT
	p.Category AS Zone,
	l.ServiceCode,
	COUNT(*) AS ShipmentCount,
	SUM(l.Quantity * l.UnitPrice) AS TotalValue
FROM CounterTransactions t
INNER JOIN TransactionLines l ON l.CounterTransactionId = t.Id
LEFT JOIN Products p ON p.Sku = l.ProductSku
WHERE l.ServiceCode IS NOT NULL
GROUP BY p.Category, l.ServiceCode
ORDER BY TotalValue DESC, l.ServiceCode;
GO
