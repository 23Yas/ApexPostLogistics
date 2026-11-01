USE ApexPostDb;
GO

SELECT
	b.Code AS BranchCode,
	b.Name,
	b.Region
FROM Branches b
LEFT JOIN CounterTransactions t
	ON t.BranchCode = b.Code
   AND CAST(t.CreatedAtUtc AS date) = CAST(SYSDATETIME() AS date)
WHERE t.Id IS NULL
ORDER BY b.Code;
GO
