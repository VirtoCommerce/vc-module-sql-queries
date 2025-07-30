# Sql Queries Module
This module is designed to empower administrators and developers by providing a secure and flexible way to perform direct database queries within the VirtoCommerce environment. It integrates seamlessly with the platform and supports a variety of query types, enhancing the ability to retrieve, analyze, and manage business data without external tools.

##  Key features
* Execute custom SQL queries against the VirtoCommerce databases.
* Integrate with the platform’s security and permissions system to control query access.
* Return results in user-friendly formats for reporting and analysis: HTML, PDF, CSV, XLSX.
* Supports query parameters: Short Text, Dare Time, Boolean, Integer, Deceimal.
* Supports multiple database providers: SQL Server (default), MySQL, and PostgreSQL.
* Supports multiple connection strings. 

## Screenshots
### List of reports
<img width="1060" height="652" alt="image" src="https://github.com/user-attachments/assets/181d8018-8951-40ad-a22a-a21d044ef0a4" />

### Create a new query 
<img width="952" height="1037" alt="image" src="https://github.com/user-attachments/assets/7d4dd749-c974-45dc-98ad-183597b78cda" />

### Run report
<img width="1372" height="987" alt="image" src="https://github.com/user-attachments/assets/a25ba8b1-66fa-4010-a5b5-18486962ce9f" />

### Review result file
<img width="1363" height="276" alt="image" src="https://github.com/user-attachments/assets/c5d82a29-1093-46b2-a8fe-3962a9df837b" />


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
