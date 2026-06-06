# Sql Queries Module
This module is designed to empower administrators and developers by providing a secure and flexible way to perform direct database queries within the VirtoCommerce environment. It integrates seamlessly with the platform and supports a variety of query types, enhancing the ability to retrieve, analyze, and manage business data without external tools.

##  Key features
* Execute custom SQL queries against the VirtoCommerce databases.
* Integrate with the platform’s security and permissions system to control query access.
* Return results in user-friendly formats for reporting and analysis: HTML, PDF, CSV, XLSX.
* Supports query parameters: Short Text, Date Time, Boolean, Integer, Decimal.
* Supports multiple database providers: SQL Server (default), MySQL, and PostgreSQL.
* Supports multiple connection strings.
* Platform Backup & Restore support.



## Screenshots
### No SQL queries yet
<img width="691" height="427" alt="image" src="https://github.com/user-attachments/assets/50dd2661-7068-423a-982e-1adacebeb894" />

### List of reports
<img width="692" height="417" alt="image" src="https://github.com/user-attachments/assets/14487ead-29eb-4795-ba81-b3a51ce7fbe9" />

### Create a new query with live preview
<img width="792" height="882" alt="image" src="https://github.com/user-attachments/assets/ad2e1632-fda0-4791-bee1-01edb6d6491b" />

### Edit query parameters
<img width="821" height="370" alt="image" src="https://github.com/user-attachments/assets/8b8b5975-9bd6-4829-8c55-03b9bf6dcfe0" />

### Run report
<img width="806" height="875" alt="image" src="https://github.com/user-attachments/assets/9a7c0b7e-635d-44f4-80cc-bd12e62a90ea" />

### Export to file
<img width="627" height="681" alt="image" src="https://github.com/user-attachments/assets/7d3c0d9a-b843-4c3a-aacb-772204a541f4" />



## Configuration
1. Create a new read-only connection string with prefix `SqlQueries.`. Ex: `SqlQueries.VirtoCommerce`.
1. Signin to Virto Commerce Back Office with `sql-queries` permissions
1. Navigate to Sql Queries section.
1. Create a new report (define parameters if required).
1. Grant 'sql-queries:access' and  'sql-queries:read' to other employees.
2. Run report

## Sample reports

### Business Report SQL Query: Monthly Order Summary per Store
```sql
SELECT
  StoreId,
  COUNT(*) AS TotalOrders,
  SUM(SubTotal) AS TotalSubTotal,
  SUM(ShippingTotal) AS TotalShipping,
  SUM(TaxTotal) AS TotalTax,
  SUM(PaymentTotal) AS TotalPayment,
  SUM(FeeTotal) AS TotalFees,
  SUM(DiscountTotal) AS TotalDiscounts,
  SUM(Total) AS TotalOrderAmount,
  SUM(SubTotal + ShippingTotal + TaxTotal + PaymentTotal + FeeTotal - DiscountTotal) AS TotalCalculatedAmount
FROM
  dbo.CustomerOrder
GROUP BY
  StoreId
ORDER BY
  StoreId;

```

### To get summary per coupon (who used what, how many times)
```sql
SELECT 
    od.CouponCode,
    co.CustomerId,
    co.CustomerName,
    u.UserName AS CustomerUserName,
    COUNT(DISTINCT co.Id) AS OrdersWithCoupon,
    SUM(od.DiscountAmount) AS TotalDiscountAmount
FROM CustomerOrder co
INNER JOIN OrderDiscount od ON co.Id = od.CustomerOrderId
LEFT JOIN AspNetUsers u ON co.CustomerId = u.Id
WHERE od.CouponCode IS NOT NULL
GROUP BY od.CouponCode, co.CustomerId, co.CustomerName, u.UserName
ORDER BY od.CouponCode;

```

### Admin Report: Return Tables (Record Count + Size in MB)

Varables:
* MinSizeMB - integer

```sql
SELECT 
    t.name AS TableName,
    p.rows AS RecordCount,
    (a.total_pages * 8.0) / 1024 AS SizeMB
FROM 
    sys.tables t
INNER JOIN      
    sys.indexes i ON t.object_id = i.object_id
INNER JOIN 
    sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
INNER JOIN 
    sys.allocation_units a ON p.partition_id = a.container_id
WHERE 
    i.index_id <= 1 AND (a.total_pages * 8.0) / 1024 >= @MinSizeMB 

ORDER BY 
    SizeMB DESC;
```

## Permissions
The module registers the following permissions:
* sql-queries:access
* sql-queries:create
* sql-queries:read
* sql-queries:update
* sql-queries:delete
  
Assign these permissions to appropriate roles/users to manage access.

## Claude Code skills

This repo ships two project-scoped [Claude Code](https://claude.com/claude-code) skills under
`.claude/skills/` that automate the module's reporting through its REST API. Pick by intent:

* **vc-sql-queries-admin** — *authoring & lifecycle.* Create, edit, test/preview, search, or
  delete saved reports and write/validate SQL. Requires `sql-queries:create` / `:update` /
  `:delete` / `:read`.
* **vc-sql-queries-business** — *run & export only.* Find an existing report, run it with
  parameter values, read the rows, and export to CSV / XLSX / PDF / HTML. Requires
  `sql-queries:access` + `sql-queries:read`. Never writes SQL.

Both skills are grounded in the module source (controller routes, model field names, and the
`sql-queries:*` permissions) and enforce the module's safety rules: read-only `SqlQueries.`-prefixed
connections only, fully parameterized inputs, and a live preview before any report is saved.
See each skill's `references/api.md` for the full endpoint table and request/response shapes,
and the repo `CLAUDE.md` for shared rules and local environment setup.

## Backup & Restore

SQL queries are included in the platform-wide backup and restore process.

When you run a platform export, all SQL queries (Name, Description, Query text, Connection string name and Parameters) are serialized into the backup archive.

On import, the queries are recreated or updated, preserving their identifiers, metadata and parameter definitions.

To run backup/restore:
1. Open Virto Commerce Admin UI.
1. Navigate to **Settings → Platform → Export** (or **Import**).
1. Ensure **Sql Queries** module is selected in the module list.
1. Run the export/import process.

## Documentation
* [View on GitHub](https://github.com/VirtoCommerce/vc-module-sql-queries)

## References
* [Deployment](https://docs.virtocommerce.org/platform/developer-guide/Tutorials-and-How-tos/Tutorials/deploy-module-from-source-code/)
* [Installation](https://docs.virtocommerce.org/platform/user-guide/modules-installation/)
* [Home](https://virtocommerce.com)
* [Community](https://www.virtocommerce.org)
* [Download latest release](https://github.com/VirtoCommerce/vc-module-sql-queries/releases)

## License
Copyright (c) Virto Solutions LTD.  All rights reserved.

This software is licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at http://virtocommerce.com/opensourcelicense.

Unless required by the applicable law or agreed to in written form, the software
distributed under the License is provided on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
