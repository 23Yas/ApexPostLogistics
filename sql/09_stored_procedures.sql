USE ApexPostDb;
GO

CREATE OR ALTER PROCEDURE dbo.usp_DailyBranchSales
	@BranchCode nvarchar(32),
	@Date date
AS
BEGIN
	SET NOCOUNT ON;

	IF @BranchCode IS NULL OR LTRIM(RTRIM(@BranchCode)) = N''
		THROW 50001, N'BranchCode is required.', 1;

	IF @Date IS NULL
		THROW 50002, N'Date is required.', 1;

	SELECT
		t.BranchCode,
		CAST(t.CreatedAtUtc AS date) AS SaleDate,
		SUM(CASE WHEN l.ProductSku IS NOT NULL THEN l.Quantity * l.UnitPrice ELSE 0 END) AS RetailTotal,
		SUM(CASE WHEN l.ServiceCode IS NOT NULL THEN l.Quantity * l.UnitPrice ELSE 0 END) AS ServiceTotal
	FROM CounterTransactions t
	INNER JOIN TransactionLines l ON l.CounterTransactionId = t.Id
	WHERE t.BranchCode = @BranchCode
	  AND CAST(t.CreatedAtUtc AS date) = @Date
	GROUP BY t.BranchCode, CAST(t.CreatedAtUtc AS date)
	ORDER BY SaleDate;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_LowStockReport
	@BranchCode nvarchar(32) = NULL,
	@Threshold int
AS
BEGIN
	SET NOCOUNT ON;

	IF @Threshold < 0
		THROW 50003, N'Threshold must be zero or greater.', 1;

	SELECT
		bs.BranchCode,
		bs.ProductSku,
		bs.QuantityOnHand,
		bs.ReorderThreshold,
		p.Name AS ProductName
	FROM BranchStock bs
	INNER JOIN Products p ON p.Sku = bs.ProductSku
	WHERE bs.QuantityOnHand <= @Threshold
	  AND (@BranchCode IS NULL OR bs.BranchCode = @BranchCode)
	ORDER BY bs.BranchCode, bs.ProductSku;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AdjustStock
	@BranchCode nvarchar(32),
	@Sku nvarchar(64),
	@Delta int,
	@Reason nvarchar(200),
	@ChangedBy nvarchar(128)
AS
BEGIN
	SET NOCOUNT ON;
	SET XACT_ABORT ON;

	IF @BranchCode IS NULL OR LTRIM(RTRIM(@BranchCode)) = N''
		THROW 50004, N'BranchCode is required.', 1;

	IF @Sku IS NULL OR LTRIM(RTRIM(@Sku)) = N''
		THROW 50005, N'Sku is required.', 1;

	IF @Reason IS NULL OR LTRIM(RTRIM(@Reason)) = N''
		THROW 50006, N'Reason is required.', 1;

	IF @ChangedBy IS NULL OR LTRIM(RTRIM(@ChangedBy)) = N''
		THROW 50007, N'ChangedBy is required.', 1;

	BEGIN TRY
		BEGIN TRAN;

		DECLARE @CurrentQty int;
		SELECT @CurrentQty = QuantityOnHand
		FROM BranchStock
		WHERE BranchCode = @BranchCode AND ProductSku = @Sku;

		IF @CurrentQty IS NULL
			THROW 50008, N'No stock row exists for the supplied branch and SKU.', 1;

		UPDATE BranchStock
		   SET QuantityOnHand = QuantityOnHand + @Delta,
			   ModifiedAtUtc = SYSUTCDATETIME()
		 WHERE BranchCode = @BranchCode AND ProductSku = @Sku;

		INSERT INTO StockAdjustments (
			BranchCode, ProductSku, Delta, Reason, ChangedBy, CreatedAtUtc)
		VALUES (
			@BranchCode, @Sku, @Delta, @Reason, @ChangedBy, SYSUTCDATETIME());

		COMMIT TRAN;
	END TRY
	BEGIN CATCH
		IF XACT_STATE() <> 0
			ROLLBACK TRAN;
		THROW;
	END CATCH;
END;
GO
