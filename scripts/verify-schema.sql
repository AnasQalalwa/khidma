-- §5.3 schema proof against a live SQL Server database named Khidma.
-- Run: sqlcmd -S "(localdb)\MSSQLLocalDB" -d Khidma -C -i scripts/verify-schema.sql

SET NOCOUNT ON;

PRINT '=== Filtered and unique indexes ===';
SELECT
    i.name AS index_name,
    OBJECT_NAME(i.object_id) AS table_name,
    i.is_unique,
    i.filter_definition,
    COL_NAME(ic.object_id, ic.column_id) AS column_name
FROM sys.indexes AS i
INNER JOIN sys.index_columns AS ic
    ON i.object_id = ic.object_id AND i.index_id = ic.index_id
WHERE i.name IN (
        'UX_Offer_OneAcceptedPerRequest',
        'IX_Offers_ServiceRequestId_ProviderId',
        'IX_Reviews_BookingId',
        'IX_Bookings_OfferId',
        'IX_ProviderServices_ProviderProfileId_ServiceId'
    )
    OR i.filter_definition IS NOT NULL
ORDER BY i.name, ic.key_ordinal;

PRINT '=== Check constraints ===';
SELECT
    name,
    OBJECT_NAME(parent_object_id) AS table_name,
    definition
FROM sys.check_constraints
WHERE name IN ('CK_Review_Rating', 'CK_ProviderVerificationDocuments_FileSize')
ORDER BY name;

PRINT '=== Rowversion columns ===';
SELECT
    OBJECT_NAME(c.object_id) AS table_name,
    c.name AS column_name,
    t.name AS type_name
FROM sys.columns AS c
INNER JOIN sys.types AS t ON c.user_type_id = t.user_type_id
WHERE c.name = 'RowVersion'
ORDER BY table_name;

PRINT '=== Money decimals ===';
SELECT
    OBJECT_NAME(c.object_id) AS table_name,
    c.name AS column_name,
    t.name AS type_name,
    c.precision,
    c.scale
FROM sys.columns AS c
INNER JOIN sys.types AS t ON c.user_type_id = t.user_type_id
WHERE c.name IN ('Price', 'FinalPrice', 'BudgetMin', 'BudgetMax')
ORDER BY table_name, column_name;

PRINT '=== String enum columns ===';
SELECT
    OBJECT_NAME(c.object_id) AS table_name,
    c.name AS column_name,
    t.name AS type_name,
    c.max_length
FROM sys.columns AS c
INNER JOIN sys.types AS t ON c.user_type_id = t.user_type_id
WHERE c.name IN ('Status', 'VerificationStatus')
    AND OBJECT_NAME(c.object_id) IN ('Offers', 'Bookings', 'ServiceRequests', 'ProviderProfiles')
ORDER BY table_name, column_name;
