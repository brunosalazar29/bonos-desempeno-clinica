/*
   Prueba técnica - Bonos de desempeño
   Script 2: tabla de bonos, relaciones y reglas persistentes.
   El script 01_schema_seed.sql se conserva sin modificaciones.
*/

USE BonusTest;
GO

IF OBJECT_ID(N'dbo.Bonus', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Bonus
    (
        BonusId          BIGINT IDENTITY(1,1) NOT NULL,
        DepartmentCode   CHAR(5)              NOT NULL,
        EmployeeNumber   INT                  NOT NULL,
        Period           DATE                 NOT NULL,
        BonusTypeId      INT                  NOT NULL,
        FixedAmount      DECIMAL(16,2)        NULL,
        SalaryPercentage DECIMAL(7,4)         NULL,
        ReasonCode       CHAR(6)              NOT NULL,
        GrossAmount      DECIMAL(16,2)        NOT NULL,
        TaxAmount        DECIMAL(16,2)        NOT NULL,
        NetAmount        DECIMAL(16,2)        NOT NULL,
        BonusStateCode   CHAR(4)              NOT NULL,
        IsDeleted        BIT                  NOT NULL
            CONSTRAINT DF_Bonus_IsDeleted DEFAULT (0),
        CreatedBy        VARCHAR(100)         NOT NULL,
        CreatedAt        DATETIME2(3)         NOT NULL
            CONSTRAINT DF_Bonus_CreatedAt DEFAULT (SYSUTCDATETIME()),
        ModifiedBy       VARCHAR(100)         NULL,
        ModifiedAt       DATETIME2(3)         NULL,

        CONSTRAINT PK_Bonus PRIMARY KEY CLUSTERED (BonusId),
        CONSTRAINT FK_Bonus_Employee FOREIGN KEY (DepartmentCode, EmployeeNumber)
            REFERENCES dbo.Employee (DepartmentCode, EmployeeNumber),
        CONSTRAINT FK_Bonus_BonusType FOREIGN KEY (BonusTypeId)
            REFERENCES dbo.BonusType (BonusTypeId),
        CONSTRAINT FK_Bonus_BonusReason FOREIGN KEY (ReasonCode)
            REFERENCES dbo.BonusReason (ReasonCode),
        CONSTRAINT FK_Bonus_State FOREIGN KEY (BonusStateCode)
            REFERENCES dbo.State (StateCode),
        CONSTRAINT CK_Bonus_Period_FirstDay
            CHECK (DAY(Period) = 1),
        CONSTRAINT CK_Bonus_Value_ByType
            CHECK
            (
                (BonusTypeId = 1 AND FixedAmount IS NOT NULL AND FixedAmount > 0 AND SalaryPercentage IS NULL)
                OR
                (BonusTypeId = 2 AND FixedAmount IS NULL AND SalaryPercentage IS NOT NULL
                    AND SalaryPercentage > 0 AND SalaryPercentage <= 100)
            ),
        CONSTRAINT CK_Bonus_Amounts
            CHECK (GrossAmount > 0 AND TaxAmount >= 0 AND NetAmount = GrossAmount - TaxAmount)
    );
END;
GO

/*
   Una fila anulada o eliminada deja de bloquear el periodo. El índice también
   protege la regla ante dos altas concurrentes para el mismo empleado y mes.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Bonus')
      AND name = N'UX_Bonus_Active_Employee_Period'
)
BEGIN
    CREATE UNIQUE INDEX UX_Bonus_Active_Employee_Period
        ON dbo.Bonus (DepartmentCode, EmployeeNumber, Period)
        WHERE IsDeleted = 0 AND BonusStateCode <> '0203';
END;
GO

/*
   Apoya la ordenación por periodo y los filtros más comunes del listado.
   La fila está cubierta con los campos necesarios para presentar la grilla.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Bonus')
      AND name = N'IX_Bonus_List_Period_State'
)
BEGIN
    CREATE INDEX IX_Bonus_List_Period_State
        ON dbo.Bonus (Period DESC, BonusStateCode, DepartmentCode, EmployeeNumber)
        INCLUDE (BonusTypeId, ReasonCode, FixedAmount, SalaryPercentage,
                 GrossAmount, TaxAmount, NetAmount, CreatedAt)
        WHERE IsDeleted = 0;
END;
GO
