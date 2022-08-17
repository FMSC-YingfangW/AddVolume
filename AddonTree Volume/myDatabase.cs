using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Database.Sqlite;
using System.IO;


namespace AddonTree_Volume
{
    public class MyDatabase
    {
        /// <summary>
        /// SQLiteDatabase object sqldTemp to handle SQLiteDatabase.
        /// </summary>
        private SQLiteDatabase sqldTemp;
        /// <summary>
        /// The sSQLquery for query handling.
        /// </summary>
        private string sSQLQuery;
        /// <summary>
        /// The sMessage to hold message.
        /// </summary>
        private string sMessage;
        /// <summary>
        /// The bDBIsAvailable for database is available or not.
        /// </summary>
        private bool bDBIsAvailable;

        private bool bHasLocalVolTable;
        /// <summary>
        /// Initializes a new instance of the <see cref="MyDatabaseDemo.MyDatabase"/> class.
        /// </summary>
        public MyDatabase()
        {
            sMessage = "";
            bDBIsAvailable = false;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="MyDatabaseDemo.MyDatabase"/> class.
        /// </summary>
        /// <param name='sDatabaseName'>
        /// Pass your database name.
        /// </param>
        public MyDatabase(string sDatabaseName)
        {
            try
            {
                sMessage = "";
                bDBIsAvailable = false;
                bHasLocalVolTable = false;
                CreateDatabase(sDatabaseName);
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
        }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="MyDatabaseDemo.MyDatabase"/> database available.
        /// </summary>
        /// <value>
        /// <c>true</c> if database available; otherwise, <c>false</c>.
        /// </value>
        public bool DatabaseAvailable
        {
            get { return bDBIsAvailable; }
            set { bDBIsAvailable = value; }
        }

        public bool HasLocalVolTable
        {
            get { return bHasLocalVolTable; }
            set { bHasLocalVolTable = value; }
        }
        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        public string Message
        {
            get { return sMessage; }
            set { sMessage = value; }
        }
        /// <summary>
        /// Creates the database.
        /// </summary>
        /// <param name='sDatabaseName'>
        /// Pass database name.
        /// </param>
        public void CreateDatabase(string sDatabaseName)
        {
            try
            {
                sMessage = "";
                //string sLocation = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                //string sDB = Path.Combine(sLocation, sDatabaseName);
                string sDB = sDatabaseName;
                bool bIsExists = File.Exists(sDB);
                if (!bIsExists)
                {
                    //if the database is cruise file, do not create new database, just put a warning message
                    if(sDB.Contains(".cruise"))
                    {
                        sMessage = "There is no cruise file selected.";
                    }
                    //the addon tree will need to create the database
                    else
                    { 
                        sqldTemp = SQLiteDatabase.OpenOrCreateDatabase(sDB, null);
                        sSQLQuery = "CREATE TABLE IF NOT EXISTS " +
                            "AddTree " +
                            "(_id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                            "Species TEXT, " + 
                            "DBH REAL, " + 
                            "Prod TEXT, " + 
                            "LiveDead TEXT, " + 
                            "CuttingUnit TEXT, " + 
                            "PaymentUnit TEXT, " +
                            "CUFT REAL, " + 
                            "PrimaryVol REAL, " +
                            "SecondaryVol REAL, " +
                            "Prod2 TEXT, " +
                            "TotalHt REAL, " +
                            "AddRemove TEXT, " + 
                            "ReportDate DATE, " +
                            "ModifiedDate TIMESTAMP, " +
                            "ModifiedBy TEXT, " +
                            "CreatedBy TEXT, " +
                            "CreatedDate TIMESTAMP Default CURRENT_TIMESTAMP, TreeCount INTEGER Default 1);";
                        sqldTemp.ExecSQL(sSQLQuery);
                        sSQLQuery = "CREATE TABLE IF NOT EXISTS " +
                            "NewCuttingUnit " +
                            "(CN INTEGER PRIMARY KEY AUTOINCREMENT, " +
                            "Code TEXT UNIQUE, Description TEXT, CreatedBy TEXT, CreatedDate TIMESTAMP Default CURRENT_TIMESTAMP);";
                        sqldTemp.ExecSQL(sSQLQuery);
                        sSQLQuery = "CREATE TABLE IF NOT EXISTS " +
                            "SaleInfo " +
                            "(SaleName TEXT, SaleNum TEXT, CruiseFile TEXT, Region TEXT, Forest TEXT, District TEXT, ContractNumber TEXT, Purchaser TEXT, Inspector TEXT);";
                        sqldTemp.ExecSQL(sSQLQuery);
                        sSQLQuery = "CREATE TRIGGER IF NOT EXISTS updateModifiedDate AFTER UPDATE OF Species, DBH, PROD, LiveDead ON AddTree " +
                            "FOR EACH ROW BEGIN UPDATE AddTree SET ModifiedDate = CURRENT_TIMESTAMP WHERE AddTree._id = old._id; END;";
                        sqldTemp.ExecSQL(sSQLQuery);
                        sSQLQuery = "CREATE TABLE IF NOT EXISTS " +
                            "SalePrice " +
                            "(CN INTEGER PRIMARY KEY AUTOINCREMENT, " +
                            "Species TEXT, Prod TEXT, Price REAL, CreatedBy TEXT, CreatedDate TIMESTAMP Default CURRENT_TIMESTAMP);";
                        sqldTemp.ExecSQL(sSQLQuery);
                        sSQLQuery = "CREATE TABLE IF NOT EXISTS " +
                            "SummaryTemp " +
                            "(CN INTEGER PRIMARY KEY AUTOINCREMENT, " +
                            "PaymentUnit TEXT, ReportDate DATE, AddRemove TEXT, Species TEXT, Prod TEXT, Volume REAL);";
                        sqldTemp.ExecSQL(sSQLQuery);
                        sSQLQuery = "CREATE TABLE IF NOT EXISTS " +
                            "TallyTree " +
                            "(CN INTEGER PRIMARY KEY AUTOINCREMENT, " +
                            "Species TEXT, Product TEXT, DBHclass INTEGER, TreeCount INTEGER Default 0, UNIQUE (Species, Product, DBHclass));";
                        sqldTemp.ExecSQL(sSQLQuery);
                        sMessage = "New database is created.";
                        bDBIsAvailable = true;
                    }
                }
                else
                {
                    sqldTemp = SQLiteDatabase.OpenDatabase(sDB, null, DatabaseOpenFlags.OpenReadwrite);
                    sMessage = "Database is opened.";
                    bDBIsAvailable = true;
                    //check if AddTree has TreeCount column,  also TallyTree table
                    // this is to make the addvol file created by older AddVolume app to work (2021/04/09)
                    if (sDB.Contains(".addvol")) TryAddTreeCountCol();

                    //for cruise file, check if there is a local vol table created.
                    if (sDB.Contains(".cruise"))
                    {
                        bHasLocalVolTable = LocalVolTableAvailable();
                        //cruiseDB = sqldTemp;
                    }
                }
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
        }
        public void CloseDB()
        {
            //sqldTemp.SetTransactionSuccessful();
            //sqldTemp.EndTransaction();
            if(sqldTemp.IsOpen)
            { sqldTemp.Close(); }
        }
        public void TryAddTreeCountCol()
        {
            try
            {
                sSQLQuery = "ALTER TABLE AddTree ADD TreeCount INTEGER Default 1";
                sqldTemp.ExecSQL(sSQLQuery);
                sSQLQuery = "CREATE TABLE IF NOT EXISTS " +
                            "TallyTree " +
                            "(CN INTEGER PRIMARY KEY AUTOINCREMENT, " +
                            "Species TEXT, Product TEXT, DBHclass INTEGER, TreeCount INTEGER Default 0, UNIQUE (Species, Product, DBHclass));";
                sqldTemp.ExecSQL(sSQLQuery);
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }

        }
        /// <summary>
        /// Adds the record.
        /// </summary>
        /// <param name='sSpecies'>
        /// Pass Species.
        /// </param>
        /// <param name='fDBH'>
        /// Pass DBH.
        /// </param>
        /// <param name='sProd'>
        /// Pass Prod.
        /// <param name='sRegSp'>
        /// Pass rSpecies.
        /// </param>
        public void AddRecord(string sSpecies, float fDBH, string sProd, string sLiveDead, string sPaymentUnit, string sCutUnit, float fPrimVol, float fSecdVol, string sProd2, string sUserInit, string sTHT, string sAddRemove, string sTreCnt)
        {
            double dDBH = (double)fDBH;
            string strCU, sSecdVol;
            try
            {
                sUserInit = sUserInit.Replace("'", "''");
                sPaymentUnit = sPaymentUnit.Replace("'", "''");
                if (fPrimVol > 0.0f)
                {
                    fPrimVol = (float)Math.Round(fPrimVol, 3);
                    strCU = fPrimVol.ToString();
                }
                else strCU = null;

                if (fSecdVol > 0.0f)
                {
                    fSecdVol = (float)Math.Round(fSecdVol, 3);
                    sSecdVol = fSecdVol.ToString(); 
                }
                else sSecdVol = null;
                sSQLQuery = "INSERT INTO AddTree (Species, DBH, Prod, LiveDead, PaymentUnit, CuttingUnit, PrimaryVol, SecondaryVol, Prod2, CreatedBy, TotalHt, AddRemove, TreeCount) " +
                        "VALUES('" + sSpecies + "'," + fDBH + ",'" + sProd + "', '" + sLiveDead + "', '" + sPaymentUnit + "', '" + sCutUnit + "', '" + strCU + "','" + sSecdVol + "','" + sProd2 + "','" + sUserInit + "', '" + sTHT + "', '" + sAddRemove + "', '" + sTreCnt + "');";
                sqldTemp.ExecSQL(sSQLQuery);
                sMessage = "Record is saved.";
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
        }
        /// <summary>
        /// Updates the record.
        /// </summary>
        /// <param name='iId'>
        /// Pass record ID.
        /// </param>
        /// <param name='sSpecies'>
        /// Pass Species.
        /// </param>
        /// <param name='fDBH'>
        /// Pass DBH.
        /// </param>
        /// <param name='sProd'>
        /// Pass Prod.
        /// <param name='sRegSp'>
        /// Pass rSpecies.
        /// </param>
        public void UpdateRecord(int iId, string sSpecies, float fDBH, string sProd, string sLiveDead, float fPrimVol, float fSecdVol, string sProd2, string sUser, string sAddRemove, string sTHT, string sTreCnt)
        {
            string strCU, sSecdVol;
            try
            {
                sUser = sUser.Replace("'", "''");
                if (fPrimVol > 0.0f)
                {
                    fPrimVol = (float)Math.Round(fPrimVol, 3);
                    strCU = fPrimVol.ToString();
                }
                else
                    strCU = null;
                if (fSecdVol > 0.0f)
                {
                    fSecdVol = (float)Math.Round(fSecdVol, 3);
                    sSecdVol = fSecdVol.ToString();
                }
                else sSecdVol = null;
                {
                    sSQLQuery = "UPDATE AddTree " +
                    "SET ReportDate = Null, Species='" + sSpecies + "',DBH='" + fDBH + "',Prod='" + sProd + "',LiveDead='" + sLiveDead + "', PrimaryVol='" + strCU + "', SecondaryVol='" + sSecdVol + "', Prod2='" + sProd2 + "', ModifiedBy='" + sUser + "', AddRemove='" + sAddRemove + "', TotalHt='" + sTHT + "', TreeCount = '" + sTreCnt + "' " +
                    " WHERE _id='" + iId + "';";
                }
                sqldTemp.ExecSQL(sSQLQuery);
                sMessage = "Record is updated: " + iId;
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
        }
        /// <summary>
        /// Deletes the record.
        /// </summary>
        /// <param name='iId'>
        /// Pass ID.
        /// </param>
        public void DeleteRecord(int iId)
        {
            try
            {
                sSQLQuery = "DELETE FROM AddTree " +
                    "WHERE _id='" + iId + "';";
                sqldTemp.ExecSQL(sSQLQuery);
                sMessage = "Record is deleted: " + iId;
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
        }
        
        public void InsertSalePrice(string sSpecies, string sProd, string sPrice, string sUserInit)
        {
            try
            {
                sUserInit = sUserInit.Replace("'", "''");
                sSQLQuery = "INSERT INTO SalePrice (Species, Prod, Price, CreatedBy) " +
                    "VALUES ( '" + sSpecies + "', '" + sProd + "', '" + sPrice + "', '" + sUserInit + "')";
                sqldTemp.ExecSQL(sSQLQuery);
                sMessage = "Record is added ";
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
        }
        //insert new record to TreeDefaultValue table
        public void InsertTreeDefaultValue(string sSpecies, string sProd, string sLiveDead, string sUserInit)
        {
            try
            {
                sUserInit = sUserInit.Replace("'", "''");
                sSQLQuery = "INSERT INTO TreeDefaultValue (Species, PrimaryProduct, LiveDead, CreatedBy) " +
                    "VALUES ( '" + sSpecies + "', '" + sProd + "', '" + sLiveDead + "', '" + sUserInit + "')";
                sqldTemp.ExecSQL(sSQLQuery);
                sMessage = "Record is added ";
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
        }
        public void UpdateSalePrice(string sId, string sSpecies, string sProd, string sPrice, string sUserInit)
        {
            try
            {
                sUserInit = sUserInit.Replace("'", "''");
                sSQLQuery = "UPDATE SalePrice " +
                    "SET Species='" + sSpecies + "',Prod='" + sProd + "',Price='" + sPrice + "', CreatedBy='" + sUserInit + "' " +
                    " WHERE CN='" + sId + "';";
                sqldTemp.ExecSQL(sSQLQuery);
                sMessage = "Record is added ";
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
        }
        public void UpdateContractSpecies(string sId, string sConSpecies)
        {
            try
            {
                sSQLQuery = "UPDATE TreeDefaultValue " +
                    "SET ContractSpecies='" + sConSpecies + "' " +
                    " WHERE TreeDefaultValue_CN='" + sId + "';";
                sqldTemp.ExecSQL(sSQLQuery);
                sMessage = "Record is updated ";
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
        }
        public void DeleteSalePrice(int iId)
        {
            try
            {
                sSQLQuery = "DELETE FROM SalePrice " +
                    "WHERE CN='" + iId + "';";
                sqldTemp.ExecSQL(sSQLQuery);
                sMessage = "Record is deleted: " + iId;
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
        }
        /// <summary>
        /// Gets the record cursor.
        /// </summary>
        /// <returns>
        /// The record cursor.
        /// </returns>
        public Android.Database.ICursor GetRecordCursor()
        {
            Android.Database.ICursor icTemp = null;
            try
            {
                sSQLQuery = "SELECT _id, Species, ROUND(DBH,1) DBH, LiveDead, Prod, CuttingUnit, PaymentUnit, AddRemove, PrimaryVol, SecondaryVol, TotalHt, TreeCount FROM AddTree ORDER BY _id DESC;";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
                if (!(icTemp != null))
                {
                    sMessage = "Record not found.";
                }
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return icTemp;
        }
        public Android.Database.ICursor GetSalePriceRecordCursor()
        {
            Android.Database.ICursor icTemp = null;
            try
            {
                sSQLQuery = "SELECT CN _id, Species, Prod, Price FROM SalePrice ORDER BY Species, Prod;";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
                if (!(icTemp != null))
                {
                    sMessage = "Record not found.";
                }
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return icTemp;
        }
        public Android.Database.ICursor GetTreeDefaultValueCursor(string strOr)
        {
            Android.Database.ICursor icTemp = null;
            try
            {
                //sSQLQuery = "SELECT TreeDefaultValue_CN _id, Species, PrimaryProduct, FIAcode, ContractSpecies FROM TreeDefaultvalue WHERE ContractSpecies is null OR LENGTH(ContractSpecies) <= 1 ORDER BY Species, PrimaryProduct;";
                sSQLQuery = "SELECT TreeDefaultValue_CN _id, Species, PrimaryProduct, FIAcode, ContractSpecies FROM TreeDefaultvalue WHERE FIAcode = 0 OR Species IN (SELECT DISTINCT Species FROM Tree) " + strOr + " ORDER BY PrimaryProduct, Species;";
                //sSQLQuery = "SELECT TreeDefaultValue_CN _id, Species, PrimaryProduct, FIAcode, ContractSpecies FROM TreeDefaultvalue ORDER BY PrimaryProduct, Species;";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
                if (!(icTemp != null))
                {
                    sMessage = "Record not found.";
                }
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return icTemp;
        }
        //add user created cutting unit code to NewCuttingUnit table
        public void AddNewCuttingUnit(string sCode, string sDesc, string sUser)
        {
            try
            {
                sDesc = sDesc.Replace("'", "''");
                sUser = sUser.Replace("'", "''");
                sSQLQuery = "INSERT INTO NewCuttingUnit (Code,Description,CreatedBy) " +
                    "VALUES('" + sCode + "','" + sDesc + "', '" + sUser + "');";
                sqldTemp.ExecSQL(sSQLQuery);
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
        }
        //add record to SaleInfo in the addon database
        public void AddRecordToSaleInfo(string sSaleName, string sSaleNum, string sCruiseFile, string sReg, string sFor, string sDist)
        {
            try
            {
                sSaleName = sSaleName.Replace("'", "''");
                sCruiseFile = sCruiseFile.Replace("'", "''");
                sSQLQuery = " SELECT COUNT(*) FROM SaleInfo;";
                Android.Database.ICursor icTemp = sqldTemp.RawQuery(sSQLQuery, null);
                string result = "0";
                if (icTemp.MoveToFirst())
                {
                    result = icTemp.GetString(0);
                }
                if (int.Parse(result) > 0)
                {
                    sSQLQuery = "UPDATE SaleInfo " +
                    "SET SaleName = '" + sSaleName + "', SaleNum = '" + sSaleNum + "', CruiseFile = '" + sCruiseFile + "', Region = '" + sReg + "', Forest = '" + sFor + "', District = '" + sDist + "';";
                    sqldTemp.ExecSQL(sSQLQuery);
                }
                else
                {
                    sSQLQuery = "INSERT INTO SaleInfo (SaleName,SaleNum,CruiseFile, Region, Forest, District) " +
                    "VALUES('" + sSaleName + "','" + sSaleNum + "', '" + sCruiseFile + "', '" + sReg + "', '" + sFor + "', '" + sDist + "');";
                    sqldTemp.ExecSQL(sSQLQuery);
                }
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
        }
        public void UpdateVol(int id, double vol, double vol2)
        {
            try
            {
                sSQLQuery = "UPDATE AddTree " +
                    "SET PrimaryVol = '" + vol + "', SecondaryVol = '" + vol2 + "' WHERE _id = '" + id + "';";
                sqldTemp.ExecSQL(sSQLQuery);
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
        }
        public void UpdateSaleInfo(string sContractNum, string sContractor, string sInspector)
        {
            try
            {
                sContractNum = sContractNum.Replace("'", "''");
                sContractor = sContractor.Replace("'", "''");
                sInspector = sInspector.Replace("'", "''");
                sSQLQuery = "UPDATE SaleInfo " +
                    "SET ContractNumber = '" + sContractNum + "', Purchaser = '" + sContractor + "', Inspector = '" + sInspector + "';";
                sqldTemp.ExecSQL(sSQLQuery);
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
        }
        //get salename from addvol
        public string GetSaleNameFromAddvol()
        {
            Android.Database.ICursor icTemp = null;
            string saleName = null;
            try
            {
                sSQLQuery = "SELECT SaleName FROM SaleInfo ;";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
                if (!(icTemp != null))
                {
                    sMessage = "Record not found.";
                }
                else
                {
                    if (icTemp.MoveToFirst())
                    {
                        do
                        {
                            saleName = icTemp.GetString(icTemp.GetColumnIndex("SaleName"));
                        } while (icTemp.MoveToNext());
                    }

                }
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return saleName;

        }
        //query database to get a value from a table colume with a where clause
        public string getValFromDB(string dbColumn, string dbTable, string sWhere)
        {
            Android.Database.ICursor icTemp = null;
            string returnStr = null;
            try
            {
                sSQLQuery = "SELECT " + dbColumn + " FROM " + dbTable + " " + sWhere + ";";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
                if (!(icTemp != null))
                {
                    sMessage = "Record not found.";
                }
                else
                {
                    if (icTemp.MoveToFirst())
                    {
                        returnStr = icTemp.GetString(icTemp.GetColumnIndex(dbColumn));
                    }
                }
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return returnStr;
        }
        //query Cruise database for a column value
        public string strQueryCruiseDB(string dbColumn, string dbTable)
        {
            Android.Database.ICursor icTemp = null;
            string returnStr = null;
            try
            {
                sSQLQuery = "SELECT " + dbColumn + " FROM " + dbTable + " ;";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
                if (!(icTemp != null))
                {
                    sMessage = "Record not found.";
                }
                else
                {
                    if (icTemp.MoveToFirst())
                    {
                        returnStr = icTemp.GetString(icTemp.GetColumnIndex(dbColumn));
                    }
                }
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return returnStr;
        }
        //query addvol DB for trees without CUFT
        public Android.Database.ICursor TreesNeedCalcCuft()
        {
            Android.Database.ICursor icTemp = null;
            try
            {
                sSQLQuery = "SELECT _id, Species, DBH, LiveDead, Prod, TotalHt FROM AddTree WHERE IFNULL(PrimaryVol,0) = 0 OR LENGTH(PrimaryVol) = 0;";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
                if (!(icTemp != null))
                {
                    sMessage = "Record not found.";
                }
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return icTemp;
        }
        //query Cruise database to return a list of value
        public Android.Database.ICursor lvQueryCruiseDB(string dbColumn, string dbTable)
        {
            Android.Database.ICursor icTemp = null;
            try
            {
                sSQLQuery = "SELECT DISTINCT " + dbColumn + " FROM " + dbTable + " WHERE " + dbColumn + " IS NOT NULL ORDER BY " + dbColumn + ";";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
                if (!(icTemp != null))
                {
                    sMessage = "Record not found.";
                }
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return icTemp;
        }
        //query Cruise database to return a list of value with where clause and order by
        public Android.Database.ICursor lvQueryCruiseDB(string dbColumn, string dbTable, string sWhere, string sOrder)
        {
            if (sWhere == null) sWhere = " 1=1 AND ";
            Android.Database.ICursor icTemp = null;
            try
            {
                sSQLQuery = "SELECT DISTINCT " + dbColumn + " FROM " + dbTable + " WHERE " + sWhere + dbColumn + " IS NOT NULL ORDER BY " + dbColumn + " " + sOrder + ";";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
                if (!(icTemp != null))
                {
                    sMessage = "Record not found.";
                }
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return icTemp;
        }
        //populate SummaryTemp table for report
        public void PopulateSummaryTemp(bool bReportAll, string sReportDate)
        {
            string sToday = DateTime.Today.ToString("yyyy-MM-dd");
            if (!string.IsNullOrEmpty(sReportDate)) sToday = sReportDate;
            string ReportDateClause;
            if (bReportAll)
                ReportDateClause = "1=1";
            else
                ReportDateClause = "ReportDate IS NULL";
            //clear the table
            sSQLQuery = "DELETE FROM SummaryTemp;";
            sqldTemp.ExecSQL(sSQLQuery);
            //add volume for primary prod
            sSQLQuery = "INSERT INTO SummaryTemp (ReportDate, PaymentUnit, AddRemove, Species, Prod, Volume) " +
                "SELECT IFNULL(ReportDate, '" + sToday + "'), PaymentUnit, AddRemove, Species, Prod, SUM(PrimaryVol*TreeCount) " +
                            " FROM AddTree " +
                            " WHERE " + ReportDateClause + 
                            " GROUP BY IFNULL(ReportDate, '" + sToday + "'), PaymentUnit, AddRemove, Species, Prod";
            sqldTemp.ExecSQL(sSQLQuery);
            //add record for secondary prod
            sSQLQuery = "INSERT INTO SummaryTemp (ReportDate, PaymentUnit, AddRemove, Species, Prod, Volume) " +
                "SELECT IFNULL(ReportDate, '" + sToday + "'), PaymentUnit, AddRemove, Species, Prod2, SUM(SecondaryVol*TreeCount) " +
                            " FROM AddTree " +
                            " WHERE " + ReportDateClause + " AND (SecondaryVol IS NOT NULL AND ROUND(SecondaryVol,3) > 0) " +
                            " GROUP BY IFNULL(ReportDate, '" + sToday + "'), PaymentUnit, AddRemove, Species, Prod2";
            sqldTemp.ExecSQL(sSQLQuery);
        }
        //query SummaryTemp to get summary
        public Android.Database.ICursor lvQuerySummaryVol(string sPayUnit, string sReportDate, string sAddRemove)
        {
            Android.Database.ICursor icTemp = null;
            try
            {
                sSQLQuery = "SELECT Species, Prod, SUM(Volume) " +
                            " FROM SummaryTemp " +
                            " WHERE PaymentUnit = '" + sPayUnit + "' AND ReportDate = '" + sReportDate + "' AND AddRemove = '" + sAddRemove + "'" +
                            " GROUP BY Species, Prod " +
                            " ORDER BY Species, Prod;";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
                if (!(icTemp != null))
                {
                    sMessage = "Record not found.";
                }
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return icTemp;
        }
        //query summary volume from addtree table
        public Android.Database.ICursor lvQueryAddTreeVol(string sAddDel, bool bReportAll)
        {
            string sToday = DateTime.Today.ToString("yyyy-MM-dd");
            string ReportDateClause;
            if (bReportAll) ReportDateClause = "1=1";
            else ReportDateClause = "ReportDate IS NULL";

            Android.Database.ICursor icTemp = null;
            try
            {
                sSQLQuery = "SELECT IFNULL(ReportDate, '" + sToday + "'), PaymentUnit, Species, Prod, SUM(PrimaryVol) " + 
                            " FROM AddTree " +
                            " WHERE " + ReportDateClause + " AND AddRemove = '" +sAddDel + "'" +
                            " GROUP BY IFNULL(ReportDate, '" + sToday + "'), PaymentUnit, Species, Prod " +
                            " ORDER BY IFNULL(ReportDate, '" + sToday + "'), PaymentUnit, Species, Prod;";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
                if (!(icTemp != null))
                {
                    sMessage = "Record not found.";
                }
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return icTemp;
        }
        //query list of trees
        public Android.Database.ICursor lvQueryAddTree(bool bReportAll, string sReportMonth = null)
        {
            string ReportDateClause;
            if (bReportAll)
            {
                if (sReportMonth == null) ReportDateClause = "1=1";
                else ReportDateClause = " strftime('%Y-%m',ReportDate) = '" + sReportMonth + "' ";
            }
            else
                ReportDateClause = "ReportDate IS NULL";

            Android.Database.ICursor icTemp = null;
            try
            {
                sSQLQuery = "SELECT PaymentUnit, CuttingUnit, Species, DBH, Prod, LiveDead, AddRemove, PrimaryVol, SecondaryVol, TotalHt, TreeCount  FROM AddTree WHERE " + ReportDateClause + " ORDER BY PaymentUnit, CuttingUnit, Species, Prod;";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
                if (!(icTemp != null))
                {
                    sMessage = "Record not found.";
                }
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return icTemp;
        }
        public bool hasTreeToReport(bool ReportAll = false)
        {
            Boolean hasTree = false;
            string result = "0";
            string myWhere = " 1=1 ";
            if (ReportAll == false) myWhere = " ReportDate IS NULL ";
            Android.Database.ICursor icTemp = null;
            sSQLQuery = "SELECT Count(*)  FROM AddTree WHERE " + myWhere + ";";
            icTemp = sqldTemp.RawQuery(sSQLQuery, null);
            if (icTemp.MoveToFirst())
            {
                result = icTemp.GetString(0);
            }
            if (int.Parse(result) > 0)
                hasTree = true;
            return hasTree; 
        }
        public int SumTreeCount(bool ReportAll, string sReportDate = null)
        {
            int result = 0;
            string myWhere = " 1=1 ";
            if (ReportAll == false) myWhere = " ReportDate IS NULL ";
            else
            {
                if (sReportDate != null) myWhere = " strftime('%Y-%m',ReportDate) = '" + sReportDate + "' ";
            }
            Android.Database.ICursor icTemp = null;
            sSQLQuery = "SELECT Sum(TreeCount)  FROM AddTree WHERE " + myWhere + ";";
            icTemp = sqldTemp.RawQuery(sSQLQuery, null);
            if (icTemp.MoveToFirst())
            {
                result = int.Parse(icTemp.GetString(0));
            }
            return result;
        }
        //check record exists in TreeDefaultValue table for the speies and prod
        public bool existTreeDefaultValue(string spec, string prod)
        {
            bool hasRec = false;
            string result = "0";
            Android.Database.ICursor icTemp = null;
            sSQLQuery = "SELECT Count(*)  FROM TreeDefaultValue WHERE Species = '" + spec + "' AND PrimaryProduct = '" + prod + "';";
            icTemp = sqldTemp.RawQuery(sSQLQuery, null);
            if (icTemp.MoveToFirst())
            {
                result = icTemp.GetString(0);
            }
            if (int.Parse(result) > 0)
                hasRec = true;
            return hasRec;
        }
        //set ReportDate on AddTree after create report
        public void SetReportDateOnAddTree(string sReportDate)
        {
            string sToday = DateTime.Today.ToString("yyyy-MM-dd");
            if(!string.IsNullOrEmpty(sReportDate))
            {
                sToday = sReportDate;
            }
            try
            {
                sSQLQuery = "UPDATE AddTree SET ReportDate='" + sToday + "' WHERE ReportDate IS NULL;";
                sqldTemp.ExecSQL(sSQLQuery);
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
        }

        //get cutting unit code list from cruise file
        public Android.Database.ICursor GetCuttingUnitCode(string dbTable)
        {
            Android.Database.ICursor icTemp = null;
            try
            {
                sSQLQuery = "SELECT DISTINCT Trim(Code||' '||IFNULL(Description, ' ')) Code FROM " + dbTable + ";";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
                if (!(icTemp != null))
                {
                    sMessage = "Record not found.";
                }
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return icTemp;
        }

        //check if the cruise file has regression done
        public bool LocalVolTableAvailable()
        {
            bool LocalVolAvailable = false;
            Android.Database.ICursor icTemp = null;
            try
            {
                sSQLQuery = "SELECT COUNT(*) FROM Regression WHERE rVolType = 'Primary' AND rVolume LIKE '%CUFT%';";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
                if (icTemp.MoveToFirst())
                {
                    int iCnt = icTemp.GetInt(0);
                    if (iCnt > 0) LocalVolAvailable = true;
                }
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }

            return LocalVolAvailable;
        }
        public bool LocalVolTableAvailable(string sSpec, string sProd, string sDBH)
        {
            bool LocalVolAvailable = false;
            Android.Database.ICursor icTemp = null;
            try
            {
                sSQLQuery = "SELECT COUNT(*) FROM Regression WHERE rVolType = 'Primary' AND rVolume LIKE '%CUFT%' " + 
                    "AND rSpeices LIKE '%" + sSpec + "%' AND rProduct LIKE '%" + sProd + "%' AND rMinDBH <= " + sDBH + " AND rMaxDBH >= " + sDBH +";";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
                if (icTemp.MoveToFirst())
                {
                    int iCnt = icTemp.GetInt(0);
                    if (iCnt > 0) LocalVolAvailable = true;
                }
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }

            return LocalVolAvailable;
        }
        public bool HasMoreThanOneProd2(string spec, string prod)
        {
            bool moreThanOne = false;
            Android.Database.ICursor icTemp = null;
            try
            {
                sSQLQuery = "SELECT COUNT(DISTINCT SecondaryProduct) " +
                    " FROM SampleGroup SG, SampleGroupTreeDefaultValue SGTDV, TreeDefaultValue TDV " +
                    " WHERE SG.SampleGroup_CN = SGTDV.SampleGroup_CN " +
                    " AND SGTDV.TreeDefaultValue_CN = TDV.TreeDefaultValue_CN " +
                    " AND TDV.Species = '" + spec + "' AND TDV.PrimaryProduct = '" + prod + "';";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
                if (icTemp.MoveToFirst())
                {
                    int iCnt = icTemp.GetInt(0);
                    if (iCnt > 1) moreThanOne = true;
                }
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }

            return moreThanOne;
        }
        public Android.Database.ICursor GetProd2List(string spec, string prod)
        {
            Android.Database.ICursor icTemp = null;
            try
            {
                sSQLQuery = "SELECT DISTINCT SecondaryProduct " +
                    " FROM SampleGroup SG, SampleGroupTreeDefaultValue SGTDV, TreeDefaultValue TDV " +
                    " WHERE SG.SampleGroup_CN = SGTDV.SampleGroup_CN " +
                    " AND SGTDV.TreeDefaultValue_CN = TDV.TreeDefaultValue_CN " +
                    " AND TDV.Species = '" + spec + "' AND TDV.PrimaryProduct = '" + prod + "' ORDER BY SecondaryProduct;";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
                if (!(icTemp != null))
                {
                    sMessage = "Record not found.";
                }
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return icTemp;
        }
        //check if the species and prod already exists in the SalePrice table
        public bool SalePriceExist(string sSpec, string sProd)
        {
            bool recordExist = false;
            Android.Database.ICursor icTemp = null;
            try
            {
                sSQLQuery = "SELECT COUNT(*) FROM SalePrice WHERE Species = '" + sSpec + "' AND Prod = '" + sProd + "';";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
                if (icTemp.MoveToFirst())
                {
                    int iCnt = icTemp.GetInt(0);
                    if (iCnt > 0) recordExist = true;
                }
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return recordExist;
        }
        public Android.Database.ICursor GetSalePriceCur()
        {
            Android.Database.ICursor icTemp = null;
            try
            {
                //the query need a column _id in order to display the data in listview with SimpleCursorAdaptor
                sSQLQuery = "SELECT CN _id, Species, Prod, Price FROM SalePrice " +
                    "ORDER BY Species, Prod;";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return icTemp;
        }
        public Android.Database.ICursor GetRegressionInfo()
        {
            Android.Database.ICursor icTemp = null;
            try
            {
                //the query need a column _id in order to display the data in listview with SimpleCursorAdaptor
                sSQLQuery = "SELECT Regression_CN _id, rVolType, rSpeices, rLiveDead, rProduct, rMinDbh, rMaxDbh, RegressModel FROM Regression " +
                    " WHERE rVolume LIKE '%CUFT%'" +
                    " ORDER BY rSpeices, rProduct, rVolType;";
                    //"WHERE rVolType = 'Primary' AND rVolume LIKE '%CUFT%';";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return icTemp;
        }
        public Android.Database.ICursor GetRegSpecProdDBH()
        {
            Android.Database.ICursor icTemp = null;
            try
            {
                sSQLQuery = "SELECT rSpeices, rProduct, ROUND(rMinDbh+0.5) rMinDBH, ROUND(rMaxDbh-0.5) rMaxDBH FROM Regression " +
                "WHERE rVolume LIKE '%CUFT%' AND rVolType = 'Primary';";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return icTemp;
        }
        //Tally Tree table
        public void AddDBHclassToTallyTree(string spec, string prod, string DBH)
        {
            try
            {
                sSQLQuery = "INSERT INTO TallyTree (Species, Product, DBHclass) " +
                    " VALUES('" + spec + "','" + prod + "', '" + DBH + "');";
                sqldTemp.ExecSQL(sSQLQuery);
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
        }
        //update TallyTree treecount
        public void UpdateTallyTreeCount(string spec, string prod, string DBH, string cnt)
        {
            try
            {
                sSQLQuery = "UPDATE TallyTree SET TreeCount = " + cnt +
                    " WHERE Species = '" + spec + "' AND Product = '" + prod + "' AND DBHclass = " + DBH;
                sqldTemp.ExecSQL(sSQLQuery);
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
        }
        public void UpdateTallyTreeCount2(string id, string cnt)
        {
            try
            {
                sSQLQuery = "UPDATE TallyTree SET TreeCount = " + cnt + " WHERE CN = " + id;
                sqldTemp.ExecSQL(sSQLQuery);
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
        }
        public void SetTallyTreeCount0()
        {
            try
            {
                sSQLQuery = "UPDATE TallyTree SET TreeCount = 0";
                sqldTemp.ExecSQL(sSQLQuery);
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
        }
        //query TallyTree to get trees tallyed
        public Android.Database.ICursor GetTallyTrees()
        {
            Android.Database.ICursor icTemp = null;
            try
            {
                sSQLQuery = "SELECT CN _id, Species, Product, DBHclass, TreeCount FROM TallyTree " +
                "WHERE TreeCount > 0 ORDER BY Species, Product, DBHclass;";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return icTemp;
        }
        //query tallytree to display DBH class and treecount
        public Android.Database.ICursor GetTallyDBHclass(string spec, string prod)
        {
            Android.Database.ICursor icTemp = null;
            try
            {
                sSQLQuery = "SELECT CN _id, DBHclass, TreeCount FROM TallyTree " +
                "WHERE Species = '" + spec + "' AND Product = '" + prod + "' ORDER BY DBHclass;";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return icTemp;
        }
        //get tallyTree species list
        public Android.Database.ICursor GetTallySpecList()
        {
            Android.Database.ICursor icTemp = null;
            try
            {
                sSQLQuery = "SELECT DISTINCT Species FROM TallyTree ";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return icTemp;
        }
        //get tally species product list
        public Android.Database.ICursor GetTallySpecProdList(string spec)
        {
            Android.Database.ICursor icTemp = null;
            try
            {
                sSQLQuery = "SELECT DISTINCT Product FROM TallyTree WHERE Species = '" + spec + "'";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return icTemp;
        }
        //check if DBH class already exist in TallyTree table
        public bool DBHclassExist()
        {
            bool recordExist = false;
            Android.Database.ICursor icTemp = null;
            try
            {
                sSQLQuery = "SELECT COUNT(*) FROM TallyTree ";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
                if (icTemp.MoveToFirst())
                {
                    int iCnt = icTemp.GetInt(0);
                    if (iCnt > 0) recordExist = true;
                }
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return recordExist;
        }
        public bool TallyTreeExist()
        {
            bool recordExist = false;
            Android.Database.ICursor icTemp = null;
            try
            {
                sSQLQuery = "SELECT COUNT(*) FROM TallyTree WHERE TreeCount > 0";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
                if (icTemp.MoveToFirst())
                {
                    int iCnt = icTemp.GetInt(0);
                    if (iCnt > 0) recordExist = true;
                }
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return recordExist;
        }
        //End TallyTree table
        public double CalcVol(string voltype, string species, double DBH, string prod, string livedead)
        {
            double cuVol = 0;
            string spc = species;
            int iSpc;
            if (int.TryParse(species, out iSpc))
            { spc = species.PadLeft(3, '0'); }

            sMessage = "";
            {
                Android.Database.ICursor icTemp = null;
                try
                {
                    sSQLQuery = "SELECT RegressModel, CoefficientA, CoefficientB, CoefficientC, rMinDbh, rMaxDbh FROM Regression " +
                                "WHERE rVolType = '" + voltype + "' AND rVolume LIKE '%CUFT%' AND rSpeices LIKE '%" + spc + "%' AND rProduct LIKE '%" + prod + "%' AND rMinDbh <= " + DBH + " AND rMaxDbh >= " + DBH + ";";
                    icTemp = sqldTemp.RawQuery(sSQLQuery, null);
                    if (icTemp.MoveToFirst() && icTemp.GetString(0) != null)
                    {
                        string rModel = icTemp.GetString(icTemp.GetColumnIndex("RegressModel"));
                        double minDBH = double.Parse(icTemp.GetString(4));
                        double maxDBH = double.Parse(icTemp.GetString(5));
                        if (DBH < minDBH || DBH > maxDBH)
                        {
                            sMessage = "DBH is out of the Local Volume Table regression DBH range: " + Math.Round(minDBH,1).ToString() + " - " + Math.Round(maxDBH,1).ToString();
                        }
                        else
                        {
                            double a = icTemp.GetFloat(1);
                            double b = icTemp.GetFloat(2);
                            if(rModel.ToUpper() == "QUADRATIC")
                            {
                                double c = icTemp.GetFloat(3);
                                cuVol = a + b * DBH + c * DBH * DBH;
                            }
                            else if(rModel.ToUpper() == "LINEAR")
                            {
                                cuVol = a + b * DBH;
                            }
                            else if (rModel.ToUpper() == "LOG")
                            {
                                cuVol = a + b * Math.Log(DBH);
                            }
                            else if (rModel.ToUpper() == "POWER")
                            {
                                cuVol = a * Math.Pow(DBH, b);
                            }
                        }
                    }
                    else
                    {
                        sMessage = "SPECIES " + species + " does not have a regression equation.";
                    }
                }
                catch (SQLiteException ex)
                {
                    sMessage = ex.Message;
                }

            }
            //return cuVol in CCF
            cuVol = cuVol / 100;
            return Math.Round(cuVol,3);
        }
        //get cruise file name from addon file
        public string GetCruiseFileFromAddon(string colName)
        {
            string sCruiseFile = "";
            Android.Database.ICursor icTemp = null;
            try
            {
                sSQLQuery = "SELECT " + colName + " FROM SaleInfo;";
                icTemp = sqldTemp.RawQuery(sSQLQuery, null);
                if (icTemp.MoveToFirst())
                {
                    sCruiseFile = icTemp.GetString(0);
                }
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
            return sCruiseFile;
        }
        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="MyDatabaseDemo.MyDatabase"/> is reclaimed by garbage collection.
        /// </summary>
        ~MyDatabase()
        {
            try
            {
                sMessage = "";
                bDBIsAvailable = false;
                sqldTemp.Close();
            }
            catch (SQLiteException ex)
            {
                sMessage = ex.Message;
            }
        }
    }
}