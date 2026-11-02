USE ApexPostDb;
GO

-- Top 10 products by revenue PER region. A global TOP 10 would not answer the question.
WITH RegionRevenue AS (
	SELECT b.Region, l.ProductSku, SUM(l.Quantity * l.UnitPrice) AS Revenue
	FROM CounterTransactions t
	INNER JOIN TransactionLines l ON l.CounterTransactionId = t.Id
	INNER JOIN Branches b ON b.Code = t.BranchCode
	WHERE l.ProductSku IS NOT NULL
	  AND t.Status = 'Paid'
	GROUP BY b.Region, l.ProductSku
),
Ranked AS (
	SELECT Region, ProductSku, Revenue,
	       ROW_NUMBER() OVER (PARTITION BY Region ORDER BY Revenue DESC, ProductSku) AS RankInRegion
	FROM RegionRevenue
)
SELECT Region, RankInRegion, ProductSku, Revenue
FROM Ranked
WHERE RankInRegion <= 10
ORDER BY Region, RankInRegion;
GO