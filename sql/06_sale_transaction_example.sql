USE ApexPostDb;
GO
SET XACT_ABORT ON;
SET NOCOUNT ON;

DECLARE @BranchCode nvarchar(32) = N'BR-001';
DECLARE @CounterId nvarchar(32) = N'C1';
DECLARE @ClerkId uniqueidentifier = '11111111-1111-1111-1111-111111111111';
DECLARE @Sku nvarchar(64) = N'SKU-1002';
DECLARE @Quantity int = 2;
DECLARE @TaxRate decimal(5,4) = 0.15;   -- must match the rate in PricingService

DECLARE @UnitPrice decimal(18,2), @ProductName nvarchar(200);
DECLARE @Subtotal decimal(18,2), @Tax decimal(18,2), @Total decimal(18,2);
DECLARE @TransactionId uniqueidentifier = NEWID();

-- Read the real price and name from the catalogue.
SELECT @UnitPrice = UnitPrice, @ProductName = Name
FROM Products
WHERE Sku = @Sku;

IF @UnitPrice IS NULL
	THROW 50010, N'SKU not found in the catalogue.', 1;

SET @Subtotal = @UnitPrice * @Quantity;
SET @Tax = ROUND(@Subtotal * @TaxRate, 2);
SET @Total = @Subtotal + @Tax;

BEGIN TRY
	BEGIN TRAN;

	-- Lock the stock row, then check it has enough before changing anything.
	DECLARE @OnHand int;
	SELECT @OnHand = QuantityOnHand
	FROM BranchStock WITH (UPDLOCK, ROWLOCK)
	WHERE BranchCode = @BranchCode AND ProductSku = @Sku;

	IF @OnHand IS NULL
		THROW 50011, N'No stock row exists for this branch and SKU.', 1;

	IF @OnHand < @Quantity
		THROW 50012, N'Insufficient stock for this sale.', 1;

	INSERT INTO CounterTransactions (
		Id, BranchCode, CounterId, ClerkId, PaymentMethod, Status,
		SubTotal, TaxTotal, Total, CreatedAtUtc, ClosedAtUtc, AuditCorrelationId)
	VALUES (
		@TransactionId, @BranchCode, @CounterId, @ClerkId, 'Card', 'Paid',
		@Subtotal, @Tax, @Total, SYSUTCDATETIME(), SYSUTCDATETIME(), 'tx-demo-001');

	INSERT INTO TransactionLines (
		Id, Description, ProductSku, Quantity, UnitPrice, TaxCategory, CounterTransactionId, CreatedAtUtc)
	VALUES (
		NEWID(), @ProductName, @Sku, @Quantity, @UnitPrice, 'Standard', @TransactionId, SYSUTCDATETIME());

	INSERT INTO Payments (Id, CounterTransactionId, Method, Amount, Status, CreatedAtUtc)
	VALUES (NEWID(), @TransactionId, 'Card', @Total, 'Captured', SYSUTCDATETIME());

	UPDATE BranchStock
	   SET QuantityOnHand = QuantityOnHand - @Quantity,
	       ModifiedAtUtc = SYSUTCDATETIME()
	 WHERE BranchCode = @BranchCode AND ProductSku = @Sku;

	COMMIT TRAN;
	SELECT @TransactionId AS TransactionId, @Total AS Total;
END TRY
BEGIN CATCH
	IF XACT_STATE() <> 0
		ROLLBACK TRAN;
	THROW;
END CATCH;
GO