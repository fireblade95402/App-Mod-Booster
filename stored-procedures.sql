-- Stored Procedures for Expense Management System
-- All CRUD operations are performed through stored procedures to avoid direct T-SQL in app code

SET NOCOUNT ON;
GO

-- =============================================
-- Expense Category Procedures
-- =============================================

-- Get all active categories
CREATE OR ALTER PROCEDURE dbo.GetExpenseCategories
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        CategoryId,
        CategoryName,
        IsActive
    FROM dbo.ExpenseCategories
    WHERE IsActive = 1
    ORDER BY CategoryName;
END
GO

-- =============================================
-- Expense Status Procedures
-- =============================================

-- Get all expense statuses
CREATE OR ALTER PROCEDURE dbo.GetExpenseStatuses
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        StatusId,
        StatusName
    FROM dbo.ExpenseStatus
    ORDER BY StatusId;
END
GO

-- =============================================
-- User Procedures
-- =============================================

-- Get all users
CREATE OR ALTER PROCEDURE dbo.GetUsers
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.UserId,
        u.UserName,
        u.Email,
        u.RoleId,
        r.RoleName,
        u.ManagerId,
        u.IsActive,
        u.CreatedAt
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
    WHERE u.IsActive = 1
    ORDER BY u.UserName;
END
GO

-- Get user by ID
CREATE OR ALTER PROCEDURE dbo.GetUserById
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.UserId,
        u.UserName,
        u.Email,
        u.RoleId,
        r.RoleName,
        u.ManagerId,
        u.IsActive,
        u.CreatedAt
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
    WHERE u.UserId = @UserId;
END
GO

-- =============================================
-- Expense Procedures
-- =============================================

-- Get all expenses with details
CREATE OR ALTER PROCEDURE dbo.GetExpenses
    @UserId INT = NULL,
    @StatusId INT = NULL,
    @CategoryId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        rm.UserName AS ReviewedByName,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    LEFT JOIN dbo.Users rm ON e.ReviewedBy = rm.UserId
    WHERE (@UserId IS NULL OR e.UserId = @UserId)
      AND (@StatusId IS NULL OR e.StatusId = @StatusId)
      AND (@CategoryId IS NULL OR e.CategoryId = @CategoryId)
    ORDER BY e.ExpenseDate DESC, e.CreatedAt DESC;
END
GO

-- Get expense by ID
CREATE OR ALTER PROCEDURE dbo.GetExpenseById
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        rm.UserName AS ReviewedByName,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    LEFT JOIN dbo.Users rm ON e.ReviewedBy = rm.UserId
    WHERE e.ExpenseId = @ExpenseId;
END
GO

-- Get pending expenses for approval (Manager view)
CREATE OR ALTER PROCEDURE dbo.GetPendingExpenses
    @ManagerId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    WHERE s.StatusName = 'Submitted'
      AND (@ManagerId IS NULL OR u.ManagerId = @ManagerId)
    ORDER BY e.SubmittedAt ASC;
END
GO

-- Create new expense
CREATE OR ALTER PROCEDURE dbo.CreateExpense
    @UserId INT,
    @CategoryId INT,
    @AmountMinor INT,
    @Currency NVARCHAR(3) = 'GBP',
    @ExpenseDate DATE,
    @Description NVARCHAR(1000) = NULL,
    @ReceiptFile NVARCHAR(500) = NULL,
    @StatusName NVARCHAR(50) = 'Draft'
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StatusId INT;
    DECLARE @NewExpenseId INT;
    
    -- Get StatusId
    SELECT @StatusId = StatusId 
    FROM dbo.ExpenseStatus 
    WHERE StatusName = @StatusName;
    
    -- Insert expense
    INSERT INTO dbo.Expenses (
        UserId,
        CategoryId,
        StatusId,
        AmountMinor,
        Currency,
        ExpenseDate,
        Description,
        ReceiptFile,
        SubmittedAt,
        CreatedAt
    )
    VALUES (
        @UserId,
        @CategoryId,
        @StatusId,
        @AmountMinor,
        @Currency,
        @ExpenseDate,
        @Description,
        @ReceiptFile,
        CASE WHEN @StatusName = 'Submitted' THEN SYSUTCDATETIME() ELSE NULL END,
        SYSUTCDATETIME()
    );
    
    SET @NewExpenseId = SCOPE_IDENTITY();
    
    -- Return the created expense
    EXEC dbo.GetExpenseById @ExpenseId = @NewExpenseId;
