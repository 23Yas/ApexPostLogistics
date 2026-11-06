USE ApexPostDb;
GO
SET NOCOUNT ON;

-- Adds 50,000 historical money orders across the five seeded branches, spread over the last 365 days.
-- Fictional data only. The guard below stops a second run.

IF (SELECT COUNT(*) FROM MoneyOrders) >= 50000
BEGIN
    THROW 50020, N'Money orders already generated. Do not run this script again.', 1;
END;

WITH Nums AS (
    SELECT TOP (50000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_objects a
    CROSS JOIN sys.all_objects b
)
INSERT INTO MoneyOrders (Id, TransactionId, BranchCode, Amount, Sender, Recipient, Fee,
                         RequiresIdentityVerification, IdentityVerificationComplete, Status, CreatedAtUtc)
SELECT
    NEWID(),
    NULL,
    CHOOSE((n % 5) + 1, 'BR-001', 'BR-002', 'BR-003', 'BR-004', 'BR-005'),
    CAST(20 + ((n * 53) % 1980) AS decimal(18,2)),
    CONCAT(N'Sender ', n),
    CONCAT(N'Recipient ', n),
    CAST(2.50 AS decimal(18,2)),
    CASE WHEN (20 + ((n * 53) % 1980)) > 1500 THEN 1 ELSE 0 END,
    CASE WHEN (20 + ((n * 53) % 1980)) > 1500 THEN 1 ELSE 0 END,
    'Approved',
    DATEADD(MINUTE, -(n % 525600), SYSUTCDATETIME())
FROM Nums;

SELECT COUNT(*) AS MoneyOrders FROM MoneyOrders;
GO