USE ApexPostDb;
GO

SELECT
	b.Code AS BranchCode,
	b.Name,
	COUNT(DISTINCT mo.Id) AS MoneyOrderCount,
	COUNT(DISTINCT iv.Id) AS IdentityVerificationCount
FROM Branches b
LEFT JOIN MoneyOrders mo ON mo.BranchCode = b.Code
LEFT JOIN IdentityVerifications iv ON iv.BranchCode = b.Code
GROUP BY b.Code, b.Name
ORDER BY b.Code;
GO
