using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

using System.Threading;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Views.InputMethods;

using Android;
using Android.Graphics;
using Android.Util;
using Java.IO;
using Environment = Android.OS.Environment;
using System.Text.RegularExpressions;

//using Android.Graphics.Pdf;
//using static Android.Graphics.Pdf.PdfDocument;
//use Syncfusion for PDF
//using Syncfusion.Pdf;
//using Syncfusion.Pdf.Graphics;
//using Syncfusion.Drawing;
//using iTextSharp
using iTextSharp;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace AddonTree_Volume
{
    [Activity(Label = "Add Volume")]
    public class AddTreeActivity : Activity
    {
        [DllImport("vollib", EntryPoint = "getvoleq3_")]
        static extern void VolEqDef(ref int a, StringBuilder forstc, StringBuilder distc, ref int spec, StringBuilder prod, StringBuilder voleq, ref int err, int l1, int l2, int l3, int l4);
        [DllImport("vollib", EntryPoint = "vollibfia3_")]
        static extern void GetVolFromVollib(ref int regn, ref int forst, string voleq, ref float dbh, ref float httot, ref float mtopp, ref float mtops, string prod, ref float totcu, ref float sawcu, ref float mcu4, ref float scrbbf, ref float intlbf, ref int errflg);

        MyDatabase myCruiseDB, myAddonDB;
        List<string> unitList = new List<string>();
        TextView tvCruiseFileName, txvSaleName, tvSaleNum;
        EditText txtSaleName, txtCutUnit2, txtCutUnit2Desc;
        EditText txtUserInit, txtPaymentUnit, txtContractNum, txtContractor;
        Spinner txtCutUnit; 
        Button btnEnterTree, btnReport, btnSalePrice, btnExitMain, btnViewReport, btnShowCutUnitList;
        string CruiseFilePicked, addonFile, addvolFolder, cruiseFile, sSaleNumber;
        LinearLayout llCutUnit2;
        string sRegion, sForest, sDist;
        //Boolean bCreateReport = true;
        Boolean bReportAll = false;
        string sToday = DateTime.Today.ToString("yyyy-MM-dd");
        float fPrimVol, fSecdVol;
        string file2display;
        string sInspectionDate = DateTime.Today.ToString("yyyy-MM-dd");

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.AddTree);
            //get the cruise file name from the Open file screen
            cruiseFile = Intent.GetStringExtra("myCruiseFile") ?? string.Empty;
            string newCruiseFile;
            addonFile = Intent.GetStringExtra("myAddonFile") ?? string.Empty;
            //sSaleNumber = null;
            sSaleNumber = "01";
            tvCruiseFileName = FindViewById<TextView>(Resource.Id.tvFileName);
            txtSaleName = FindViewById<EditText>(Resource.Id.txtSaleName);
            txvSaleName = FindViewById<TextView>(Resource.Id.txvSaleName);
            txtCutUnit = FindViewById<Spinner>(Resource.Id.txtCutUnit);
            txtCutUnit2 = FindViewById<EditText>(Resource.Id.txtCutUnit2);
            txtCutUnit2Desc = FindViewById<EditText>(Resource.Id.txtCutUnit2Desc);
            llCutUnit2 = FindViewById<LinearLayout>(Resource.Id.llCutUnit2);

            btnEnterTree = FindViewById<Button>(Resource.Id.btnEnterTree);
            btnReport = FindViewById<Button>(Resource.Id.btnReport);
            btnSalePrice = FindViewById<Button>(Resource.Id.btnSalePrice);
            btnExitMain = FindViewById<Button>(Resource.Id.btnExitMain);
            btnViewReport = FindViewById<Button>(Resource.Id.btnView);
            btnShowCutUnitList = FindViewById<Button>(Resource.Id.btnShowCutUnitList);

            tvSaleNum = FindViewById<TextView>(Resource.Id.tvSaleNum);
            txtPaymentUnit = FindViewById<EditText>(Resource.Id.txtPaymentUnit);
            txtUserInit = FindViewById<EditText>(Resource.Id.txtUserInit);
            txtContractNum = FindViewById<EditText>(Resource.Id.txtContractNum);
            txtContractor = FindViewById<EditText>(Resource.Id.txtContractor);

            //1. Started with NEW and select a valid cruise file
            if (!String.IsNullOrEmpty(cruiseFile))
            {
                //first check if cruise file name hase space. 
                if (cruiseFile.Contains("%20"))
                {
                    newCruiseFile = cruiseFile.Replace("%20", " ");
                    cruiseFile = newCruiseFile;
                }
                //txtCutUnit.RequestFocus();
                string crzFileDisplay;
                int position = cruiseFile.LastIndexOf('/');
                crzFileDisplay = cruiseFile.Substring(position + 1);
                //display the file name only (no path)
                tvCruiseFileName.Text = crzFileDisplay;
                myCruiseDB = new MyDatabase(cruiseFile);
                if (myCruiseDB.DatabaseAvailable)
                {
                    txtSaleName.Text = myCruiseDB.strQueryCruiseDB("Name", "Sale");
                    txvSaleName.Text = txtSaleName.Text;
                    sSaleNumber = myCruiseDB.strQueryCruiseDB("SaleNumber", "Sale");
                    txtSaleName.Focusable = false;
                    txtSaleName.Visibility = Android.Views.ViewStates.Gone;
                    txvSaleName.Visibility = Android.Views.ViewStates.Visible;

                    //CreateListItemFromCruise();
                    CreateCutUnitList();

                    //create the addon database file name
                    addonFile = cruiseFile.Replace(".cruise", ".addvol");
                    myAddonDB = new MyDatabase(addonFile);
                    sRegion = myCruiseDB.strQueryCruiseDB("Region", "Sale");
                    sForest = myCruiseDB.strQueryCruiseDB("Forest", "Sale");
                    sDist = myCruiseDB.strQueryCruiseDB("District", "Sale");
                    myAddonDB.AddRecordToSaleInfo(txtSaleName.Text, sSaleNumber, cruiseFile, sRegion, sForest, sDist);
                }
            }
            //2. Started with OPEN a valid addon file to continue adding trees
            else if (!String.IsNullOrEmpty(addonFile))
            {
                if (addonFile.Contains("%20"))
                {
                    addonFile = addonFile.Replace("%20", " ");
                }
                myAddonDB = new MyDatabase(addonFile);
                addvolFolder = addonFile.Substring(0, addonFile.LastIndexOf('/'));
                //need to get the cruise file from addon DB SaleInfo table
                cruiseFile = myAddonDB.GetCruiseFileFromAddon("CruiseFile");
                sSaleNumber = myAddonDB.strQueryCruiseDB("SaleNum", "SaleInfo");
                if (!String.IsNullOrEmpty(cruiseFile))
                {
                    myCruiseDB = new MyDatabase(cruiseFile);
                    //display cruise file and sale name on the screen
                    tvCruiseFileName.Text = cruiseFile.Substring(cruiseFile.LastIndexOf('/') + 1);
                    txtSaleName.Text = myAddonDB.GetCruiseFileFromAddon("SaleName");
                    txvSaleName.Text = txtSaleName.Text;
                    sSaleNumber = myCruiseDB.strQueryCruiseDB("SaleNumber", "Sale");
                    txtSaleName.Focusable = false;
                    txtSaleName.Visibility = Android.Views.ViewStates.Gone;
                    txvSaleName.Visibility = Android.Views.ViewStates.Visible;
                    sRegion = myCruiseDB.strQueryCruiseDB("Region", "Sale");
                    sForest = myCruiseDB.strQueryCruiseDB("Forest", "Sale");
                    sDist = myCruiseDB.strQueryCruiseDB("District", "Sale");
                    myAddonDB.AddRecordToSaleInfo(txtSaleName.Text, sSaleNumber, cruiseFile, sRegion, sForest, sDist);
                    //CreateListItemFromCruise();
                    CreateCutUnitList();
                }
                else
                //the addon does not have a cruise file, alert to select cruise file
                {
                    if (!string.IsNullOrEmpty(CruiseFilePicked) && CruiseFilePicked.Contains(".cruise"))
                    {
                        //txtCutUnit2.Focusable = false;
                        if (CruiseFilePicked.Contains("%20"))
                        {
                            CruiseFilePicked = CruiseFilePicked.Replace("%20", " ");
                        }
                        cruiseFile = CruiseFilePicked;
                        myCruiseDB = new MyDatabase(cruiseFile);
                        //display cruise file and sale name on the screen
                        tvCruiseFileName.Text = cruiseFile.Substring(cruiseFile.LastIndexOf('/') + 1);
                        txtSaleName.Text = myCruiseDB.strQueryCruiseDB("Name", "Sale");
                        txvSaleName.Text = txtSaleName.Text;
                        txtSaleName.Focusable = false;
                        txtSaleName.Visibility = Android.Views.ViewStates.Gone;
                        txvSaleName.Visibility = Android.Views.ViewStates.Visible;
                        sSaleNumber = myCruiseDB.strQueryCruiseDB("SaleNumber", "Sale");
                        sRegion = myCruiseDB.strQueryCruiseDB("Region", "Sale");
                        sForest = myCruiseDB.strQueryCruiseDB("Forest", "Sale");
                        sDist = myCruiseDB.strQueryCruiseDB("District", "Sale");
                        myAddonDB.AddRecordToSaleInfo(txtSaleName.Text, sSaleNumber, cruiseFile, sRegion, sForest, sDist);
                        //txtCutUnit.RequestFocus();
                    }
                    else
                    {
                        txtSaleName.Text = myAddonDB.GetSaleNameFromAddvol();
                        txvSaleName.Text = txtSaleName.Text;
                    }
                }
                CreateCutUnitList();
                txtCutUnit.RequestFocus();
            }
            //3. Started with NEW but did not select a cruise file but want to enter tree record anyway
            else
            {
                txtSaleName.RequestFocus();
            }

            if (!string.IsNullOrEmpty(addonFile))
            {
                txtContractNum.Text = myAddonDB.strQueryCruiseDB("ContractNumber", "SaleInfo");
                txtContractor.Text = myAddonDB.strQueryCruiseDB("Purchaser", "SaleInfo");
                txtUserInit.Text = myAddonDB.strQueryCruiseDB("Inspector", "SaleInfo");
            }

            tvSaleNum.Text = "SaleNumber: " + sSaleNumber;
            //set to display spinner or edittext item based on cruise file
            if (String.IsNullOrEmpty(cruiseFile) && (unitList.Count == 0 || unitList[0] == ""))
            {
                HideSpnCutUnit();
            }

            //var adapterUnit = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, unitList);
            var adapterUnit = new ArrayAdapter<string>(this, Resource.Layout.spnUnit_layout, unitList);
            txtCutUnit.Adapter = adapterUnit;
            txtCutUnit.ItemSelected += SpinnerUnit_ItemSelected;

            btnEnterTree.Click += BtnEnterTree_Click;
            btnReport.Click += BtnReport_Click;
            btnSalePrice.Click += BtnSalePrice_Click;
            btnExitMain.Click += BtnExitMain_Click;
            btnViewReport.Click += BtnViewReport_Click;
            btnShowCutUnitList.Click += BtnShowCutUnitList_Click;

            GetCursorView();

            // get ListView object instance from resource and add ItemClick, EventHandler.
            ListView lvTemp = FindViewById<ListView>(Resource.Id.lvTemp);
            lvTemp.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(ListView_ItemClick);

            //test following to pick cruise file
            if (string.IsNullOrEmpty(cruiseFile) && !string.IsNullOrEmpty(addonFile))
            {
                SelectCruiseFileAlert();
            }
            //pick file folder for addvol database
            if (string.IsNullOrEmpty(addonFile))
            {
                ChooseFolder();
            }
        }

        private void BtnExitMain_Click(object sender, EventArgs e)
        {
            base.Finish();
        }
        private void BtnShowCutUnitList_Click(object sender, EventArgs e)
        {
            if((unitList.Count==0 || unitList[0]=="") && txtCutUnit2.Text=="")
            {
                Toast.MakeText(this, "Please enter a unit code  ... ", ToastLength.Short).Show();
                return;
            }
            ShowSpnCutUnit();
            if (txtCutUnit2.Text.Length > 0)
            {
                unitList.Remove("New");
                if (!unitList.Contains(txtCutUnit2.Text)) unitList.Add(txtCutUnit2.Text + " " + txtCutUnit2Desc.Text);
                //unitList = unitList.Distinct().ToList();
                unitList.Add("New");
                //var adapterUnit = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, unitList);
                var adapterUnit = new ArrayAdapter<string>(this, Resource.Layout.spnUnit_layout, unitList);
                txtCutUnit.Adapter = adapterUnit;
                SetSpinnerSelection(txtCutUnit, unitList, txtCutUnit2.Text + " " + txtCutUnit2Desc.Text);
                if (string.IsNullOrEmpty(addonFile))
                {
                    if(string.IsNullOrEmpty(txtSaleName.Text))
                    {
                        Toast.MakeText(this, "Please enter a sale name ... ", ToastLength.Short).Show();
                        txtSaleName.RequestFocus();
                        return;
                    }
                    else if(!string.IsNullOrEmpty(addvolFolder))
                    {
                        addonFile = addvolFolder + txtSaleName.Text + ".addvol";
                        myAddonDB = new MyDatabase(addonFile);
                        myAddonDB.AddRecordToSaleInfo(txtSaleName.Text, sSaleNumber, cruiseFile, "", "", "");
                    }
                }
                if (!string.IsNullOrEmpty(addonFile))
                {
                    myAddonDB.AddNewCuttingUnit(txtCutUnit2.Text, txtCutUnit2Desc.Text, txtUserInit.Text);
                }
                txtCutUnit2.Text = "";
                txtCutUnit2Desc.Text = "";
            }
            else txtCutUnit.SetSelection(0);
        }
        private async void BtnViewReport_Click(object sender, EventArgs e)
        {
            //test display the report works!
            if (string.IsNullOrEmpty(file2display))
            {
                //SimpleFileDialog fileDialog = new SimpleFileDialog(this, SimpleFileDialog.FileSelectionMode.FileOpen);
                SimpleFileDialog fileDialog = new SimpleFileDialog(this, SimpleFileDialog.FileSelectionMode.OpenPDF);
                string path = await fileDialog.GetFileOrDirectoryAsync(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/Documents");
                if (!string.IsNullOrEmpty(path) && path.Contains(".pdf"))
                    //if (!string.IsNullOrEmpty(path) && path.Contains(".txt"))
                {
                    file2display = path;
                }
                else
                {
                    Toast.MakeText(this, "Please select a report  ... ", ToastLength.Short).Show();
                    return;
                }
            }    
            
            var filePath = file2display;
            var bytes = System.IO.File.ReadAllBytes(filePath);
            System.IO.File.WriteAllBytes(file2display, bytes);
            Java.IO.File file = new Java.IO.File(file2display);
            //Android.Net.Uri uri = Android.Net.Uri.FromFile(file);
            Intent intent = new Intent(Intent.ActionView);
            //intent.SetDataAndType(uri, "text/plain");
            intent.SetFlags(ActivityFlags.ClearWhenTaskReset | ActivityFlags.NewTask);
            //the following is added to handle android version for open txt file for API 24+
            if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
            //if (Build.VERSION.SdkInt >= Build.VERSION_CODES.N)
            {
                Android.Net.Uri uri = Android.Support.V4.Content.FileProvider.GetUriForFile(this, getPackageName() + ".provider", file);
                //intent.SetDataAndType(uri, "text/plain");
                intent.SetDataAndType(uri, "application/pdf");
                intent.AddFlags(ActivityFlags.GrantReadUriPermission);
            }
            else
            {
                Android.Net.Uri uri = Android.Net.Uri.FromFile(file);
                //intent.SetDataAndType(uri, "text/plain");
                intent.SetDataAndType(uri, "application/pdf");
            }
            //this StartActivity will not work for API 24+
            StartActivity(intent);
            file2display = "";
        }

        private string getPackageName()
        {
            return "com.AddonTree_Volume.AddVolume";
        }

        private Context getApplicationContext()
        {
            throw new NotImplementedException();
        }

        private void BtnReport_Click(object sender, EventArgs e)
        {
            var v = (Vibrator)Android.App.Application.Context.GetSystemService(Android.App.Application.VibratorService);
            //text file works!!
            //check cruise file to create report
            if(string.IsNullOrEmpty(cruiseFile))
            {
                Toast.MakeText(this, "You need to link to a Cruise file (.cruise) to create a report...", ToastLength.Short).Show();
                v.Vibrate(VibrationEffect.CreateOneShot(200, VibrationEffect.DefaultAmplitude));
                return;
            }
            //check if there is entered trees
            if(string.IsNullOrEmpty(addonFile))
            {
                Toast.MakeText(this, "The Addvol file (.addvol) has not been created...", ToastLength.Short).Show();
                v.Vibrate(VibrationEffect.CreateOneShot(200, VibrationEffect.DefaultAmplitude));
                return;
            }
            bool All = true;
            if(!myAddonDB.hasTreeToReport(All))
            {
                Toast.MakeText(this, "You have not entered any tree yet...", ToastLength.Short).Show();
                v.Vibrate(VibrationEffect.CreateOneShot(200, VibrationEffect.DefaultAmplitude));
                return;
            }
            if (txtContractNum.Text.Length == 0)
            {
                Toast.MakeText(this, "Please enter a Contract Number", ToastLength.Short).Show();
                txtContractNum.RequestFocus();
                //var inputManager = (Android.Views.InputMethods.InputMethodManager)GetSystemService(InputMethodService);
                //inputManager.ShowSoftInput()
                v.Vibrate(VibrationEffect.CreateOneShot(200, VibrationEffect.DefaultAmplitude));
                //showSoftKeyboard(txtContractNum);
            }
            else if (txtContractor.Text.Length == 0)
            {
                Toast.MakeText(this, "Please enter a Purchaser/Contractor ", ToastLength.Short).Show();
                txtContractor.RequestFocus();
                //v.Vibrate(100);
                v.Vibrate(VibrationEffect.CreateOneShot(200, VibrationEffect.DefaultAmplitude));
            }
            else
            {
                if (!string.IsNullOrEmpty(addonFile))
                {
                    UpdateTreeVol();
                    bool reportAll = false;
                    if (!myAddonDB.hasTreeToReport())
                    {
                        reportAll = true;
                        SummaryReport_Alert(reportAll);
                    }
                    else
                    {
                        //CreateSumVolReport();
                        SummaryReport_Alert(reportAll);
                    }


                }
                else
                {
                    Toast.MakeText(this, "The addvol database has not been created yet!", ToastLength.Short).Show();
                    //v.Vibrate(100);
                    v.Vibrate(VibrationEffect.CreateOneShot(200, VibrationEffect.DefaultAmplitude));
                }
                
            }
        }
        private void SummaryReport_Alert(bool All)
        {
            Android.App.AlertDialog.Builder dialog = new AlertDialog.Builder(this);
            AlertDialog alert = dialog.Create();
            alert.SetTitle("Create Summary Report");
            alert.SetIcon(Resource.Drawable.info);
            if (All)
            {
                bReportAll = true;
                alert.SetMessage("All addvol trees have been reported. Do you want an overall report?");
                alert.SetButton("Yes", (c, ev) =>
                {
                    CreateSumVolReportPDF();
                });
                alert.SetButton2("No", (c, ev) =>
                {
                    bReportAll = false;
                });
            }
            else
            {
                bReportAll = false;
                alert.SetMessage("You are going to create a report for the unreported trees. Do you want to use Today's date as inspection date for those trees?");
                alert.SetButton("Yes", (c, ev) =>
                {
                    sInspectionDate = sToday;
                    CreateSumVolReportPDF();
                });
                alert.SetButton2("No, pick a different date", (c, ev) =>
                {
                    bReportAll = false;
                    //test datepicker
                    var intent = new Intent(this, typeof(DatePickerActivity));
                    //StartActivity(intent);
                    int requestCode = 2022;
                    StartActivityForResult(intent, requestCode);
                    //end test
                    //create report with  differene date. ! need to pass in the selected date (have not set yet)
                    CreateSumVolReportPDF();
                });
                alert.SetButton3("Cancel", (c, ev) =>
                {
                    bReportAll = false;
                });
            }
            //alert.SetButton("Yes", (c, ev) =>
            //{
            //    //CreateSumVolReport();
            //    CreateSumVolReportPDF();
            //});
            //alert.SetButton2("No", (c, ev) =>
            //{
            //    bReportAll = false;                
            //    //test datepicker
            //    var intent = new Intent(this, typeof(DatePickerActivity));
            //    StartActivity(intent);
            //    //end test

            //});
            alert.Show();
        }
        private void BtnEnterTree_Click(object sender, EventArgs e)
        {
            //hideSoftKeyboard();
            Boolean goEnterTree = true;
            //var v = (Vibrator)Android.App.Application.Context.GetSystemService(Android.App.Application.VibratorService);
            if (string.IsNullOrEmpty(addonFile))
            {
                if(txtSaleName.Text.Length==0)
                {
                    Toast.MakeText(this, "Please enter a sale name to continue... ", ToastLength.Short).Show();
                    goEnterTree = false;
                    txtSaleName.RequestFocus();
                    //v.Vibrate(100);
                    vibrateDevice();
                }
                else
                {
                    if (string.IsNullOrEmpty(addvolFolder))
                    {
                        ChooseFolder();
                    }
                    if (!string.IsNullOrEmpty(addvolFolder))
                    {
                        addonFile = addvolFolder + txtSaleName.Text + ".addvol";
                        myAddonDB = new MyDatabase(addonFile);
                        myAddonDB.AddRecordToSaleInfo(txtSaleName.Text, sSaleNumber, cruiseFile, "", "", "");
                    }
                    else
                    {
                        goEnterTree = false;
                        Toast.MakeText(this, "Please select a folder for addvol file to continue... ", ToastLength.Short).Show();
                        ChooseFolder();
                    }
                }
            }
            if(goEnterTree)
            {
                if (txtUserInit.Text.Length == 0)
                {
                    Toast.MakeText(this, "Please enter User Initial", ToastLength.Short).Show();
                    txtUserInit.RequestFocus();
                    //v.Vibrate(100);
                    vibrateDevice();
                }
                else if (txtPaymentUnit.Text.Length == 0)
                {
                    Toast.MakeText(this, "Please enter Payment Unit", ToastLength.Short).Show();
                    txtPaymentUnit.RequestFocus();
                    //v.Vibrate(100);
                    vibrateDevice();
                    //showSoftKeyboard(txtPaymentUnit);
                }
                else if(llCutUnit2.Visibility == ViewStates.Visible && txtCutUnit2.Text.Length ==0)
                {
                    Toast.MakeText(this, "Please enter Cutting Unit", ToastLength.Short).Show();
                    txtCutUnit2.RequestFocus();
                    //v.Vibrate(100);
                    vibrateDevice();
                }
                //else if(unitList.Count==0)
                //{
                //    Toast.MakeText(this, "Please enter Cutting Unit", ToastLength.Short).Show();
                //    HideSpnCutUnit();
                //    txtCutUnit2.RequestFocus();
                //    v.Vibrate(100);
                //}
                else
                {
                    string sCutUnitCode;
                    if(llCutUnit2.Visibility == ViewStates.Visible)
                    {
                        sCutUnitCode = txtCutUnit2.Text;
                        //add new cutting unit code to NewcuttingUnit table
                        myAddonDB.AddNewCuttingUnit(txtCutUnit2.Text, txtCutUnit2Desc.Text, txtUserInit.Text);
                    }
                    else
                    {
                        
                        int idx = txtCutUnit.SelectedItem.ToString().IndexOf(" ", 1);
                        if (idx > 0)
                        {
                            sCutUnitCode = txtCutUnit.SelectedItem.ToString().Substring(0, idx);
                        }
                        else
                        {
                            sCutUnitCode = txtCutUnit.SelectedItem.ToString();
                        }
                        
                    }
                    
                    var intent = new Intent(this, typeof(EnterTreeActivity));
                    intent.PutExtra("cruiseFile", cruiseFile);
                    intent.PutExtra("addvolFile", addonFile);
                    intent.PutExtra("SaleName", txtSaleName.Text);
                    intent.PutExtra("SaleNum", sSaleNumber);
                    intent.PutExtra("CutUnit", sCutUnitCode);
                    intent.PutExtra("UserInit", txtUserInit.Text);
                    intent.PutExtra("PayUnit", txtPaymentUnit.Text);
                    intent.PutExtra("ID", "");
                    intent.PutExtra("Region", sRegion);
                    intent.PutExtra("Forest", sForest);
                    intent.PutExtra("District", sDist);
                    //StartActivity(intent);
                    int requestCode = 1234;
                    StartActivityForResult(intent, requestCode);
                }
            }
        }
        private void BtnSalePrice_Click(object sender, EventArgs e)
        {
            //hideSoftKeyboard();
            Boolean goSalePrice = true;
            //var v = (Vibrator)Android.App.Application.Context.GetSystemService(Android.App.Application.VibratorService);
            if (string.IsNullOrEmpty(addonFile))
            {
                if (txtSaleName.Text.Length == 0)
                {
                    Toast.MakeText(this, "Please enter a sale name to continue... ", ToastLength.Short).Show();
                    goSalePrice = false;
                    txtSaleName.RequestFocus();
                    //v.Vibrate(100);
                    vibrateDevice();
                }
                else
                {
                    if (string.IsNullOrEmpty(addvolFolder))
                    {
                        ChooseFolder();
                    }
                    if (!string.IsNullOrEmpty(addvolFolder))
                    {
                        addonFile = addvolFolder + txtSaleName.Text + ".addvol";
                        myAddonDB = new MyDatabase(addonFile);
                        myAddonDB.AddRecordToSaleInfo(txtSaleName.Text, sSaleNumber, cruiseFile, "", "", "");
                    }
                    else
                    {
                        goSalePrice = false;
                        Toast.MakeText(this, "Please select a folder for addvol file to continue... ", ToastLength.Short).Show();
                        ChooseFolder();
                    }
                }
            }
            if (goSalePrice)
            {
                if (txtUserInit.Text.Length == 0)
                {
                    Toast.MakeText(this, "Please enter User Initial", ToastLength.Short).Show();
                    txtUserInit.RequestFocus();
                    //v.Vibrate(100);
                    vibrateDevice();
                }
                else
                {
                    var intent = new Intent(this, typeof(SalePriceActivity));
                    intent.PutExtra("cruiseFile", cruiseFile);
                    intent.PutExtra("addvolFile", addonFile);
                    intent.PutExtra("SaleName", txtSaleName.Text);
                    intent.PutExtra("SaleNum", sSaleNumber);
                    intent.PutExtra("UserInit", txtUserInit.Text);
                    //StartActivity(intent);
                    int requestCode = 1000;
                    StartActivityForResult(intent, requestCode);
                }
            }
        }
        private void vibrateDevice()
        {
            var v = (Vibrator)Android.App.Application.Context.GetSystemService(Android.App.Application.VibratorService);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                v.Vibrate(VibrationEffect.CreateOneShot(200, VibrationEffect.DefaultAmplitude));
            }
            else v.Vibrate(100);
        }
        private void HideSpnCutUnit()
        {
            TableRow.LayoutParams cutunit2params = new TableRow.LayoutParams(0, TableRow.LayoutParams.WrapContent, 1.0f);
            llCutUnit2.LayoutParameters = cutunit2params;
            TableRow.LayoutParams cutunitparams = new TableRow.LayoutParams(0, TableRow.LayoutParams.WrapContent, 0.0f);
            txtCutUnit.LayoutParameters = cutunitparams;
            txtCutUnit.Visibility = Android.Views.ViewStates.Gone;
            llCutUnit2.Visibility = Android.Views.ViewStates.Visible;
        }
        private void ShowSpnCutUnit()
        {
            TableRow.LayoutParams cutunit2params = new TableRow.LayoutParams(0, TableRow.LayoutParams.WrapContent, 0.0f);
            llCutUnit2.LayoutParameters = cutunit2params;
            TableRow.LayoutParams cutunitparams = new TableRow.LayoutParams(0, TableRow.LayoutParams.WrapContent, 1.0f);
            txtCutUnit.LayoutParameters = cutunitparams;
            txtCutUnit.Visibility = Android.Views.ViewStates.Visible;
            llCutUnit2.Visibility = Android.Views.ViewStates.Gone;
        }
        private void UpdateTreeVol()
        {
            string spec, liveDead, prod;
            string voltype = "Primary";
            int id;
            double dbh, vol, vol2;
            float tht;
            Android.Database.ICursor Trees = myAddonDB.TreesNeedCalcCuft();
            if(Trees.MoveToFirst())
            {
                do
                {
                    id = int.Parse(Trees.GetString(0));
                    spec = Trees.GetString(1);
                    dbh = double.Parse(Trees.GetString(2));
                    liveDead = Trees.GetString(3);
                    prod = Trees.GetString(4);
                    if (!string.IsNullOrEmpty(Trees.GetString(5))) tht = float.Parse(Trees.GetString(5));
                    else tht = 0.0f;
                    vol = 0.0;
                    vol2 = 0.0;
                    vol = myCruiseDB.CalcVol(voltype, spec, dbh, prod, liveDead);
                    if (vol > 0)
                    {
                        voltype = "Secondary";
                        vol2 = myCruiseDB.CalcVol(voltype, spec, dbh, prod, liveDead);
                        if(vol2 > 0)
                        {
                            vol2 = vol2 - vol;
                            if (vol2 < 0) vol2 = 0;
                        }
                        //need to add vol2 to UpdateVol for SecondaryVol
                        //myAddonDB.UpdateVol(id, vol);
                    }
                    else
                    {
                        //volume is not calculated from Local volume table, then call volume library to calculate volume
                        float dbhob = (float)dbh;
                        CalcVolWithVollib(spec, prod, dbhob, tht);
                        vol = fPrimVol;
                        vol2 = fSecdVol;
                    }
                    if (vol > 0)
                    {
                        vol = Math.Round(vol, 1);
                        vol2 = Math.Round(vol2, 1);
                        myAddonDB.UpdateVol(id, vol, vol2);
                    }
                } while (Trees.MoveToNext());
            }
        }
        private void CalcVolWithVollib(string spec, string prod, float dbhob, float totht)
        {
            fPrimVol = 0.0f;
            fSecdVol = 0.0f;
            float mtopp, mtops;
            int regn;
            string sWhere = " WHERE Species = '" + spec + "' AND PrimaryProduct = '" + prod + "' ";
            string voleq = myCruiseDB.getValFromDB("VolumeEquationNumber", "VolumeEquation", sWhere);
            string strMtopp = myCruiseDB.getValFromDB("TopDIBPrimary", "VolumeEquation", sWhere);
            string strMtops = myCruiseDB.getValFromDB("TopDIBSecondary", "VolumeEquation", sWhere);
            string strCalcTW = myCruiseDB.getValFromDB("CalcTopwood", "VolumeEquation", sWhere);
            if (string.IsNullOrEmpty(strMtopp)) mtopp = 0.0f;
            else mtopp = float.Parse(strMtopp);
            if (string.IsNullOrEmpty(strMtops)) mtops = 0.0f;
            else mtops = float.Parse(strMtops);
            //if the species has no voleq in the cruise file, then get the default voleq from volume library 
            if (string.IsNullOrEmpty(voleq))
            {
                regn = int.Parse(sRegion);
                StringBuilder forstc = new StringBuilder(256);
                StringBuilder distc = new StringBuilder(256);
                StringBuilder prodc = new StringBuilder(256);
                StringBuilder voleqc = new StringBuilder(256);
                sWhere = " WHERE Species = '" + spec + "' AND FIAcode > 0";
                string sFIAcode = myCruiseDB.getValFromDB("FIAcode", "TreeDefaultValue", sWhere);
                if (string.IsNullOrEmpty(sFIAcode))
                {
                    int Num;
                    bool isInt = int.TryParse(spec, out Num);
                    if (isInt) sFIAcode = spec;
                }
                if (!string.IsNullOrEmpty(sFIAcode))
                {
                    
                    int ispec = int.Parse(sFIAcode);
                    forstc.Append(sForest);
                    distc.Append(sDist);
                    prodc.Append(prod);
                    int errflg = 0;
                    int i256 = 256;
                    VolEqDef(ref regn, forstc, distc, ref ispec, prodc, voleqc, ref errflg, i256, i256, i256, i256);
                    if (errflg == 0 || (errflg > 0 && voleqc.Length == 10))
                    {
                        voleq = voleqc.ToString();
                    }
                }
                else
                {
                    voleq = null;
                    Toast.MakeText(this, "There is no volume equation for " + spec, ToastLength.Short).Show();
                }
                if (!string.IsNullOrEmpty(voleq))
                {
                    float totcu = 0;
                    float sawcu = 0;
                    float mcu4 = 0;
                    float scrbbf = 0;
                    float intlbf = 0;
                    int errflg = 0;
                    int forst = int.Parse(sForest);
                    //CalcVolFromVollib(ref regn, ref forst, voleq, ref dbhob, ref totht, ref totcu, ref sawcu, ref mcu4, ref scrbbf, ref intlbf, ref errflg);
                    GetVolFromVollib(ref regn, ref forst, voleq, ref dbhob, ref totht, ref mtopp, ref mtops, prod, ref totcu, ref sawcu, ref mcu4, ref scrbbf, ref intlbf, ref errflg);
                    if (errflg == 0)
                    {
                        fPrimVol = sawcu;
                        if (strCalcTW == "1")
                        {
                            fSecdVol = mcu4 - sawcu;
                            if (fSecdVol < 0.0) fSecdVol = 0.0f;
                        }
                    }
                }
            }

        }
        private void CreateCutUnitList()
        {
            Android.Database.ICursor CuttingUnitList;
            string unitcode;
            unitList = new List<string>();
            if (!string.IsNullOrEmpty(cruiseFile))
            {
                CuttingUnitList = myCruiseDB.GetCuttingUnitCode("CuttingUnit");
                if (CuttingUnitList != null)
                {
                    if (CuttingUnitList.MoveToFirst())
                    {
                        
                        do
                        {
                            unitcode = CuttingUnitList.GetString(CuttingUnitList.GetColumnIndex("Code"));
                            unitList.Add(unitcode);
                        } while (CuttingUnitList.MoveToNext());
                    }
                }
            }
            if(!string.IsNullOrEmpty(addonFile))
            {
                CuttingUnitList = myAddonDB.GetCuttingUnitCode("NewCuttingUnit");
                if (CuttingUnitList != null)
                {
                    if (CuttingUnitList.MoveToFirst())
                    {
                        do
                        {
                            unitcode = CuttingUnitList.GetString(CuttingUnitList.GetColumnIndex("Code"));
                            unitList.Add(unitcode);
                        } while (CuttingUnitList.MoveToNext());
                    }
                }
            }
            if(unitList.Count()>0)
            { unitList.Add("New"); }
            else
            {
                HideSpnCutUnit();
                txtCutUnit2.RequestFocus();
            }
        }

        private void SpinnerUnit_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            string s1 = unitList[e.Position].ToString();
            if(s1=="New")
            {
                HideSpnCutUnit();
                txtCutUnit2.RequestFocus();
            }
        }
        /// <summary>
		/// Lists the view_ item click.
		/// </summary>
		/// <param name='sender'>
		/// object sender.
		/// </param>
		/// <param name='e'>
		/// ItemClickEventArgs e.
		/// </param>
		void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            // get TextView object instance from resource layout record_view.axml.
            TextView tvIdShow = e.View.FindViewById<TextView>(Resource.Id.tvIdShow);
            TextView tvSpeciesShow = e.View.FindViewById<TextView>(Resource.Id.tvSpeciesShow);
            TextView tvDBHShow = e.View.FindViewById<TextView>(Resource.Id.tvDBHShow);
            TextView tvProdShow = e.View.FindViewById<TextView>(Resource.Id.tvProdShow);
            TextView tvCutUnitShow = e.View.FindViewById<TextView>(Resource.Id.tvCutUnitShow);
            TextView tvLDShow = e.View.FindViewById<TextView>(Resource.Id.tvLiveDeadShow);
            TextView tvCUShow = e.View.FindViewById<TextView>(Resource.Id.tvCUFTShow);
            TextView tvTHTShow = e.View.FindViewById<TextView>(Resource.Id.tvTotHTShow);
            TextView tvAddRmvShow = e.View.FindViewById<TextView>(Resource.Id.tvAddRemoveShow);
            TextView tvPayUnitShow = e.View.FindViewById<TextView>(Resource.Id.tvPayUnitShow);
            TextView tvTrCntShow = e.View.FindViewById<TextView>(Resource.Id.tvTrCntShow);

            //send items to EnterTree screen to modify
            string sID, sSpecies, sDBH, sLD, sProd;
            sID = tvIdShow.Text;
            sSpecies = tvSpeciesShow.Text;
            sDBH = double.Parse(tvDBHShow.Text).ToString("0.0");
            sLD = tvLDShow.Text;
            sProd = tvProdShow.Text;
            var intent = new Intent(this, typeof(EnterTreeActivity));
            intent.PutExtra("cruiseFile", cruiseFile);
            intent.PutExtra("addvolFile", addonFile);
            intent.PutExtra("SaleName", txtSaleName.Text);
            intent.PutExtra("SaleName", txtSaleName.Text);
            intent.PutExtra("SaleNum", sSaleNumber);
            intent.PutExtra("CutUnit", tvCutUnitShow.Text);
            intent.PutExtra("UserInit", txtUserInit.Text);
            intent.PutExtra("PayUnit", tvPayUnitShow.Text);

            intent.PutExtra("ID", sID);
            intent.PutExtra("Species", sSpecies);
            intent.PutExtra("DBH", sDBH);
            intent.PutExtra("LD", sLD);
            intent.PutExtra("Prod", sProd);
            intent.PutExtra("AddRemove", tvAddRmvShow.Text);
            intent.PutExtra("THT", tvTHTShow.Text);
            intent.PutExtra("Region", sRegion);
            intent.PutExtra("Forest", sForest);
            intent.PutExtra("District", sDist);
            intent.PutExtra("TreeCount", tvTrCntShow.Text);
            //StartActivity(intent);
            int requestCode = 1234;
            StartActivityForResult(intent, requestCode);
        }
        /// <summary>
		/// Gets the cursor view to show all record.
		/// </summary>
		protected void GetCursorView()
        {
            //if (myAddonDB.DatabaseAvailable)
            if(addonFile.Length>0)
            {
                Android.Database.ICursor icTemp = myAddonDB.GetRecordCursor();
                if (icTemp != null && icTemp.MoveToFirst())
                {
                    //icTemp.MoveToFirst();
                    ListView lvTemp = FindViewById<ListView>(Resource.Id.lvTemp);
                    string[] from = new string[] { "_id", "Species", "DBH", "LiveDead", "Prod","CuttingUnit", "PaymentUnit","AddRemove", "PrimaryVol", "SecondaryVol", "TotalHt", "TreeCount" };
                    int[] to = new int[] {
                    Resource.Id.tvIdShow,
                    Resource.Id.tvSpeciesShow,
                    Resource.Id.tvDBHShow,
                    Resource.Id.tvLiveDeadShow,
                    Resource.Id.tvProdShow,
                    Resource.Id.tvCutUnitShow,
                    Resource.Id.tvPayUnitShow,
                    Resource.Id.tvAddRemoveShow,
                    Resource.Id.tvCUFTShow,
                    Resource.Id.tvSecdVolShow,
                    Resource.Id.tvTotHTShow,
                    Resource.Id.tvTrCntShow
                    };
                    // creating a SimpleCursorAdapter to fill ListView object.
                    SimpleCursorAdapter scaTemp = new SimpleCursorAdapter(this, Resource.Layout.record_view, icTemp, from, to, 0);
                    lvTemp.Adapter = scaTemp;
                }
                else
                {
                    ListView lvTemp = FindViewById<ListView>(Resource.Id.lvTemp);
                    lvTemp.Adapter = null;
                }
            }
        }
        private void SetSpinnerSelection(Spinner spinner, List<string> array, string text)
        {
            for (int i = 0; i < array.Count; i++)
            {
                if (array[i].Equals(text))
                {
                    spinner.SetSelection(i);
                }
            }
        }

        //alert to slect cruise file
        private void SelectCruiseFileAlert()
        {
            //int resultCode = 1111;

            Android.App.AlertDialog.Builder dialog = new AlertDialog.Builder(this);
            AlertDialog alert = dialog.Create();
            alert.SetTitle("Select Cruise File (.cruise)");
            alert.SetMessage("This addvol file does not have a cruise file for local volume table. Do you want to add a cruise file for it?");
            alert.SetIcon(Resource.Drawable.info);
            alert.SetButton("Yes", (c, ev) =>
            {
                // Ok button click to pick cruise file
                //Intent i = new Intent(Intent.ActionGetContent);
                //StartActivityForResult(i, resultCode);
                pickCruiseFile();
            });
            alert.SetButton2("No", (c, ev) => {
            });
            alert.Show();
        }
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            //refresh list view for tree
            GetCursorView();
            if(llCutUnit2.Visibility == ViewStates.Visible)
            {
                CreateCutUnitList();
                //var adapterUnit = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, unitList);
                //var adapterUnit = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, unitList);
                var adapterUnit = new ArrayAdapter<string>(this, Resource.Layout.spnUnit_layout, unitList);
                txtCutUnit.Adapter = adapterUnit;
                ShowSpnCutUnit();
                SetSpinnerSelection(txtCutUnit, unitList, txtCutUnit2.Text + " " + txtCutUnit2Desc.Text);
            }
            //get the date picked for InspectionDAte
            if(requestCode==2022 && resultCode==Result.Ok)
            {
                sInspectionDate = data.GetStringExtra("InspectionDate");
                //Toast.MakeText(this, "2. InspectionDate: " + sInspectionDate, ToastLength.Long).Show();
            }
        }
        public void hideSoftKeyboard()
        {
            //Hide keyboard
            var inputManager = (Android.Views.InputMethods.InputMethodManager)GetSystemService(InputMethodService);
            inputManager.HideSoftInputFromWindow(btnEnterTree.WindowToken, Android.Views.InputMethods.HideSoftInputFlags.None);
        }
        //private void showSoftKeyboard(View view)
        //{
        //    if (view.RequestFocus())
        //    {
        //        InputMethodManager imm = (InputMethodManager)GetSystemService(Context.InputMethodService);
        //        imm.ShowSoftInput(view, InputMethodManager.ShowImplicit);
        //    }
        //}

        //choose a file folder
        private async void ChooseFolder()
        {
            SimpleFileDialog fileDialog = new SimpleFileDialog(this, SimpleFileDialog.FileSelectionMode.FolderChoose);
            string path = await fileDialog.GetFileOrDirectoryAsync(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/Documents");
            if (!string.IsNullOrEmpty(path))
            {
                //Use path
                addvolFolder = path + "/";
            }
        }
        private async void pickCruiseFile()
        {
            SimpleFileDialog fileDialog = new SimpleFileDialog(this, SimpleFileDialog.FileSelectionMode.OpenCruise);
            string path = await fileDialog.GetFileOrDirectoryAsync(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/Documents");
            path = path.Replace(".CRUISE", ".cruise");
            if (!string.IsNullOrEmpty(path) && path.Contains(".cruise"))
            {
                CruiseFilePicked = path;
                cruiseFile = CruiseFilePicked;
                myCruiseDB = new MyDatabase(cruiseFile);
                tvCruiseFileName.Text = cruiseFile.Substring(cruiseFile.LastIndexOf('/') + 1);
                txtSaleName.Text = myCruiseDB.strQueryCruiseDB("Name", "Sale");
                txvSaleName.Text = txtSaleName.Text;
                txtSaleName.Visibility = Android.Views.ViewStates.Gone;
                txvSaleName.Visibility = Android.Views.ViewStates.Visible;
                sSaleNumber = myCruiseDB.strQueryCruiseDB("SaleNumber", "Sale");
                tvSaleNum.Text = "SaleNumber: " + sSaleNumber;
                CreateCutUnitList();
                //set to display cutting unit spinner
                ShowSpnCutUnit();
                //var adapterUnit = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, unitList);
                var adapterUnit = new ArrayAdapter<string>(this, Resource.Layout.spnUnit_layout, unitList);
                txtCutUnit.Adapter = adapterUnit;
                sRegion = myCruiseDB.strQueryCruiseDB("Region", "Sale");
                sForest = myCruiseDB.strQueryCruiseDB("Forest", "Sale");
                sDist = myCruiseDB.strQueryCruiseDB("District", "Sale");
                myAddonDB.AddRecordToSaleInfo(txtSaleName.Text, sSaleNumber, cruiseFile, sRegion, sForest, sDist);
                //recalc vol from local voltable
                UpdateTreeVol();
                //refresh list view for tree
                GetCursorView();
            }
            //CreateCutUnitList();
            ////set to display cutting unit spinner
            //if (unitList.Count() > 0) ShowSpnCutUnit();
        }
        private async void FileSaveAs(string filename)
        {
            SimpleFileDialog fileDialog = new SimpleFileDialog(this, SimpleFileDialog.FileSelectionMode.FileSave);
            fileDialog.DefaultFileName = filename;
            string filepath = await fileDialog.GetFileOrDirectoryAsync(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/Documents");
            
        }
        private async void CreateSumVolReport()
        {

            ////test using iTextSharp
            //string pdfFile = "testPDF.pdf";
            //BaseFont bfTimes = BaseFont.CreateFont(BaseFont.TIMES_ROMAN, BaseFont.CP1252, false);
            //Font times = new Font(bfTimes, 12, Font.ITALIC, iTextSharp.text.Color.RED);
            //Font Courier10 = FontFactory.GetFont("Courier", 10);
            //Font Courier12 = FontFactory.GetFont("Courier", 12);
            //string root = Environment.ExternalStorageDirectory.AbsolutePath + "/Documents";
            //if (System.IO.File.Exists(root + "/" + pdfFile)) System.IO.File.Delete(root + "/" + pdfFile);
            //FileStream fs = new FileStream(root + "/" + pdfFile, FileMode.Create);
            //Document document = new Document(PageSize.A4, 25, 25, 30, 30);
            //PdfWriter writer = PdfWriter.GetInstance(document, fs);
            //// Open the document to enable you to write to the document  
            //document.Open();
            //// Add a simple and wellknown phrase to the document in a flow layout 
            //string text = string.Format("{0,56}", "Report of Additional Volume");
            //document.Add(new Paragraph(text));
            //document.Add(new Paragraph(text, Courier12));
            //text = string.Format("{0,54}", "Tree Measurement Sales");
            //document.Add(new Paragraph(text, Courier12));
            //text = "";
            //document.Add(new Paragraph(text));
            //document.Add(new Paragraph("Hello World!!!!",times));
            //document.Add(new Paragraph("Hello World!!!!", Courier10));
            //for (int ii = 0; ii < 100; ii++)
            //{
            //    document.Add(new Paragraph(ii+" -- Hello World!!!!", Courier10));
            //}
            //// Close the document  
            //document.Close();
            //// Close the writer instance  
            //writer.Close();
            //// Always close open filehandles explicity
            //fs.Close();
            ////End test itextShaps for PDF

            //bool overwrite = false;
            string sFileExt;
            string sFileName = addonFile.Replace(".addvol", "_" + sToday);
            double total = 0;
            double totValue = 0;
            double fRate, fValue, fVol;
            double unitTotVol, unitTotValue;
            string sText;
            Font Courier10 = FontFactory.GetFont("Courier", 10);
            Font Courier12 = FontFactory.GetFont("Courier", 12);
            string pdfFile;
            if (bReportAll)
                sFileExt = "_All.txt";
                //sFileExt = "_All.pdf";
            else
                sFileExt = ".txt";
                //sFileExt = ".pdf";

            var txtFile = sFileName + sFileExt;
            int i = 1;
            while (System.IO.File.Exists(txtFile))
            {
                //System.IO.File.Delete(txtFile);
                if (txtFile.Contains("(" + i + ")"))
                {
                    txtFile = txtFile.Replace("(" + i + ")", "");
                    i += 1;
                }
                txtFile = txtFile.Replace(".txt", "(" + i + ")" + ".txt");
                //txtFile = txtFile.Replace(".pdf", "(" + i + ")" + ".pdf");
            }

            string sTxtFileNameOnly = txtFile.Substring(txtFile.LastIndexOf('/') + 1);
            SimpleFileDialog fileDialog = new SimpleFileDialog(this, SimpleFileDialog.FileSelectionMode.FileSave);
            fileDialog.DefaultFileName = sTxtFileNameOnly;
            txtFile = await fileDialog.GetFileOrDirectoryAsync(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/Documents");

            if (!string.IsNullOrEmpty(txtFile) && !System.IO.File.Exists(txtFile))
            {
                pdfFile = txtFile.Replace(".txt", ".pdf");
                FileStream fs = new FileStream(pdfFile, FileMode.Create);
                Document document = new Document(PageSize.A4, 25, 25, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, fs);
                // Open the document to enable you to write to the document  
                document.Open();

                using (System.IO.StreamWriter write = new System.IO.StreamWriter(txtFile, true))
                {
                    write.WriteLine(string.Format("{0,56}", "Report of Additional Volume"));
                    write.WriteLine(string.Format("{0,54}", "Tree Measurement Sales"));
                    write.WriteLine("");
                    write.WriteLine(string.Format("{0,20} {1,-20} {2,22} {3,-20}", "Sale Name:", txtSaleName.Text, "Forest:", sForest));
                    write.WriteLine(string.Format("{0,20} {1,-20} {2,22} {3,-20}", "Contract Number:", txtContractNum.Text, "Ranger District:", sDist));

                    sText = string.Format("{0,50}", "Report of Additional Volume");
                    document.Add(new Paragraph(sText, Courier12));
                    sText = string.Format("{0,48}", "Tree Measurement Sales");
                    document.Add(new Paragraph(sText, Courier12));
                    sText = " ";
                    document.Add(new Paragraph(sText, Courier10));
                    sText = string.Format("{0,20} {1,-20} {2,22} {3,-20}", "Sale Name:", txtSaleName.Text, "Forest:", sForest);
                    document.Add(new Paragraph(sText, Courier10));
                    sText = string.Format("{0,20} {1,-20} {2,22} {3,-20}", "Contract Number:", txtContractNum.Text, "Ranger District:", sDist);
                    document.Add(new Paragraph(sText, Courier10));

                    string sRptDate, sPayUnit, sSpec, sDBH, sProd, sCU, sVol2, sCutUnit, sLD, sAddDel, sHT;
                    string sWhere, sOrder, sAddRemoveText;
                    Android.Database.ICursor PayUnitCur, AddRemoveCur, ReportDateCur, SumCur;
                    myAddonDB.PopulateSummaryTemp(bReportAll, sInspectionDate);
                    string sMinDate, sMaxDate, sMonthYr;
                    sMinDate = myAddonDB.getValFromDB("Min(ReportDate)", "SummaryTemp", null);
                    sMaxDate = myAddonDB.getValFromDB("Max(ReportDate)", "SummaryTemp", null);
                    if (sMinDate.Substring(0, 7) == sMaxDate.Substring(0, 7)) sMonthYr = sMinDate.Substring(5, 2) + "/" + sMinDate.Substring(0, 4);
                    else sMonthYr = sMinDate.Substring(5, 2) + "/" + sMinDate.Substring(0, 4) + " - " + sMaxDate.Substring(5, 2) + "/" + sMaxDate.Substring(0, 4);
                    write.WriteLine(string.Format("{0,20} {1,-20} {2,22} {3,-20}", "Reporting Month/Yr:", sMonthYr, "Purchaser/Contractor:", txtContractor.Text));
                    write.WriteLine("");
                    sText = string.Format("{0,20} {1,-20} {2,22} {3,-20}", "Reporting Month/Yr:", sMonthYr, "Purchaser/Contractor:", txtContractor.Text);
                    document.Add(new Paragraph(sText, Courier10));
                    sText = " ";
                    document.Add(new Paragraph(sText, Courier10));

                    PayUnitCur = myAddonDB.lvQueryCruiseDB("PaymentUnit", "SummaryTemp");
                    if(PayUnitCur != null)
                    {
                        if (PayUnitCur.MoveToFirst())
                        {
                            do
                            {
                                
                                sPayUnit = PayUnitCur.GetString(0);
                                write.WriteLine("Payment Unit: " + PayUnitCur.GetString(0));
                                write.WriteLine("===================");
                                sText = "Payment Unit: " + PayUnitCur.GetString(0);
                                document.Add(new Paragraph(sText, Courier10));
                                sText = "===================";
                                document.Add(new Paragraph(sText, Courier10));

                                sWhere = "PaymentUnit = '" + PayUnitCur.GetString(0) + "' AND ";
                                sOrder = "";
                                AddRemoveCur = myAddonDB.lvQueryCruiseDB("AddRemove", "SummaryTemp", sWhere, sOrder);
                                if (AddRemoveCur != null)
                                {
                                    if (AddRemoveCur.MoveToFirst())
                                    {
                                        do
                                        {
                                            sAddDel = AddRemoveCur.GetString(0);
                                            sAddRemoveText = "Volume added (+) to the contract";
                                            if (AddRemoveCur.GetString(0) == "-") sAddRemoveText = "Volume removed (-) from the contract";
                                            write.WriteLine(sAddRemoveText);
                                            sText = sAddRemoveText;
                                            document.Add(new Paragraph(sText, Courier10));
                                            string sWhere2 = sWhere + "AddRemove = '" + AddRemoveCur.GetString(0) + "' AND ";
                                            string sOrder2 = "DESC";
                                            int count = 0;
                                            unitTotValue = 0;
                                            unitTotVol = 0;
                                            ReportDateCur = myAddonDB.lvQueryCruiseDB("ReportDate", "SummaryTemp", sWhere2, sOrder2);
                                            if (ReportDateCur != null)
                                            {
                                                if (ReportDateCur.MoveToFirst())
                                                {
                                                    do
                                                    {
                                                        count += 1;
                                                        sRptDate = ReportDateCur.GetString(0);
                                                        write.WriteLine("Inspection Report Date: " + ReportDateCur.GetString(0));
                                                        sText = "Inspection Report Date: " + ReportDateCur.GetString(0);
                                                        document.Add(new Paragraph(sText, Courier10));

                                                        total = 0;
                                                        totValue = 0;
                                                        fValue = 0;
                                                        SumCur = myAddonDB.lvQuerySummaryVol(sPayUnit, sRptDate, sAddDel);
                                                        if (SumCur != null)
                                                        {
                                                            if (SumCur.MoveToFirst())
                                                            {
                                                                write.WriteLine("________________________________________________________");
                                                                write.WriteLine(string.Format("{0,-8} | {1,-8} | {2,12} | {3,12} | {4,8}", "Species", "Product", "Volume(CF)", "Rate($/CCF)", "Value($)"));
                                                                write.WriteLine("________________________________________________________");
                                                                sText = "____________________________________________________________";
                                                                document.Add(new Paragraph(sText, Courier10));
                                                                sText = string.Format("{0,-8} | {1,-8} | {2,12} | {3,12} | {4,8}", "Species", "Product", "Volume(CF)", "Rate($/CCF)", "Value($)");
                                                                document.Add(new Paragraph(sText, Courier10));
                                                                sText = "____________________________________________________________";
                                                                document.Add(new Paragraph(sText, Courier10));

                                                                do
                                                                {
                                                                    sSpec = SumCur.GetString(0);
                                                                    sProd = SumCur.GetString(1);
                                                                    sCU = SumCur.GetString(2);
                                                                    string sWhere3 = " WHERE Species = '" + sSpec + "' AND Prod = '" + sProd + "'";
                                                                    string sRate = myAddonDB.getValFromDB("Price", "SalePrice", sWhere3);
                                                                    if(string.IsNullOrEmpty(sRate))
                                                                    {
                                                                        //check ContractSpecies
                                                                        string sWhere4 = " WHERE Species = '" + sSpec + "' AND PrimaryProduct = '" + sProd + "'";
                                                                        string sContractSpec = myCruiseDB.getValFromDB("ContractSpecies", "TreeDefaultValue", sWhere4);
                                                                        if(!string.IsNullOrEmpty(sContractSpec))
                                                                        {
                                                                            sWhere3 = " WHERE Species = '" + sContractSpec + "' AND Prod = '" + sProd + "'";
                                                                            sRate = myAddonDB.getValFromDB("Price", "SalePrice", sWhere3);
                                                                        }
                                                                    }
                                                                    //float fRate, fValue,fVol;
                                                                    if (!string.IsNullOrEmpty(sCU)) fVol = double.Parse(sCU);
                                                                    else fVol = 0;
                                                                    string sValue = "";
                                                                    if (!string.IsNullOrEmpty(sRate))
                                                                    {
                                                                        fRate = double.Parse(sRate);
                                                                        fValue = fRate * fVol / 100;
                                                                        fValue = Math.Round(fValue, 2);
                                                                        sValue = fValue.ToString();
                                                                    }
                                                                    else fValue = 0;
                                                                    write.WriteLine(string.Format("{0,-8} | {1,-8} | {2,12} | {3,12} | {4,8}", sSpec, sProd, sCU, sRate, sValue));
                                                                    sText = string.Format("{0,-8} | {1,-8} | {2,12} | {3,12} | {4,8}", sSpec, sProd, sCU, sRate, sValue);
                                                                    document.Add(new Paragraph(sText, Courier10));
                                                                    total += fVol;
                                                                    totValue += fValue;
                                                                } while (SumCur.MoveToNext());
                                                                totValue = Math.Round(totValue, 2);
                                                                total = Math.Round(total,1);
                                                                write.WriteLine("____________________________________________________________");
                                                                write.WriteLine(string.Format("{0,20} {1,9} {2,25}", "Total:", total.ToString(), totValue.ToString()));
                                                                write.WriteLine();
                                                                sText = "____________________________________________________________";
                                                                document.Add(new Paragraph(sText, Courier10));
                                                                sText = string.Format("{0,20} {1,9} {2,25}", "Total:", total.ToString(), totValue.ToString());
                                                                document.Add(new Paragraph(sText, Courier10));
                                                                sText = " ";
                                                                document.Add(new Paragraph(sText, Courier10));

                                                                unitTotVol += total;
                                                                unitTotValue += totValue;
                                                            }
                                                        }
                                                    } while (ReportDateCur.MoveToNext());
                                                    
                                                    if (count > 1)
                                                    {
                                                        unitTotValue = Math.Round(unitTotValue, 2);
                                                        unitTotVol = Math.Round(unitTotVol, 1);
                                                        write.WriteLine(string.Format("{0,20} {1,9} {2,25}", "Unit " + sPayUnit + " Total:", unitTotVol.ToString(), unitTotValue.ToString()));
                                                        write.WriteLine();
                                                        sText = string.Format("{0,20} {1,9} {2,25}", "Unit " + sPayUnit + " Total:", unitTotVol.ToString(), unitTotValue.ToString());
                                                        document.Add(new Paragraph(sText, Courier10));
                                                        sText = " ";
                                                        document.Add(new Paragraph(sText, Courier10));

                                                    }
                                                }
                                            }
                                        } while (AddRemoveCur.MoveToNext());
                                    }
                                }
                            } while (PayUnitCur.MoveToNext());
                        }
                    }

                    Android.Database.ICursor AddRmvVol;

                    write.WriteLine("");
                    sText = " ";
                    document.Add(new Paragraph(sText, Courier10));

                    //now add list of all trees
                    AddRmvVol = myAddonDB.lvQueryAddTree(bReportAll);
                    if (AddRmvVol != null)
                    {
                        if (AddRmvVol.MoveToFirst())
                        {
                            write.WriteLine("List of trees for additional volume(CF):");
                            write.WriteLine("_______________________________________________________________________________________");
                            write.WriteLine(string.Format("{0,-7} | {1,-7} | {2,-7} | {3,4} | {4,4} | {5,3} | {6,7} | {7,8} | {8,8} | {9,5}", "PayUnit", "CutUnit", "Species", "DBH", "Prod", "L/D", "Add/Del", "Prim Vol", "Secd Vol", "TotHT"));
                            write.WriteLine("_______________________________________________________________________________________");
                            sText = "List of trees for additional volume(CF):";
                            document.Add(new Paragraph(sText, Courier10));
                            sText = "_______________________________________________________________________________________";
                            document.Add(new Paragraph(sText, Courier10));
                            sText = string.Format("{0,-7} | {1,-7} | {2,-7} | {3,4} | {4,4} | {5,3} | {6,7} | {7,8} | {8,8} | {9,5}", "PayUnit", "CutUnit", "Species", "DBH", "Prod", "L/D", "Add/Del", "Prim Vol", "Secd Vol", "TotHT");
                            document.Add(new Paragraph(sText, Courier10));
                            sText = "_______________________________________________________________________________________";
                            document.Add(new Paragraph(sText, Courier10));
                            do
                            {
                                sPayUnit = AddRmvVol.GetString(0);
                                sCutUnit = AddRmvVol.GetString(1);
                                sSpec = AddRmvVol.GetString(2);
                                sDBH = AddRmvVol.GetString(3);
                                sProd = AddRmvVol.GetString(4);
                                sLD = AddRmvVol.GetString(5);
                                sAddDel = AddRmvVol.GetString(6);
                                sCU = AddRmvVol.GetString(7);
                                sVol2 = AddRmvVol.GetString(8);
                                if (sVol2 == "0") sVol2 = null;
                                sHT = AddRmvVol.GetString(9);
                                write.WriteLine(string.Format("{0,-7} | {1,-7} | {2,-7} | {3,4} | {4,4} | {5,3} | {6,7} | {7,8} | {8,8} | {9,5}", sPayUnit, sCutUnit, sSpec, sDBH, sProd, sLD, sAddDel, sCU, sVol2, sHT));
                                sText = string.Format("{0,-7} | {1,-7} | {2,-7} | {3,4} | {4,4} | {5,3} | {6,7} | {7,8} | {8,8} | {9,5}", sPayUnit, sCutUnit, sSpec, sDBH, sProd, sLD, sAddDel, sCU, sVol2, sHT);
                                document.Add(new Paragraph(sText, Courier10));
                            } while (AddRmvVol.MoveToNext());
                            write.WriteLine("_______________________________________________________________________________________");
                            sText = "_______________________________________________________________________________________";
                            document.Add(new Paragraph(sText, Courier10));
                        }
                    }
                    //add signature line
                    write.WriteLine("");
                    write.WriteLine(string.Format("{0,20}", "Signatures:"));
                    write.WriteLine("______________________________________________________________________________________");
                    write.WriteLine(string.Format("{0,30}|{1,10}|{2,30}|{3,10}", "", "", "", ""));
                    write.WriteLine(string.Format("{0,30}|{1,10}|{2,30}|{3,10}", "", "", "", ""));
                    write.WriteLine("______________________________________________________________________________________");
                    write.WriteLine(string.Format("{0,-30} {1,-10} {2,-30} {3,-10}", "        Sale Administrator", "Date", "Timber Sale Accounting", "Date"));
                    write.WriteLine("______________________________________________________________________________________");
                    sText = " ";
                    document.Add(new Paragraph(sText, Courier10));
                    sText = string.Format("{0,20}", "Signatures:");
                    document.Add(new Paragraph(sText, Courier10));
                    sText = "______________________________________________________________________________________";
                    document.Add(new Paragraph(sText, Courier10));
                    sText = string.Format("{0,30}|{1,10}|{2,30}|{3,10}", "", "", "", "");
                    document.Add(new Paragraph(sText, Courier10));
                    sText = string.Format("{0,30}|{1,10}|{2,30}|{3,10}", "", "", "", "");
                    document.Add(new Paragraph(sText, Courier10));
                    sText = "______________________________________________________________________________________";
                    document.Add(new Paragraph(sText, Courier10));
                    sText = string.Format("{0,-30} {1,-10} {2,-30} {3,-10}", "        Sale Administrator", "Date", "Timber Sale Accounting", "Date");
                    document.Add(new Paragraph(sText, Courier10));
                    sText = "______________________________________________________________________________________";
                    document.Add(new Paragraph(sText, Courier10));
                }
                // Close the document  
                document.Close();
                // Close the writer instance  
                writer.Close();
                // Always close open filehandles explicity
                fs.Close();

                Toast.MakeText(this, "The Summary report has been saved in " + txtFile, ToastLength.Long).Show();
                myAddonDB.SetReportDateOnAddTree(sInspectionDate);
                file2display = txtFile;
            }
        }
        private async void CreateSumVolReportPDF()
        {
            //if(!string.IsNullOrEmpty(sInspectionDate))
            //{
            //    sToday = sInspectionDate;
            //}
            string sFileExt;
            string sFileName = addonFile.Replace(".addvol", "_" + sToday);
            double total = 0;
            double totValue = 0;
            double fRate, fValue, fVol;
            double unitTotVol, unitTotValue;
            string sText;
            Font Courier10 = FontFactory.GetFont("Courier", 10);
            Font Courier12 = FontFactory.GetFont("Courier", 12);
            Font Helvetica12 = FontFactory.GetFont("Helvetica", 12);
            string pdfFile;
            if (bReportAll) sFileExt = "_All.pdf";
            else sFileExt = ".pdf";

            var txtFile = sFileName + sFileExt;
            int i = 1;
            while (System.IO.File.Exists(txtFile))
            {
                if (txtFile.Contains("(" + i + ")"))
                {
                    txtFile = txtFile.Replace("(" + i + ")", "");
                    i += 1;
                }
                txtFile = txtFile.Replace(".pdf", "(" + i + ")" + ".pdf");
            }

            string sTxtFileNameOnly = txtFile.Substring(txtFile.LastIndexOf('/') + 1);
            SimpleFileDialog fileDialog = new SimpleFileDialog(this, SimpleFileDialog.FileSelectionMode.SavePDF);
            fileDialog.DefaultFileName = sTxtFileNameOnly;
            txtFile = await fileDialog.GetFileOrDirectoryAsync(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/Documents");
            pdfFile = txtFile;
            if(string.IsNullOrEmpty(pdfFile)||pdfFile.Substring(pdfFile.Length-1)=="/")
            {
                Toast.MakeText(this, "You did not select a file name for the report...", ToastLength.Long).Show();
                return;
            }
            if (!pdfFile.ToLower().Contains(".pdf")) pdfFile = pdfFile + ".pdf";
            if (System.IO.File.Exists(txtFile)) System.IO.File.Delete(pdfFile);

            if (!string.IsNullOrEmpty(pdfFile) && !System.IO.File.Exists(pdfFile))
            {
                FileStream fs = new FileStream(pdfFile, FileMode.Create);
                //Document document = new Document(PageSize.A4, 25, 25, 30, 30);
                //add header and footer
                Document document = new Document(PageSize.A4, 25, 25, 100, 50);
                PdfWriter writer = PdfWriter.GetInstance(document, fs);
                //testing header and footer
                writer.PageEvent = new ITextEvents();
                // Open the document to enable you to write to the document  
                document.Open();

                //sText = string.Format("{0,50}", "Report of Additional Volume");
                //document.Add(new Paragraph(sText, Courier12));
                //sText = string.Format("{0,48}", "Tree Measurement Sales");
                //document.Add(new Paragraph(sText, Courier12));
                sText = "Report of Additional Volume";
                Paragraph title1 = new Paragraph(sText, Helvetica12);
                title1.Alignment = Element.ALIGN_CENTER;
                //document.Add(title1);
                sText = "Tree Measurement Sales";
                title1 = new Paragraph(sText, Helvetica12);
                title1.Alignment = Element.ALIGN_CENTER;
                //document.Add(title1);
                sText = " ";
                //document.Add(new Paragraph(sText, Courier10));
                sText = string.Format("{0,20} {1,-20} {2,22} {3,-20}", "Sale Name:", txtSaleName.Text, "Forest:", sForest);
                document.Add(new Paragraph(sText, Courier10));
                sText = string.Format("{0,20} {1,-20} {2,22} {3,-20}", "Contract Number:", txtContractNum.Text, "Ranger District:", sDist);
                document.Add(new Paragraph(sText, Courier10));

                string sRptDate, sPayUnit, sSpec, sDBH, sProd, sCU, sVol2, sCutUnit, sLD, sAddDel, sHT, sTrCnt;
                string sWhere, sOrder, sAddRemoveText;
                string sRptMonthWhere = " ";
                sOrder = "";
                Android.Database.ICursor PayUnitCur, AddRemoveCur, ReportDateCur, SumCur;
                myAddonDB.PopulateSummaryTemp(bReportAll,sInspectionDate);
                string sMinDate, sMaxDate, sMonthYr;
                bool bReportByMonth = false;
                sMinDate = myAddonDB.getValFromDB("Min(ReportDate)", "SummaryTemp", null);
                sMaxDate = myAddonDB.getValFromDB("Max(ReportDate)", "SummaryTemp", null);
                if (sMinDate.Substring(0, 7) == sMaxDate.Substring(0, 7))
                {
                    sMonthYr = sMinDate.Substring(5, 2) + "/" + sMinDate.Substring(0, 4);
                }
                else
                {
                    sMonthYr = sMinDate.Substring(5, 2) + "/" + sMinDate.Substring(0, 4) + " - " + sMaxDate.Substring(5, 2) + "/" + sMaxDate.Substring(0, 4);
                    bReportByMonth = true;
                }
                sText = string.Format("{0,20} {1,-20} {2,22} {3,-20}", "Reporting Month/Yr:", sMonthYr, "Purchaser/Contractor:", txtContractor.Text);
                document.Add(new Paragraph(sText, Courier10));
                sText = " ";
                document.Add(new Paragraph(sText, Courier10));

                //get report month and report summary by month (2022/02/22)
                Android.Database.ICursor ReportMonthCur;
                string sReportMonth = null;
                ReportMonthCur = myAddonDB.lvQueryCruiseDB("strftime('%Y-%m',ReportDate)", "SummaryTemp");
                if (ReportMonthCur != null)
                {
                    if (ReportMonthCur.MoveToFirst())
                    {
                        do
                        {
                            if (bReportByMonth)
                            {
                                sReportMonth = ReportMonthCur.GetString(0);
                                sText = "Report Month: " + ReportMonthCur.GetString(0);
                                document.Add(new Paragraph(sText, Courier10));
                                //sText = " ";
                                sText = "________________________________________________________________________________________";
                                document.Add(new Paragraph(sText, Courier10));
                                sRptMonthWhere = " strftime('%Y-%m',ReportDate) = '" + ReportMonthCur.GetString(0) + "' AND ";
                            }

                            //PayUnitCur = myAddonDB.lvQueryCruiseDB("PaymentUnit", "SummaryTemp");
                            PayUnitCur = myAddonDB.lvQueryCruiseDB("PaymentUnit", "SummaryTemp", sRptMonthWhere, sOrder);
                            if (PayUnitCur != null)
                            {
                                if (PayUnitCur.MoveToFirst())
                                {
                                    do
                                    {

                                        sPayUnit = PayUnitCur.GetString(0);
                                        sText = "Payment Unit: " + PayUnitCur.GetString(0);
                                        document.Add(new Paragraph(sText, Courier10));
                                        sText = "===================";
                                        document.Add(new Paragraph(sText, Courier10));

                                        sWhere = sRptMonthWhere + "PaymentUnit = '" + PayUnitCur.GetString(0) + "' AND ";
                                        sOrder = "";
                                        AddRemoveCur = myAddonDB.lvQueryCruiseDB("AddRemove", "SummaryTemp", sWhere, sOrder);
                                        if (AddRemoveCur != null)
                                        {
                                            if (AddRemoveCur.MoveToFirst())
                                            {
                                                do
                                                {
                                                    sAddDel = AddRemoveCur.GetString(0);
                                                    sAddRemoveText = "Volume added (+) to the contract";
                                                    if (AddRemoveCur.GetString(0) == "-") sAddRemoveText = "Volume removed (-) from the contract";
                                                    sText = sAddRemoveText;
                                                    document.Add(new Paragraph(sText, Courier10));
                                                    string sWhere2 = sWhere + "AddRemove = '" + AddRemoveCur.GetString(0) + "' AND ";
                                                    string sOrder2 = "DESC";
                                                    int count = 0;
                                                    unitTotValue = 0;
                                                    string UnitTotvalDisp;
                                                    unitTotVol = 0;
                                                    ReportDateCur = myAddonDB.lvQueryCruiseDB("ReportDate", "SummaryTemp", sWhere2, sOrder2);
                                                    if (ReportDateCur != null)
                                                    {
                                                        if (ReportDateCur.MoveToFirst())
                                                        {
                                                            do
                                                            {
                                                                count += 1;
                                                                sRptDate = ReportDateCur.GetString(0);
                                                                sText = "Inspection Report Date: " + ReportDateCur.GetString(0);
                                                                document.Add(new Paragraph(sText, Courier10));

                                                                total = 0;
                                                                totValue = 0;
                                                                fValue = 0;
                                                                SumCur = myAddonDB.lvQuerySummaryVol(sPayUnit, sRptDate, sAddDel);
                                                                if (SumCur != null)
                                                                {
                                                                    if (SumCur.MoveToFirst())
                                                                    {
                                                                        sText = "_______________________________________________________________________________";
                                                                        document.Add(new Paragraph(sText, Courier10));
                                                                        sText = string.Format("{0,-8} | {1,-8} | {2,-16} | {3,12} | {4,12} | {5,8}", "Species", "Product", "ContractSpecies", "Volume(CCF)", "Rate($/CCF)", "Value($)");
                                                                        document.Add(new Paragraph(sText, Courier10));
                                                                        sText = "_______________________________________________________________________________";
                                                                        document.Add(new Paragraph(sText, Courier10));

                                                                        do
                                                                        {
                                                                            sSpec = SumCur.GetString(0);
                                                                            sProd = SumCur.GetString(1);
                                                                            sCU = SumCur.GetString(2);
                                                                            string sWhere3 = " WHERE Species = '" + sSpec + "' AND Prod = '" + sProd + "'";
                                                                            string sRate = myAddonDB.getValFromDB("Price", "SalePrice", sWhere3);
                                                                            string sContractSpec = "";
                                                                            if (string.IsNullOrEmpty(sRate))
                                                                            {
                                                                                //check ContractSpecies
                                                                                string sWhere4 = " WHERE Species = '" + sSpec + "' AND PrimaryProduct = '" + sProd + "'";
                                                                                sContractSpec = myCruiseDB.getValFromDB("ContractSpecies", "TreeDefaultValue", sWhere4);
                                                                                if (!string.IsNullOrEmpty(sContractSpec))
                                                                                {
                                                                                    sWhere3 = " WHERE Species = '" + sContractSpec + "' AND Prod = '" + sProd + "'";
                                                                                    sRate = myAddonDB.getValFromDB("Price", "SalePrice", sWhere3);
                                                                                }

                                                                            }
                                                                            if (string.IsNullOrEmpty(sContractSpec) || sContractSpec.Length < 1 || sContractSpec == " ") sContractSpec = sSpec;
                                                                            //float fRate, fValue,fVol;
                                                                            if (!string.IsNullOrEmpty(sCU)) fVol = double.Parse(sCU);
                                                                            else fVol = 0;
                                                                            string sValue = "";
                                                                            if (!string.IsNullOrEmpty(sRate))
                                                                            {
                                                                                fRate = double.Parse(sRate);
                                                                                fValue = fRate * fVol;
                                                                                fValue = Math.Round(fValue, 2);
                                                                                sValue = fValue.ToString();
                                                                                sRate = string.Format("{0:0.00}", fRate);
                                                                                sValue = string.Format("{0:0.00}", fValue);
                                                                            }
                                                                            else fValue = 0;
                                                                            sCU = string.Format("{0:0.000}", fVol);
                                                                            sText = string.Format("{0,-8} | {1,-8} | {2,-16} | {3,12} | {4,12} | {5,8}", sSpec, sProd, sContractSpec, sCU, sRate, sValue);
                                                                            document.Add(new Paragraph(sText, Courier10));
                                                                            total += fVol;
                                                                            totValue += fValue;
                                                                        } while (SumCur.MoveToNext());
                                                                        totValue = Math.Round(totValue, 2);
                                                                        string TotalValDisp;
                                                                        if (totValue > 0) TotalValDisp = totValue.ToString();
                                                                        else TotalValDisp = "";
                                                                        total = Math.Round(total, 3);
                                                                        sText = "_______________________________________________________________________________";
                                                                        document.Add(new Paragraph(sText, Courier10));
                                                                        sText = string.Format("{0,39} {1,13} {2,25}", "Total:", total.ToString(), TotalValDisp);
                                                                        document.Add(new Paragraph(sText, Courier10));
                                                                        sText = " ";
                                                                        document.Add(new Paragraph(sText, Courier10));

                                                                        unitTotVol += total;
                                                                        unitTotValue += totValue;
                                                                    }
                                                                }
                                                            } while (ReportDateCur.MoveToNext());

                                                            if (count > 1)
                                                            {
                                                                unitTotValue = Math.Round(unitTotValue, 2);
                                                                if (unitTotValue > 0) UnitTotvalDisp = unitTotValue.ToString();
                                                                else UnitTotvalDisp = "";
                                                                unitTotVol = Math.Round(unitTotVol, 3);
                                                                sText = string.Format("{0,39} {1,13} {2,25}", "Unit " + sPayUnit + " Total:", unitTotVol.ToString(), UnitTotvalDisp);
                                                                document.Add(new Paragraph(sText, Courier10));
                                                                sText = " ";
                                                                document.Add(new Paragraph(sText, Courier10));

                                                            }
                                                        }
                                                    }
                                                } while (AddRemoveCur.MoveToNext());
                                            }
                                        }
                                    } while (PayUnitCur.MoveToNext());
                                }
                            }
                            //end PayUnitCur

                            Android.Database.ICursor AddRmvVol;

                            sText = " ";
                            document.Add(new Paragraph(sText, Courier10));

                            //now add list of all trees
                            AddRmvVol = myAddonDB.lvQueryAddTree(bReportAll, sReportMonth);
                            if (AddRmvVol != null)
                            {
                                if (AddRmvVol.MoveToFirst())
                                {
                                    sText = "List of trees for additional volume(CCF):";
                                    document.Add(new Paragraph(sText, Courier10));
                                    sText = "________________________________________________________________________________________";
                                    document.Add(new Paragraph(sText, Courier10));
                                    sText = string.Format("{0,-7} | {1,-7} | {2,-7} | {3,4} | {4,4} | {5,3} | {6,3} | {7,7} | {8,6} | {9,5} | {10,5}", "PayUnit", "CutUnit", "Species", "DBH", "Prod", "L/D", "+/-", "PrimVol", "2ndVol", "TotHT", "TrCnt");
                                    document.Add(new Paragraph(sText, Courier10));
                                    sText = "________________________________________________________________________________________";
                                    document.Add(new Paragraph(sText, Courier10));
                                    do
                                    {
                                        sPayUnit = AddRmvVol.GetString(0);
                                        sCutUnit = AddRmvVol.GetString(1);
                                        sSpec = AddRmvVol.GetString(2);
                                        //sDBH = AddRmvVol.GetString(3);
                                        sDBH = string.Format("{0:0.0}", float.Parse(AddRmvVol.GetString(3)));
                                        sProd = AddRmvVol.GetString(4);
                                        sLD = AddRmvVol.GetString(5);
                                        sAddDel = AddRmvVol.GetString(6);
                                        sCU = AddRmvVol.GetString(7);
                                        if (!String.IsNullOrEmpty(sCU)) sCU = string.Format("{0:0.000}", float.Parse(AddRmvVol.GetString(7)));
                                        else sCU = null;
                                        sVol2 = AddRmvVol.GetString(8);
                                        if (!String.IsNullOrEmpty(sVol2)) sVol2 = string.Format("{0:0.000}", float.Parse(AddRmvVol.GetString(8)));
                                        if (sVol2 == "0") sVol2 = null;
                                        //else sVol2 = string.Format("{0:0.000}", float.Parse(AddRmvVol.GetString(8)));
                                        sHT = AddRmvVol.GetString(9);
                                        sTrCnt = AddRmvVol.GetString(10);
                                        sText = string.Format("{0,-7} | {1,-7} | {2,-7} | {3,4} | {4,4} | {5,3} | {6,3} | {7,7} | {8,6} | {9,5} | {10,5}", sPayUnit, sCutUnit, sSpec, sDBH, sProd, sLD, sAddDel, sCU, sVol2, sHT, sTrCnt);
                                        document.Add(new Paragraph(sText, Courier10));
                                    } while (AddRmvVol.MoveToNext());
                                    sText = "________________________________________________________________________________________";
                                    document.Add(new Paragraph(sText, Courier10));
                                }
                            }
                            //add total tree count
                            int totalTreeCnt = myAddonDB.SumTreeCount(bReportAll, sReportMonth);
                            sText = string.Format("{0,80} | {1,5}", "Total Count:", totalTreeCnt);
                            document.Add(new Paragraph(sText, Courier10));
                        } while (ReportMonthCur.MoveToNext());
                        //end do loop
                    }
                    //end ReportMonthCur.MoveToFirst
                }
                //end ReportMonthCur

                //add signature line
                sText = " ";
                document.Add(new Paragraph(sText, Courier10));
                sText = string.Format("{0,20}", "Signatures:");
                document.Add(new Paragraph(sText, Courier12));
                sText = "______________________________________________________________________________________";
                document.Add(new Paragraph(sText, Courier10));
                sText = string.Format("{0,30}|{1,10}|{2,30}|{3,10}", "", "", "", "");
                document.Add(new Paragraph(sText, Courier10));
                //sText = string.Format("{0,30}|{1,10}|{2,30}|{3,10}", "", "", "", "");
                document.Add(new Paragraph(sText, Courier10));
                sText = "______________________________________________________________________________________";
                document.Add(new Paragraph(sText, Courier10));
                sText = string.Format("{0,-30} {1,-10} {2,-30} {3,-10}", "        Sale Administrator", "Date", "Timber Sale Accounting", "Date");
                document.Add(new Paragraph(sText, Courier10));
                //sText = "______________________________________________________________________________________";
                //document.Add(new Paragraph(sText, Courier10));

                // Close the document  
                document.Close();
                // Close the writer instance  
                writer.Close();
                // Always close open filehandles explicity
                fs.Close();

                Toast.MakeText(this, "The Summary report has been saved in " + pdfFile, ToastLength.Long).Show();
                myAddonDB.SetReportDateOnAddTree(sInspectionDate);
                file2display = pdfFile;
            }
        }
        //close database ondestroy
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if(!string.IsNullOrEmpty(cruiseFile))
            {
                myCruiseDB.CloseDB();
                if (System.IO.File.Exists(cruiseFile + "-journal"))
                {
                    System.IO.File.Delete(cruiseFile + "-journal");
                }
            }
            if (!string.IsNullOrEmpty(addonFile))
            {
                myAddonDB.UpdateSaleInfo(txtContractNum.Text, txtContractor.Text, txtUserInit.Text);
                myAddonDB.CloseDB();
                if (System.IO.File.Exists(addonFile + "-journal"))
                {
                    System.IO.File.Delete(addonFile + "-journal");
                }
            }
        }

    }
}