END
GO

-- Update expense
CREATE OR ALTER PROCEDURE dbo.UpdateExpense
    @ExpenseId INT,
    @CategoryId INT = NULL,
    @AmountMinor INT = NULL,
    @ExpenseDate DATE = NULL,
    @Description NVARCHAR(1000) = NULL,
    @ReceiptFile NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE dbo.Expenses
    SET 
        CategoryId = COALESCE(@CategoryId, CategoryId),
        AmountMinor = COALESCE(@AmountMinor, AmountMinor),
        ExpenseDate = COALESCE(@ExpenseDate, ExpenseDate),
        Description = COALESCE(@Description, Description),
        ReceiptFile = COALESCE(@ReceiptFile, ReceiptFile)
    WHERE ExpenseId = @ExpenseId;
    
    -- Return the updated expense
    EXEC dbo.GetExpenseById @ExpenseId = @ExpenseId;
END
GO

-- Submit expense (change status from Draft to Submitted)
CREATE OR ALTER PROCEDURE dbo.SubmitExpense
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StatusId INT;
    
    -- Get StatusId for 'Submitted'
    SELECT @StatusId = StatusId 
    FROM dbo.ExpenseStatus 
    WHERE StatusName = 'Submitted';
    
    UPDATE dbo.Expenses
    SET 
        StatusId = @StatusId,
        SubmittedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
    
    -- Return the updated expense
    EXEC dbo.GetExpenseById @ExpenseId = @ExpenseId;
END
GO

-- Approve expense (Manager action)
CREATE OR ALTER PROCEDURE dbo.ApproveExpense
    @ExpenseId INT,
    @ReviewedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StatusId INT;
    
    -- Get StatusId for 'Approved'
    SELECT @StatusId = StatusId 
    FROM dbo.ExpenseStatus 
    WHERE StatusName = 'Approved';
    
    UPDATE dbo.Expenses
    SET 
        StatusId = @StatusId,
        ReviewedBy = @ReviewedBy,
        ReviewedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
    
    -- Return the updated expense
    EXEC dbo.GetExpenseById @ExpenseId = @ExpenseId;
END
GO

-- Reject expense (Manager action)
CREATE OR ALTER PROCEDURE dbo.RejectExpense
    @ExpenseId INT,
    @ReviewedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StatusId INT;
    
    -- Get StatusId for 'Rejected'
    SELECT @StatusId = StatusId 
    FROM dbo.ExpenseStatus 
    WHERE StatusName = 'Rejected';
    
    UPDATE dbo.Expenses
    SET 
        StatusId = @StatusId,
        ReviewedBy = @ReviewedBy,
        ReviewedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
    
    -- Return the updated expense
    EXEC dbo.GetExpenseById @ExpenseId = @ExpenseId;
END
GO

-- Delete expense (soft delete or hard delete based on business rules)
CREATE OR ALTER PROCEDURE dbo.DeleteExpense
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Hard delete for demo purposes
    -- In production, consider soft delete by updating a IsDeleted flag
    DELETE FROM dbo.Expenses
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @ExpenseId AS DeletedExpenseId;
END
GO

-- Get expense summary statistics
CREATE OR ALTER PROCEDURE dbo.GetExpenseSummary
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        COUNT(*) AS TotalExpenses,
        SUM(CASE WHEN s.StatusName = 'Draft' THEN 1 ELSE 0 END) AS DraftCount,
        SUM(CASE WHEN s.StatusName = 'Submitted' THEN 1 ELSE 0 END) AS SubmittedCount,
        SUM(CASE WHEN s.StatusName = 'Approved' THEN 1 ELSE 0 END) AS ApprovedCount,
        SUM(CASE WHEN s.StatusName = 'Rejected' THEN 1 ELSE 0 END) AS RejectedCount,
        SUM(AmountMinor) AS TotalAmountMinor,
        SUM(CASE WHEN s.StatusName = 'Approved' THEN AmountMinor ELSE 0 END) AS ApprovedAmountMinor
    FROM dbo.Expenses e
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    WHERE @UserId IS NULL OR e.UserId = @UserId;
END
GO

PRINT 'Stored procedures created successfully!';
GO
