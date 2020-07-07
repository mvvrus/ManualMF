﻿CREATE TABLE [dbo].[PERMISSIONS] (
    [UPN]           NVARCHAR (64)  NOT NULL,
    [VALID_UNTIL]   DATETIME       NOT NULL,
    [FROM_IP]       VARBINARY (16) NULL,
    [REQUEST_STATE] TINYINT        NOT NULL,
    [EPACCESSTOKEN] NVARCHAR(10) NULL, 
    PRIMARY KEY CLUSTERED ([UPN] ASC)
);

