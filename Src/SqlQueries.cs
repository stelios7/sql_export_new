using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SQL_Export.Src
{
    public static class SqlQueries
    {
        private static readonly string srv = $@"{Environment.MachineName}";
        private const string DefaultPassword = "124578";
        private const string DefaultLogin = "sa";

        public static string GetSQL_DuplicateBarcodes()
        {
            return $@"-- Step 1: Assign a primary barcode per 'des' with its corresponding price
WITH RankedBarcodes AS (
    SELECT 
        des,
        barcode,
        price,
        FIRST_VALUE(barcode) OVER (PARTITION BY des	ORDER BY barcode desc) AS PrimaryBarcode,
        MIN(price) OVER (PARTITION BY des) AS PrimaryPrice
    FROM vw_ProductDetails
    WHERE id_external > 0
    AND LEFT(barcode, 2) <> '21'
),
-- Step 2: Find the smallest PrimaryBarcode for each duplicate AdditionalBarcode
BarcodeAssignment AS (
    SELECT 
        barcode AS AdditionalBarcode,
        MIN(PrimaryBarcode) AS AssignedPrimaryBarcode  -- Assign it to the smallest primary barcode
    FROM RankedBarcodes
    WHERE barcode <> PrimaryBarcode  -- Only consider additional barcodes
    GROUP BY barcode
)
-- Step 3: Get the final result with unique barcode assignments
SELECT DISTINCT
    BA.AssignedPrimaryBarcode AS PrimaryBarcode,    
    RB.barcode AS AdditionalBarcode
FROM RankedBarcodes RB
JOIN BarcodeAssignment BA ON RB.barcode = BA.AdditionalBarcode
WHERE RB.barcode <> RB.PrimaryBarcode  -- Exclude primary barcode itself
AND RB.price = RB.PrimaryPrice        -- Exclude additional barcodes with the same price
AND RB.PrimaryBarcode = BA.AssignedPrimaryBarcode  -- Ensure barcode is assigned to only one primary barcode;";
        }
        public static string GetSQL_DropView()
        {
            return $@"-- Step 1: Drop the view if it exists
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

IF OBJECT_ID('dbo.vw_ProductDetails', 'V') IS NOT NULL
	DROP VIEW dbo.vw_ProductDetails;";
        }
        public static string GetSQL_CreateView()
        {
            return $@"-- Step 2: Create the view
CREATE VIEW [dbo].[vw_ProductDetails] AS 
SELECT 
    pr.des, 
    pr.category_des, 
    pr.category_des2, 
    pr.id_external, 
    pr.countryImpName, 
    pr.countryFeedName,
    pw.price1 as price, 
    pw.fpa,
    mt.showdes,
    pb.Barcode,
    st.team_name, 
    st.team_zig_id,
	pw.qty
FROM dbo.Products pr
JOIN dbo.Products_WH pw ON pr.guid = pw.PrGuid
LEFT JOIN dbo.MessureType mt ON pr.messureType = mt.id
LEFT JOIN dbo.Products_Barcodes pb ON pr.guid = pb.prguid
LEFT JOIN dbo.ScaleTeams st ON pw.scaleTeamId = st.team_zig_id;";
        }
        public static string GetSQL_CreateIndexes()
        {
            return $@"BEGIN TRY
	CREATE INDEX idx_products_guid ON dbo.Products (guid);
	CREATE INDEX idx_products_wh_prguid ON dbo.Products_WH (PrGuid);
	CREATE INDEX idx_products_messureType ON dbo.Products (messureType);
	CREATE INDEX idx_products_barcodes_prguid ON dbo.Products_Barcodes (prguid);
	CREATE INDEX idx_products_wh_scaleTeamId ON dbo.Products_WH (scaleTeamId);
	CREATE INDEX idx_scaleteams_team_zig_id ON dbo.ScaleTeams (team_zig_id);
END TRY
BEGIN CATCH
END CATCH";
        }
        public static string GetSQLQueryString(int opt = 0)
        {
            var sb = new StringBuilder();
            string query_main_butcher = $@"SELECT 
des AS 'ΠΕΡΙΓΡΑΦΗ',
category_des AS 'ΚΑΤΗΓΟΡΙΑ',
ISNULL(category_des2, '') AS 'ΥΠΟΚΑΤΗΓΟΡΙΑ',
CASE
	WHEN showdes = 'TEM' THEN 'ΤΕΜ'
	WHEN showdes = 'Kg' THEN 'ΚΙΛ'
	ELSE showdes
END  AS 'ΜΟΝ ΜΕΤΡ',
price AS 'ΤΙΜΗ', 
fpa AS 'ΦΠΑ',
ISNULL(barcode,'') as 'ΚΩΔΙΚΟΣ',
ISNULL(LEFT(barcode,7),
CONCAT('21',RIGHT(CONCAT('00000', id_external),5))) AS 'ΚΩΔΙΚΟΣ ΖΥΓ',
ISNULL(team_name, '') AS 'ΖΥΓΑΡΙΑ ΟΝΟΜΑ',
ISNULL(team_zig_id, '') AS 'ΖΥΓΑΡΙΑ id',
RIGHT(CONCAT('00000', id_external), 5) AS 'PLU',
countryImpName as 'ΧΩΡΑ ΓΕΝΝΗΣΗΣ',
countryFeedName AS 'ΧΩΡΑ ΕΚΤΡΟΦΗΣ',
qty AS 'ΠΟΣΟΤΗΤΑ'
FROM dbo.vw_ProductDetails
order by des";
            sb.AppendLine(query_main_butcher);
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt"), sb.ToString());
            return sb.ToString();
        }
        public static string GetSQLCustomers()
        {
            return $@"SELECT 
	afm AS 'ΑΦΜ', 
	CASE phone_1
		WHEN '1' then ''
		ELSE phone_1
	END AS 'ΤΗΛΕΦΩΝΟ', 
	ISNULL(email, '') as 'EMAIL',
	creditMoney AS 'ΥΠΟΛΟΙΠΟ',
	bonus_points AS 'ΠΟΝΤΟΙ'
	FROM [dbo].[Customers]";
        }
        public static string GetSQLPromitheftes()
        {
            return $@"SELECT afm AS 'ΑΦΜ' FROM [dbo].[Promitheuths]";
        }
        public static string GetSQL_Timokatalogoi()
        {
            return $@"SELECT       
	dbo.Customers.afm as 'ΑΦΜ', 
	CONCAT('21',RIGHT(CONCAT('00000', id_external),5)) AS 'ΚΩΔΙΚΟΣ ΖΥΓ',
	dbo.Price_PriceList.priceXondriki AS 'ΤΙΜΗ ΤΙΜΟΚΑΤΑΛΟΓΟΥ'
FROM            dbo.Customers INNER JOIN
                         dbo.PriceList ON dbo.Customers.priceList = dbo.PriceList.listGuid INNER JOIN
                         dbo.Price_PriceList ON dbo.PriceList.listGuid = dbo.Price_PriceList.listguid INNER JOIN
                         dbo.Products ON dbo.Price_PriceList.prguid = dbo.Products.guid INNER JOIN
                         dbo.Products_WH ON dbo.Products.guid = dbo.Products_WH.PrGuid
						 order by afm";
        }
        public static string GetSQLSoftwareInfo()
        {
            return @$"
            SELECT [title] as 'ΕΠΩΝΥΜΙΑ'
      ,[profession] AS 'ΕΠΑΓΓΕΛΜΑ' 
      ,[street] AS 'ΔΙΕΥΘΥΝΣΗ'
      ,[region] AS 'ΠΕΡΙΟΧΗ'
      ,[city] AS 'ΠΟΛΗ'
      ,[zip] AS 'ΤΚ'
      ,[afm] AS	'ΑΦΜ'
      ,[doy] AS 'ΔΟΥ'
      ,[tel1] AS 'ΤΗΛΕΦΩΝΟ'
      ,[mobile] AS 'ΚΙΝΗΤΟ'
      ,[email] AS 'EMAIL'
      ,[shopid] AS 'ΚΩΔΙΚΟΣ ΚΑΤΑΣΤΗΜΑΤΟΣ'
      ,[sn] AS 'ΣΕΙΡΙΑΚΟ'
            FROM [dbo].[Info_Software]";
        }

        /// <summary>
        /// Generates a connection string for SQL Server when restoring a database. Tuple SQL contains Item1: server INSTANCE and Item2: DATABASE name.
        /// </summary>
        /// <param name="SQL"></param>
        /// <param name="login"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string GetConnectionString(Tuple<string, string> SQL, string login = DefaultLogin, string password = DefaultPassword)
        {
            return @$"Server={srv}\{SQL.Item1};Database={SQL.Item2};User ID={login};Password={password};TrustServerCertificate=True;";
        }

        /// <summary>
        /// Generates a default connection string for SQL Server. If ommited, default login = "sa" and password = "124578".
        /// </summary>
        /// <param name="sqlInstance"></param>
        /// <param name="login"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string GetConnectionString(string sqlInstance, string login = DefaultLogin, string password = DefaultPassword)
        {
            return @$"Server={srv}\{sqlInstance.Replace(@".\", "")};User ID={login};Password={password};TrustServerCertificate=True;Connect Timeout=5;";
        }
    }
}
