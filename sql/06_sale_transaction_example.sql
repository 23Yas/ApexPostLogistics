USE ApexPostDb;
GO
SET XACT_ABORT ON;
BEGIN TRY
	BEGIN TRAN;

	DECLARE @BranchCode nvarchar(32) = N'BR-001';
	DECLARE @CounterId nvarchar(32) = N'C1';
	DECLARE @ClerkId uniqueidentifier = '11111111-1111-1111-1111-111111111111';
	DECLARE @TransactionId uniqueidentifier = NEWID();
	DECLARE @Subtotal decimal(18,2) = 40.00;
	DECLARE @Tax decimal(18,2) = 6.00;
	DECLARE @Total decimal(18,2) = 46.00;

	INSERT INTO CounterTransactions (
		Id, BranchCode, CounterId, ClerkId, PaymentMethod, Status, SubTotal, TaxTotal, Total, CreatedAtUtc, ClosedAtUtc, AuditCorrelationId)
	VALUES (
		@TransactionId, @BranchCode, @CounterId, @ClerkId, 'Card', 'Paid', @Subtotal, @Tax, @Total, SYSUTCDATETIME(), SYSUTCDATETIME(), 'tx-demo-001');

	INSERT INTO TransactionLines (
		Id, Description, ProductSku, Quantity, UnitPrice, TaxCategory, CounterTransactionId, CreatedAtUtc)
	VALUES (
		NEWID(), N'Packing Tape', N'SKU-1002', 2, 12.00, 'Standard', @TransactionId, SYSUTCDATETIME());

	UPDATE BranchStock
	   SET QuantityOnHand = QuantityOnHand - 2
	 WHERE BranchCode = @BranchCode AND ProductSku = N'SKU-1002';

	COMMIT TRAN;
END TRY
BEGIN CATCH
	IF XACT_STATE() <> 0
		ROLLBACK TRAN;

	THROW;
END CATCH;
GO
