using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android;
using Android.Views.InputMethods;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace AddonTree_Volume
{
    [Activity(Label = "Enter Tree")]
    public class EnterTreeActivity : Activity
    {
        List<string> SpecList = new List<string>() { "Select a species" };
        List<string> LiveDeadList = new List<string>() { "L", "D" };
        List<string> ProdList = new List<string>() { "01", "02" };
        List<string> Prod2List = new List<string>() { "Select a prod" };
        List<string> DBHclassList = new List<string>() { "DBH Class" };
        string sCruiseFile, sAddvolFile, sId, sAddRemove, sTHT, sTrCnt;
        string sSpecies, sLD, sProd, sDBH, sUserInit, sCutUnit, sSaleName, sSaleNum, sPayUnit;
        Spinner spnSpecies, spnLD, spnProd, spnProd2;
        EditText txtDBH, txtSpecies, txtTotalHT, txtProd;
        TextView tvSaleName, tvCutUnit, tvSaleNum, tvPayUnit, tvTreCnt;
        Button buttonNext, buttonExit, buttonDelete, buttonMinus, buttonPlus, btnDBHclass, btnSaveTallyTree;
        GridView lvTallySumList;
        MyDatabase myCruiseDB, myAddvolDB;
        string sToday = DateTime.Today.ToString("yyyy-MM-dd");
        float fCUFT, fPrimVol, fSecnVol; // fSawVol, fPulpVol;
        //Boolean MoreSpList = false;
        Boolean needTHT = false;
        LinearLayout llTotalHT, llProd2, llTallySum, llTallyTree;
        RadioButton rbAdd, rbRemove;
        string sRegion, sForest, sDist, sTallySpecies, sTallyProduct;
        int Counter = 1;

        [DllImport("vollib", EntryPoint = "vernum_")]
        static extern void VollibVersion(ref int ver);
        [DllImport("vollib", EntryPoint = "vollibfia2_")]
        static extern void CalcVolFromVollib(ref int regn, ref int forst, string voleq, ref float dbh, ref float httot, ref float totcu, ref float sawcu, ref float mcu4, ref float scrbbf, ref float intlbf, ref int errflg);
        [DllImport("vollib", EntryPoint = "getvoleq3_")]
        static extern void VolEqDef(ref int a, StringBuilder forstc, StringBuilder distc, ref int spec, StringBuilder prod, StringBuilder voleq, ref int err, int l1, int l2, int l3, int l4);
        [DllImport("vollib", EntryPoint = "vollibfia3_")]
        static extern void GetVolFromVollib(ref int regn, ref int forst, string voleq, ref float dbh, ref float httot, ref float mtopp, ref float mtops, string prod, ref float totcu, ref float sawcu, ref float mcu4, ref float scrbbf, ref float intlbf, ref int errflg);

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.EnterTree);
            sCruiseFile = Intent.GetStringExtra("cruiseFile") ?? string.Empty;
            if(!string.IsNullOrEmpty(sCruiseFile))
            { myCruiseDB = new MyDatabase(sCruiseFile); }
            sAddvolFile = Intent.GetStringExtra("addvolFile") ?? string.Empty;
            if(!string.IsNullOrEmpty(sAddvolFile))
            { myAddvolDB = new MyDatabase(sAddvolFile); }

            sSaleName = Intent.GetStringExtra("SaleName") ?? string.Empty;
            sSaleNum = Intent.GetStringExtra("SaleNum") ?? string.Empty;
            sCutUnit = Intent.GetStringExtra("CutUnit") ?? string.Empty;
            sUserInit = Intent.GetStringExtra("UserInit") ?? string.Empty;
            sPayUnit = Intent.GetStringExtra("PayUnit") ?? string.Empty;
            sId = Intent.GetStringExtra("ID") ?? string.Empty;
            sSpecies = Intent.GetStringExtra("Species") ?? string.Empty;
            sDBH = Intent.GetStringExtra("DBH") ?? string.Empty;
            sLD = Intent.GetStringExtra("LD") ?? string.Empty;
            sProd = Intent.GetStringExtra("Prod") ?? string.Empty;
            sAddRemove = Intent.GetStringExtra("AddRemove") ?? string.Empty;
            sTHT = Intent.GetStringExtra("THT") ?? string.Empty;
            sRegion = Intent.GetStringExtra("Region") ?? string.Empty;
            sForest = Intent.GetStringExtra("Forest") ?? string.Empty;
            sDist = Intent.GetStringExtra("District") ?? string.Empty;
            sTrCnt = Intent.GetStringExtra("TreeCount") ?? string.Empty;

            spnSpecies = FindViewById<Spinner>(Resource.Id.spinnerSpec);
            spnLD = FindViewById<Spinner>(Resource.Id.spinnerLD);
            spnProd = FindViewById<Spinner>(Resource.Id.spinnerProd);
            spnProd2 = FindViewById<Spinner>(Resource.Id.spinnerProd2);
            txtDBH = FindViewById<EditText>(Resource.Id.editTextDBH);
            btnDBHclass = FindViewById<Button>(Resource.Id.btnDBHclassTally);
            tvSaleName = FindViewById<TextView>(Resource.Id.tvSaleName);
            tvCutUnit = FindViewById<TextView>(Resource.Id.tvCutUnit);
            tvSaleNum = FindViewById<TextView>(Resource.Id.textSaleNum);
            tvPayUnit = FindViewById<TextView>(Resource.Id.textPayUnit);
            txtSpecies = FindViewById<EditText>(Resource.Id.editTextSpec);
            txtProd = FindViewById<EditText>(Resource.Id.editTextProd);
            txtTotalHT = FindViewById<EditText>(Resource.Id.editTextTotalHT);
            llTotalHT = FindViewById<LinearLayout>(Resource.Id.linearLayoutTHT);
            llProd2 = FindViewById<LinearLayout>(Resource.Id.linearLayoutProd2);
            llTallySum = FindViewById<LinearLayout>(Resource.Id.llTallySumHeader);
            llTallyTree = FindViewById<LinearLayout>(Resource.Id.llTallyTreeHeader);
            rbAdd = FindViewById<RadioButton>(Resource.Id.radioAdd);
            rbRemove = FindViewById<RadioButton>(Resource.Id.radioRemove);
            tvTreCnt = FindViewById<TextView>(Resource.Id.tvTreCnt);

            tvTreCnt.Text = "1";
            tvSaleName.Text = "SaleName: " + sSaleName;
            tvSaleNum.Text = "SaleNumber: " + sSaleNum;
            tvCutUnit.Text = "CuttingUnit: " + sCutUnit;
            tvPayUnit.Text = "PaymentUnit: " + sPayUnit;
            spnSpecies.Focusable = true;
            txtDBH.RequestFocus();

            txtDBH.AfterTextChanged += TxtDBH_AfterTextChanged;

            CreateCruiseSpList("Species", "Tree");
            CreateProdList("PrimaryProduct", "TreeDefaultValue");
            CreateDBHclassList();
            
            var adapterSpec = new ArrayAdapter<string>(this, Resource.Layout.spinner_layout, SpecList);
            spnSpecies.Adapter = adapterSpec;
            spnSpecies.ItemSelected += Spinner1_ItemSelected;
            var adapterLD = new ArrayAdapter<string>(this, Resource.Layout.spinner_layout, LiveDeadList);
            spnLD.Adapter = adapterLD;
            var adapterProd = new ArrayAdapter<string>(this, Resource.Layout.spinner_layout, ProdList);
            spnProd.Adapter = adapterProd;
            spnProd.ItemSelected += SpinnerProd_ItemSelected;
            var adapterProd2 = new ArrayAdapter<string>(this, Resource.Layout.spinner_layout, Prod2List);
            spnProd2.Adapter = adapterProd2;

            if (!string.IsNullOrEmpty(sId))
            {
                SetSpinnerSelection(spnSpecies, SpecList, sSpecies);
                SetSpinnerSelection(spnLD, LiveDeadList, sLD);
                SetSpinnerSelection(spnProd, ProdList, sProd);
                txtDBH.Text = sDBH;
                tvTreCnt.Text = sTrCnt;
                if (sAddRemove == "-")  rbRemove.Checked = true;
                if (!string.IsNullOrEmpty(sTHT))
                {
                    llTotalHT.Visibility = Android.Views.ViewStates.Visible;
                    txtTotalHT.Text = sTHT;
                }
            }
            btnDBHclass.Click += btnDBHclass_Click;
            buttonMinus = FindViewById<Button>(Resource.Id.buttonMinus);
            buttonMinus.Click += ButtonMinus_Click;
            buttonPlus = FindViewById<Button>(Resource.Id.buttonPlus);
            buttonPlus.Click += ButtonPlus_Click;
            buttonNext = FindViewById<Button>(Resource.Id.buttonNext);
            buttonNext.Click += ButtonNext_Click;
            buttonExit = FindViewById<Button>(Resource.Id.buttonExit);
            buttonExit.Click += ButtonExit_Click;
            buttonDelete = FindViewById<Button>(Resource.Id.buttonDelete);
            if (!string.IsNullOrEmpty(sId))
            {
                buttonDelete.Visibility = Android.Views.ViewStates.Visible;
            }
            buttonDelete.Click += ButtonDelete_Click;
            btnSaveTallyTree= FindViewById<Button>(Resource.Id.btnSaveTallyTrees);
            btnSaveTallyTree.Click += ButtonSaveTallyTree_Click;

            //display regression info
            GetRegCursorView();
            ListView lvRegTemp = FindViewById<ListView>(Resource.Id.lvRegTemp);
            //display tally tree summary
            //llTallySum.Visibility = Android.Views.ViewStates.Visible;
            GetTallySumCursorView();
            lvTallySumList = FindViewById<GridView>(Resource.Id.lvTallySumList);
            lvTallySumList.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(TallySumList_ItemClick);
        }

        //the string formating not working!!!
        private void TxtDBH_AfterTextChanged(object sender, Android.Text.AfterTextChangedEventArgs e)
        {
            var text = e.Editable.ToString();
            txtDBH.AfterTextChanged -= TxtDBH_AfterTextChanged;
            var formatedText = DBHwithOneDecimal(text);
            txtDBH.Text = formatedText;
            txtDBH.SetSelection(formatedText.Length);
            txtDBH.AfterTextChanged += TxtDBH_AfterTextChanged;
        }
        private static string DBHwithOneDecimal(string text)
        {
            var numbers = Regex.Replace(text, @"\.?", "");
            if (numbers.Length == 0)
                return numbers;
            if (numbers.Length == 1)
                return string.Format(".{0}", numbers.ToString());
            if (numbers.Length == 2)
                return string.Format("{0}.{1}", numbers.Substring(0, 1), numbers.Substring(1));

            return string.Format("{0}.{1}", numbers.Substring(0, 2), numbers.Substring(2));
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

        private void ButtonNext_Click(object sender, EventArgs e)
        {
            string spec, prod, prod2;
            bool addNewSpecies = false;
            bool existSpeciesProd = true;
            prod2 = "";
            if (rbAdd.Checked) sAddRemove = "+";
            else if (rbRemove.Checked) sAddRemove = "-";
            
            if (spnSpecies.SelectedItem.ToString() == SpecList[0] && txtSpecies.Text.Length == 0)
            {
                Toast.MakeText(this, "Please select a species from the list...", ToastLength.Short).Show();
                spnSpecies.RequestFocus();
                //var v = (Vibrator)Android.App.Application.Context.GetSystemService(Android.App.Application.VibratorService);
                //v.Vibrate(100);
                vibrateDevice();
                return;
            }
            if (string.IsNullOrEmpty(txtDBH.Text))
            {
                Toast.MakeText(this, "Please enter a DBH...", ToastLength.Short).Show();
                txtDBH.RequestFocus();
                //var v = (Vibrator)Android.App.Application.Context.GetSystemService(Android.App.Application.VibratorService);
                //v.Vibrate(100);
                vibrateDevice();
                return;
            }
            if (spnProd.SelectedItem.ToString() == ProdList[0] && txtProd.Text.Length == 0)
            {
                Toast.MakeText(this, "Please select a prod from the list...", ToastLength.Short).Show();
                spnProd.RequestFocus();
                //var v = (Vibrator)Android.App.Application.Context.GetSystemService(Android.App.Application.VibratorService);
                //v.Vibrate(100);
                vibrateDevice();
                return;
            }

            if (txtSpecies.Text.Length > 0)
            {
                spec = txtSpecies.Text;
            }
            else
            {
                spec = spnSpecies.SelectedItem.ToString();
            }
            if (txtProd.Text.Length > 0)
            {
                prod = txtProd.Text;
            }
            else
            {
                prod = spnProd.SelectedItem.ToString();
            }
            //check height requirement
            if (string.IsNullOrEmpty(sCruiseFile))
            {
                //no cruise file attached, height is required
                if (txtTotalHT.Text.Length == 0)
                {
                    displayTotalHT(true);
                    return;
                }
                else
                {
                    float tht = float.Parse(txtTotalHT.Text);
                    if(tht<5.0 || tht > 300.0)
                    {
                        displayTotalHT(true);
                        return;
                    }
                }
            }
            else
            {
                //there is cruise file with it
                //check topwood prod code requirement
                string sWhere = " WHERE Species = '" + spec + "' AND PrimaryProduct = '" + prod + "' ";
                string strCalcTW = myCruiseDB.getValFromDB("CalcTopwood", "VolumeEquation", sWhere);
                if(strCalcTW=="1")
                {
                    //CreateProd2List(spec, prod);
                    if(myCruiseDB.HasMoreThanOneProd2(spec,prod))
                    {
                        if (spnProd2.SelectedItem.ToString() == Prod2List[0])
                        {
                            //display topwood prod field to select a prod code
                            displayProd2(true);
                            return;
                        }
                        else prod2 = spnProd2.SelectedItem.ToString();
                    }
                    else prod2 = Prod2List[1];
                }
                //first check if the Species, Prod and DBH has regression
                if (!myCruiseDB.LocalVolTableAvailable(spec, prod, txtDBH.Text))
                {
                    //if no regression or DBH out of range, Total height is required
                    if (txtTotalHT.Text.Length == 0)
                    {
                        displayTotalHT(true);
                        return;
                    }
                    else
                    {
                    //calculate volume using volume library and save tree record
                        float tht = float.Parse(txtTotalHT.Text);
                        if (tht < 5.0 || tht > 300.0)
                        {
                            displayTotalHT(true);
                            return;
                        }
                        else getVollibVol();
                    }
                }
                else
                {
                    //local volume table available, calculate volume with local regression equation
                    getCUFTfromLocalVolTable();
                }
            }
                
            if (txtSpecies.Text.ToString().Length > 0)
            {
                string newSpec = txtSpecies.Text.ToString();
                SpecList.Remove("Custom");
                SpecList.Add(txtSpecies.Text.ToString());
                SpecList.Add("Custom");
                txtSpecies.Text = "";
                ShowTxtSpec(false);
                var adapterSpec = new ArrayAdapter<string>(this, Resource.Layout.spinner_layout, SpecList);
                spnSpecies.Adapter = adapterSpec;
                SetSpinnerSelection(spnSpecies, SpecList, newSpec);
                addNewSpecies = true;
            }
            if (txtProd.Text.ToString().Length > 0)
            {
                string newProd = txtProd.Text.ToString();
                ProdList.Remove("New");
                ProdList.Add(txtProd.Text.ToString());
                ProdList.Add("New");
                txtProd.Text = "";
                ShowTxtProd(false);
                var adapterProd = new ArrayAdapter<string>(this, Resource.Layout.spinner_layout, ProdList);
                spnProd.Adapter = adapterProd;
                SetSpinnerSelection(spnProd, ProdList, newProd);
            }
            //everything is fine, the save (insert/update) the resord
            fSecnVol = (float)Math.Round((double)fSecnVol, 3);
            if (string.IsNullOrEmpty(prod2) || prod2.Length > 2) prod2 = "02";
            if (string.IsNullOrEmpty(sId))
            {
                myAddvolDB.AddRecord(spec, float.Parse(txtDBH.Text), prod, spnLD.SelectedItem.ToString(), sPayUnit, sCutUnit, fPrimVol, fSecnVol, prod2, sUserInit, txtTotalHT.Text, sAddRemove, tvTreCnt.Text);
                clearInputFields();
            }
            else
            {
                //update record
                myAddvolDB.UpdateRecord(int.Parse(sId), spec, float.Parse(txtDBH.Text), prod, spnLD.SelectedItem.ToString(), fPrimVol, fSecnVol, prod2, sUserInit, sAddRemove, txtTotalHT.Text, tvTreCnt.Text);
                var inputManager = (Android.Views.InputMethods.InputMethodManager)GetSystemService(InputMethodService);
                inputManager.HideSoftInputFromWindow(buttonNext.WindowToken, Android.Views.InputMethods.HideSoftInputFlags.None);
                base.Finish();
            }
            //Insert the new species to TreeDefaultValue table for setup contract species
            existSpeciesProd = myCruiseDB.existTreeDefaultValue(spec, prod);
            if(addNewSpecies==true || existSpeciesProd == false)
            {
                string sLD = spnLD.SelectedItem.ToString();
                myCruiseDB.InsertTreeDefaultValue(spec, prod, sLD, sUserInit);
            }
            Toast.MakeText(this, "Tree saved.", ToastLength.Short).Show();
        }

        private void ButtonExit_Click(object sender, EventArgs e)
        {
            var inputManager = (Android.Views.InputMethods.InputMethodManager)GetSystemService(InputMethodService);
            inputManager.HideSoftInputFromWindow(buttonNext.WindowToken, Android.Views.InputMethods.HideSoftInputFlags.None);

            if(myAddvolDB.TallyTreeExist())
            {
                TallyTreeAlert();
            }
            else base.Finish();
        }

        private void ButtonDelete_Click(object sender, EventArgs e)
        {
            DeleteAlert();
        }
        //Save Tally Trees button
        private void ButtonSaveTallyTree_Click(object sender, EventArgs e)
        {
            SaveTallyTrees();
            //hide tally sum items
            llTallySum.Visibility = Android.Views.ViewStates.Gone;
            llTallyTree.Visibility = Android.Views.ViewStates.Gone;
            lvTallySumList.Visibility = Android.Views.ViewStates.Gone;
        }
        private void ButtonMinus_Click(object sender, EventArgs e)
        {
            Counter = int.Parse(tvTreCnt.Text);
            if(Counter>1)
            {
                Counter--;
                tvTreCnt.Text = Counter.ToString();
            }
        }
        private void ButtonPlus_Click(object sender, EventArgs e)
        {
            Counter = int.Parse(tvTreCnt.Text);
            Counter++;
            tvTreCnt.Text = Counter.ToString();
        }
        private void GetRegCursorView()
        {
            if (sCruiseFile.Length > 0)
            {
                Android.Database.ICursor icTemp2 = myCruiseDB.GetRegressionInfo();
                if (icTemp2 != null)
                {
                    icTemp2.MoveToFirst();
                    ListView lvRegTemp = FindViewById<ListView>(Resource.Id.lvRegTemp);
                    string[] from = new string[] { "rVolType", "rSpeices", "rLiveDead", "rProduct", "rMinDbh", "rMaxDbh", "RegressModel" };
                    int[] to = new int[] {
                    Resource.Id.tvrVolTypeShow,
                    Resource.Id.tvrSpeciesShow,
                    Resource.Id.tvrLiveDeadShow,
                    Resource.Id.tvrProdShow,
                    Resource.Id.tvrMinDBHShow,
                    Resource.Id.tvrMaxDBHShow,
                    Resource.Id.tvrModelShow
                    };
                    // creating a SimpleCursorAdapter to fill ListView object.
                    SimpleCursorAdapter scaTemp = new SimpleCursorAdapter(this, Resource.Layout.reg_record, icTemp2, from, to, 0);
                    lvRegTemp.Adapter = scaTemp;
                }
            }
        }
        //get tally tree summary and display 
        private void GetTallySumCursorView()
        {
            Android.Database.ICursor icTemp2 = myAddvolDB.GetTallyTrees();
            if (icTemp2 != null)
            {
                icTemp2.MoveToFirst();
                GridView lvTallySumTemp = FindViewById<GridView>(Resource.Id.lvTallySumList);
                string[] from = new string[] { "_id", "Species", "Product", "DBHclass", "TreeCount" };
                int[] to = new int[] {
                    Resource.Id.tvTallySumIdR,
                    Resource.Id.tvTallySumSpecR,
                    Resource.Id.tvTallySumProdR,
                    Resource.Id.tvTallySumDBHclassR,
                    Resource.Id.tvTallySumTreCntR
                    };
                // creating a SimpleCursorAdapter to fill ListView object.
                SimpleCursorAdapter scaTemp = new SimpleCursorAdapter(this, Resource.Layout.TallySumRecord, icTemp2, from, to, 0);
                lvTallySumTemp.Adapter = scaTemp;
            }
        }
        //spiner selection for species
        private void Spinner1_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            string s1 = SpecList[e.Position].ToString();
            if (s1 == "More")
            {
                CreateCruiseSpList("Species", "TreeDefaultValue");
                var adapterSpecMore = new ArrayAdapter<string>(this, Resource.Layout.spinner_layout, SpecList);
                spnSpecies.Adapter = adapterSpecMore;
                spnSpecies.PerformClick();
                //MoreSpList = true;
            }
            else if (s1 == "Custom")
            {
                ShowTxtSpec(true);
                spnSpecies.SetSelection(0);
                txtSpecies.RequestFocus();
            }
            //species selection changed, recreate prod2 list
            else
            {
                //get the regression primaryproduct for the selected species and set the value to the primary product selection
                //just for entering new record
                if (string.IsNullOrEmpty(sId))
                {
                    string regProd = "";
                    if (!string.IsNullOrEmpty(sCruiseFile))
                    {
                        string sWhere = " WHERE rVolume LIKE '%CUFT%' AND rVolType = 'Primary' AND rSpeices LIKE '%" + s1 + "%'";
                        regProd = myCruiseDB.getValFromDB("rProduct", "Regression", sWhere);
                    }
                    if (string.IsNullOrEmpty(regProd))
                    {
                        spnProd.SetSelection(1);
                    }
                    else
                    {
                        SetSpinnerSelection(spnProd, ProdList, regProd);
                        spnProd.SetSelection(1);
                    }
                }
                CreateProd2List(s1, spnProd.SelectedItem.ToString());
                var adapterProd2 = new ArrayAdapter<string>(this, Resource.Layout.spinner_layout, Prod2List);
                spnProd2.Adapter = adapterProd2;
            }
        }
        private void ShowTxtSpec(bool show)
        {
            LinearLayout.LayoutParams spnSpecParams = new LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.WrapContent, 0.0f);
            LinearLayout.LayoutParams txtSpecParams = new LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.WrapContent, 1.0f);
            if(show==true)
            {
                spnSpecies.LayoutParameters = spnSpecParams;
                spnSpecies.Visibility = Android.Views.ViewStates.Gone;
                txtSpecies.LayoutParameters = txtSpecParams;
                txtSpecies.Visibility = Android.Views.ViewStates.Visible;
            }
            else
            {
                spnSpecies.LayoutParameters = txtSpecParams;
                spnSpecies.Visibility = Android.Views.ViewStates.Visible;
                txtSpecies.LayoutParameters = spnSpecParams;
                txtSpecies.Visibility = Android.Views.ViewStates.Gone;
            }
        }
        //Create DBH class list
        private void CreateDBHclassList()
        {
            string MinDBH, MaxDBH;
            int iMinDBH, iMaxDBH;
            MinDBH = myCruiseDB.getValFromDB("Round(Min(DBH))", "Tree", "WHERE DBH > 0");
            MaxDBH = myCruiseDB.getValFromDB("Round(Max(DBH))", "Tree", "");
            iMinDBH = int.Parse(MinDBH);
            iMaxDBH = int.Parse(MaxDBH);
            for (int i = iMinDBH; i <= iMaxDBH; i++)
            {
                DBHclassList.Add(i.ToString());
            }
        }
        private void CreateCruiseSpList(string dbColumn, string dbTable)
        {
            SpecList = new List<string>() { "Select a species" };
            List<string> SpecListTemp = new List<string>() { };
            List<string> SpecListTemp2 = new List<string>() { };
            string spec;
            //comment out 2020/04/29
            //Android.Database.ICursor CruiseSp2 = myAddvolDB.lvQueryCruiseDB("Species", "SalePrice");
            //if (CruiseSp2 != null)
            //{
            //    if (CruiseSp2.MoveToFirst())
            //    {
            //        do
            //        {
            //            spec = CruiseSp2.GetString(CruiseSp2.GetColumnIndex("Species"));
            //            SpecListTemp.Add(spec);
            //        } while (CruiseSp2.MoveToNext());
            //    }
            //}
            Android.Database.ICursor CruiseSp3 = myAddvolDB.lvQueryCruiseDB("Species", "AddTree");
            if (CruiseSp3 != null)
            {
                if (CruiseSp3.MoveToFirst())
                {
                    do
                    {
                        spec = CruiseSp3.GetString(CruiseSp3.GetColumnIndex("Species"));
                        SpecListTemp.Add(spec);
                    } while (CruiseSp3.MoveToNext());
                }
            }
            if(!string.IsNullOrEmpty(sCruiseFile))
            {
                Android.Database.ICursor CruiseSp = myCruiseDB.lvQueryCruiseDB(dbColumn, dbTable);
                if (CruiseSp != null)
                {
                    if (CruiseSp.MoveToFirst())
                    {
                        do
                        {
                            spec = CruiseSp.GetString(CruiseSp.GetColumnIndex("Species"));
                            SpecListTemp.Add(spec);
                        } while (CruiseSp.MoveToNext());
                    }
                }
            }
            SpecListTemp.Sort();
            SpecListTemp2 = SpecListTemp.Distinct().ToList();
            SpecList.AddRange(SpecListTemp2);
            if (string.IsNullOrEmpty(sCruiseFile))
                SpecList.Add("Custom");
            else if (dbTable.ToUpper() == "TREE")
            { SpecList.Add("More"); }
            else if (dbTable == "TreeDefaultValue")
            { SpecList.Add("Custom"); }
        }
        private void CreateProdList(string dbColumn, string dbTable)
        {
            ProdList = new List<string>() { "Select a prod" };
            List<string> ProdListTemp = new List<string>() { };
            List<string> ProdListTemp2 = new List<string>() { };
            string prodcode;
            Android.Database.ICursor cProdList3 = myAddvolDB.lvQueryCruiseDB("Prod", "AddTree");
            if (cProdList3 != null)
            {
                if (cProdList3.MoveToFirst())
                {
                    do
                    {
                        //prodcode = cProdList.GetString(cProdList.GetColumnIndex("PrimaryProduct"));
                        prodcode = cProdList3.GetString(0);
                        ProdListTemp.Add(prodcode);
                    } while (cProdList3.MoveToNext());
                }
            }
            if (!string.IsNullOrEmpty(sCruiseFile))
            {
                Android.Database.ICursor cProdList = myCruiseDB.lvQueryCruiseDB(dbColumn, dbTable);
                if (cProdList != null)
                {
                    if (cProdList.MoveToFirst())
                    {
                        do
                        {
                            //prodcode = cProdList.GetString(cProdList.GetColumnIndex("PrimaryProduct"));
                            //ProdList.Add(prodcode);
                            prodcode = cProdList.GetString(0);
                            ProdListTemp.Add(prodcode);
                        } while (cProdList.MoveToNext());
                        //ProdList.Add("New");
                    }
                }
            }
                
            ProdListTemp.Sort();
            ProdListTemp2 = ProdListTemp.Distinct().ToList();
            ProdList.AddRange(ProdListTemp2);
            ProdList.Add("New");
        }
        private void CreateProd2List(string spec, string prod)
        {
            Prod2List = new List<string>() { "Select a Secondary Prod" };
            string prodcode;
            if (!string.IsNullOrEmpty(sCruiseFile))
            {
                Android.Database.ICursor cProdList = myCruiseDB.GetProd2List(spec, prod);
                if (cProdList != null)
                {
                    if (cProdList.MoveToFirst())
                    {
                        do
                        {
                            prodcode = cProdList.GetString(0);
                            Prod2List.Add(prodcode);
                        } while (cProdList.MoveToNext());
                    }
                    else Prod2List.Add("02");
                }
            }
        }
        //button DBHclass tally
        private void btnDBHclass_Click(object sender, EventArgs e)
        {
            if (!myCruiseDB.HasLocalVolTable)
            {
                Toast.MakeText(this, "This cruise file does not have local volume table. DBH class tally require local volume table!", ToastLength.Short).Show();
                VibrateDevice();

            }
            else
            {
                //if DBH class records created in TallyTree, the navigate to Tally screen
                if (!myAddvolDB.DBHclassExist())
                {
                    CreateTallyDBHclass();
                    //Toast.MakeText(this, "DBH class for tally created successfully.", ToastLength.Short).Show();
                    //VibrateDevice();
                }
                var intent = new Intent(this, typeof(TallyTreeActivity));
                intent.PutExtra("cruiseFile", sCruiseFile);
                intent.PutExtra("addvolFile", sAddvolFile);
                intent.PutExtra("SaleName", sSaleName);
                intent.PutExtra("SaleNum", sSaleNum);
                intent.PutExtra("CutUnit", sCutUnit);
                //StartActivity(intent);
                int requestCode = 1199;
                StartActivityForResult(intent, requestCode);
            }
        }
        //tally tree summary listview record click
        private void TallySumList_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            TextView tvTallySpecShow = e.View.FindViewById<TextView>(Resource.Id.tvTallySumSpecR);
            TextView tvTallyProdShow = e.View.FindViewById<TextView>(Resource.Id.tvTallySumProdR);
            sTallySpecies = tvTallySpecShow.Text;
            sTallyProduct = tvTallyProdShow.Text;
            var intent = new Intent(this, typeof(TallyTreeActivity));
            intent.PutExtra("cruiseFile", sCruiseFile);
            intent.PutExtra("addvolFile", sAddvolFile);
            intent.PutExtra("SaleName", sSaleName);
            intent.PutExtra("SaleNum", sSaleNum);
            intent.PutExtra("CutUnit", sCutUnit);
            intent.PutExtra("TallySpecies", sTallySpecies);
            intent.PutExtra("TallyProduct", sTallyProduct);
            //StartActivity(intent);
            int requestCode = 1199;
            StartActivityForResult(intent, requestCode);
        }
        public void VibrateDevice()
        {
            var v = (Vibrator)Android.App.Application.Context.GetSystemService(Android.App.Application.VibratorService);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                v.Vibrate(VibrationEffect.CreateOneShot(200, VibrationEffect.DefaultAmplitude));
            }
            else v.Vibrate(100);
        }
        private void SpinnerProd_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            string s1 = ProdList[e.Position].ToString();

            if (s1 == "New")
            {
                ShowTxtProd(true);
                spnProd.SetSelection(0);
                txtProd.RequestFocus();
            }
            else
            {
                CreateProd2List(spnSpecies.SelectedItem.ToString(), s1);
                var adapterProd2 = new ArrayAdapter<string>(this, Resource.Layout.spinner_layout, Prod2List);
                spnProd2.Adapter = adapterProd2;
            }
        }
        private void ShowTxtProd(bool show)
        {
            LinearLayout.LayoutParams spnProdParams = new LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.WrapContent, 0.0f);
            LinearLayout.LayoutParams txtProdParams = new LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.WrapContent, 1.0f);
            if(show==true)
            {
                spnProd.LayoutParameters = spnProdParams;
                spnProd.Visibility = Android.Views.ViewStates.Gone;
                txtProd.LayoutParameters = txtProdParams;
                txtProd.Visibility = Android.Views.ViewStates.Visible;
            }
            else
            {
                spnProd.LayoutParameters = txtProdParams;
                spnProd.Visibility = Android.Views.ViewStates.Visible;
                txtProd.LayoutParameters = spnProdParams;
                txtProd.Visibility = Android.Views.ViewStates.Gone;
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
        private void DeleteAlert()
        {
            Android.App.AlertDialog.Builder dialog = new AlertDialog.Builder(this);
            AlertDialog alert = dialog.Create();

            alert.SetTitle("Delete Tree Record");
            alert.SetMessage("Do you want to delete the tree Species = " + sSpecies + " DBH = " + sDBH + "? ");
            alert.SetIcon(Resource.Drawable.info);
            alert.SetButton("YES", (c, ev) =>
            {
                // Ok button click, then continue to delete record
                myAddvolDB.DeleteRecord(int.Parse(sId));
                base.Finish();
            });
            alert.SetButton2("NO", (c, ev) => {
                base.Finish(); 
            });
            alert.Show();
        }
        private void TallyTreeAlert()
        {
            Android.App.AlertDialog.Builder dialog = new AlertDialog.Builder(this);
            AlertDialog alert = dialog.Create();

            alert.SetTitle("Save Tally Tree Record");
            alert.SetMessage("The tally trees have not been saved. Do you want to save the tally trees? ");
            alert.SetIcon(Resource.Drawable.info);
            alert.SetButton("YES", (c, ev) =>
            {
                // Ok button click, then save tally trees
                SaveTallyTrees();
                myAddvolDB.SetTallyTreeCount0();
                base.Finish();
            });
            alert.SetButton2("NO", (c, ev) => {
                //no save and set tally record to 0
                myAddvolDB.SetTallyTreeCount0();
                base.Finish();
            });
            alert.Show();
        }
        private void SpeciesOrDBH_Alert(string speciesOrDBH)
        {
            Android.App.AlertDialog.Builder dialog = new AlertDialog.Builder(this);
            AlertDialog alert = dialog.Create();
            if (speciesOrDBH.Substring(0, 7) == "SPECIES")
            {
                alert.SetTitle("Regression species");
                if (txtTotalHT.Text.Length == 0) needTHT = true;
            }
            else if (speciesOrDBH.Substring(0, 3) == "DBH")
            {
                alert.SetTitle("DBH out of range");
                if (txtTotalHT.Text.Length == 0) needTHT = true;
                else needTHT = false;
            }
            else
                needTHT = false;

            alert.SetMessage(speciesOrDBH);
            alert.SetIcon(Resource.Drawable.info);
            alert.SetButton("OK", (c, ev) =>
            {
                // Ok button click, then continue to tree enter screen
                if(needTHT)
                {
                    llTotalHT.Visibility = Android.Views.ViewStates.Visible;
                    Toast.MakeText(this, "Please enter total height for the tree.", ToastLength.Short).Show();
                    txtTotalHT.RequestFocus();
                    var v = (Vibrator)Android.App.Application.Context.GetSystemService(Android.App.Application.VibratorService);
                    v.Vibrate(100);
                }
            });
            alert.Show();
        }
        //Add DBH class records to TallyTree table
        private void CreateTallyDBHclass()
        {
            string strSpec, strProd;
            int iMinD, iMaxD;
            if (!myAddvolDB.DBHclassExist())
            {
                if(myCruiseDB.LocalVolTableAvailable())
                {
                    //get species, prod and DNH class from regression table and insert record to TallyTree table
                    Android.Database.ICursor cRegList = myCruiseDB.GetRegSpecProdDBH();
                    if (cRegList != null)
                    {
                        if (cRegList.MoveToFirst())
                        {
                            do
                            {
                                strSpec = cRegList.GetString(0);
                                strProd = cRegList.GetString(1);
                                iMinD = int.Parse(cRegList.GetString(2));
                                iMaxD = int.Parse(cRegList.GetString(3));
                                string[] rspecList = strSpec.Split("/");
                                string[] rprodList = strProd.Split("/");
                                foreach (string spc in rspecList)
                                    foreach(string prd in rprodList)
                                        for (int i=iMinD; i<= iMaxD; i++)
                                        {
                                            myAddvolDB.AddDBHclassToTallyTree(spc, prd, i.ToString());
                                        }
                            } while (cRegList.MoveToNext());
                        }
                    }
                }
                else
                {
                    //display alert message for local volume table 
                    Toast.MakeText(this, "There is no Local Volume Table available. DBH class tally needs local volume table.", ToastLength.Short).Show();
                    //EnterTreeActivity.vibrateDevice();
                }
            }
        }
        private void SaveTallyTrees()
        {
            string voltype = "Primary";
            string spec, prod, lord, sDBH, sTrCnt;
            lord = "L";
            string sTotalHT = "";
            string prod2 = Prod2List[1];
            double dDBH;
            if (rbAdd.Checked) sAddRemove = "+";
            else sAddRemove = "-";
            if (myAddvolDB.TallyTreeExist())
            {
                //loop through tally trees and calculate volume
                Android.Database.ICursor cTallyTreeList = myAddvolDB.GetTallyTrees();
                if (cTallyTreeList != null)
                {
                    if (cTallyTreeList.MoveToFirst())
                    {
                        do
                        {
                            fPrimVol = 0.0f;
                            fSecnVol = 0.0f;
                            spec = cTallyTreeList.GetString(1);
                            prod = cTallyTreeList.GetString(2);
                            sDBH = cTallyTreeList.GetString(3);
                            dDBH = double.Parse(sDBH);
                            voltype = "Primary";
                            fPrimVol = (float)myCruiseDB.CalcVol(voltype, spec, dDBH, prod, lord);
                            string sWhere = " WHERE Species = '" + spec + "' AND PrimaryProduct = '" + prod + "' ";
                            string strCalcTW = myCruiseDB.getValFromDB("CalcTopwood", "VolumeEquation", sWhere);
                            if (strCalcTW == "1")
                            {
                                voltype = "Secondary";
                                fSecnVol = (float)myCruiseDB.CalcVol(voltype, spec, dDBH, prod, lord);
                                if (fSecnVol > 0.0)
                                {
                                    fSecnVol = fSecnVol - fPrimVol;
                                    if (fSecnVol < 0.0) fSecnVol = 0.0f;
                                }
                            }
                            //insert tree to AddTree table
                            sTrCnt = cTallyTreeList.GetString(4);
                            myAddvolDB.AddRecord(spec, float.Parse(sDBH), prod, lord, sPayUnit, sCutUnit, fPrimVol, fSecnVol, prod2, sUserInit, sTotalHT, sAddRemove, sTrCnt);
                        } while (cTallyTreeList.MoveToNext());
                    }
                }
                //reset treecount to 0 in TallyTree table
                myAddvolDB.SetTallyTreeCount0();
                Toast.MakeText(this, "Tally trees saved.", ToastLength.Short).Show();
            }
        }
        private void getCUFTfromLocalVolTable()
        {
            fCUFT = 0.0f;
            //fSawVol = 0.0f;
            //fPulpVol = 0.0f;
            fPrimVol = 0.0f;
            fSecnVol = 0.0f;
            string spec;
            string voltype = "Primary";
            //int regn = int.Parse(sRegion);
            if (txtSpecies.Visibility == ViewStates.Visible)
                spec = txtSpecies.Text;
            else
                spec = spnSpecies.SelectedItem.ToString();
            if (!string.IsNullOrEmpty(sCruiseFile))
            {
                if (myCruiseDB.HasLocalVolTable)
                {

                    fCUFT = (float)myCruiseDB.CalcVol(voltype, spec, double.Parse(txtDBH.Text), spnProd.SelectedItem.ToString(), spnLD.SelectedItem.ToString());
                    fPrimVol = fCUFT;
                    if (myCruiseDB.Message.Length > 0)
                    {
                        if (myCruiseDB.Message.Substring(0, 3) == "DBH" || myCruiseDB.Message.Substring(0, 7) == "SPECIES")
                        {
                            SpeciesOrDBH_Alert(myCruiseDB.Message);
                        }
                    }
                    string sWhere = " WHERE Species = '" + spec + "' AND PrimaryProduct = '" + spnProd.SelectedItem.ToString() + "' ";
                    string strCalcTW = myCruiseDB.getValFromDB("CalcTopwood", "VolumeEquation", sWhere);
                    if(strCalcTW == "1")
                    {
                        voltype = "Secondary";
                        fSecnVol = (float)myCruiseDB.CalcVol(voltype, spec, double.Parse(txtDBH.Text), spnProd.SelectedItem.ToString(), spnLD.SelectedItem.ToString());
                        if(fSecnVol > 0.0)
                        {
                            fSecnVol = fSecnVol - fPrimVol;
                            if (fSecnVol < 0.0) fSecnVol = 0.0f;
                        }
                    }
                }
                else
                {
                    if (txtTotalHT.Text.Length == 0)
                    {
                        needTHT = true;
                        llTotalHT.Visibility = Android.Views.ViewStates.Visible;
                        Toast.MakeText(this, "Please enter total height for the tree.", ToastLength.Short).Show();
                        txtTotalHT.RequestFocus();
                        var v = (Vibrator)Android.App.Application.Context.GetSystemService(Android.App.Application.VibratorService);
                        v.Vibrate(100);
                    }
                    else
                    {
                        needTHT = false;
                    }
                }
            }
        }
        //display TotalHT field
        private void displayTotalHT(bool show)
        {
            if (show == true)
            {
                llTotalHT.Visibility = Android.Views.ViewStates.Visible;
                //if(!string.IsNullOrEmpty(sCruiseFile))
                { 
                    Toast.MakeText(this, "Please enter total height (5 to 300) for the tree.", ToastLength.Short).Show();
                    txtTotalHT.RequestFocus();
                    var v = (Vibrator)Android.App.Application.Context.GetSystemService(Android.App.Application.VibratorService);
                    v.Vibrate(100);
                }
            }
            else llTotalHT.Visibility = Android.Views.ViewStates.Gone;
        }
        //display topwood prod selection field
        private void displayProd2(bool show)
        {
            if (show == true)
            {
                llProd2.Visibility = Android.Views.ViewStates.Visible;
                Toast.MakeText(this, "Please select a product code for the topwood", ToastLength.Short).Show();
                var v = (Vibrator)Android.App.Application.Context.GetSystemService(Android.App.Application.VibratorService);
                v.Vibrate(100);
                spnProd2.RequestFocus();
            }
            else
            {
                llProd2.Visibility = Android.Views.ViewStates.Gone;
                spnProd2.SetSelection(0);
            }
        }
        private void clearInputFields()
        {
            txtDBH.Text = txtProd.Text = txtSpecies.Text = txtTotalHT.Text = "";
            //spnProd.SetSelection(0);
            //spnSpecies.SetSelection(0);
            if (!string.IsNullOrEmpty(sCruiseFile))
            {
                displayTotalHT(false);
                displayProd2(false);
            }
            txtDBH.RequestFocus();
        }
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            //Toast.MakeText(this, "requestCode = " + requestCode, ToastLength.Short).Show();
            if (requestCode == 1199)
            {
                //check tally trees and displa the summary here
                GetTallySumCursorView();
                if (myAddvolDB.TallyTreeExist())
                {
                    //Toast.MakeText(this, "There are tally trees", ToastLength.Short).Show();
                    //make the items for tally summary visiable
                    llTallySum.Visibility = Android.Views.ViewStates.Visible;
                    llTallyTree.Visibility = Android.Views.ViewStates.Visible;
                    lvTallySumList.Visibility = Android.Views.ViewStates.Visible;
                    //GetTallySumCursorView();
                    lvTallySumList = FindViewById<GridView>(Resource.Id.lvTallySumList);
                }
                else
                {
                    llTallySum.Visibility = Android.Views.ViewStates.Gone;
                    llTallyTree.Visibility = Android.Views.ViewStates.Gone;
                    lvTallySumList.Visibility = Android.Views.ViewStates.Gone;
                }
            }
        }
        //call volume library
        private void getVollibVol()
        {
            string spec;
            float mtopp, mtops;
            fPrimVol = 0.0f;
            fSecnVol = 0.0f;
            if (!string.IsNullOrEmpty(sRegion))
            {
                int regn = int.Parse(sRegion);
                if (txtSpecies.Visibility == ViewStates.Visible)
                    spec = txtSpecies.Text;
                else
                    spec = spnSpecies.SelectedItem.ToString();

                //if volume is not calculated, then calculate the volume from volume library
                //if (fCUFT == 0)
                {
                    string sWhere = " WHERE Species = '" + spec + "' AND PrimaryProduct = '" + spnProd.SelectedItem.ToString() + "' ";
                    string voleq = myCruiseDB.getValFromDB("VolumeEquationNumber", "VolumeEquation", sWhere);
                    string strMtopp = myCruiseDB.getValFromDB("TopDIBPrimary", "VolumeEquation", sWhere);
                    string strMtops = myCruiseDB.getValFromDB("TopDIBSecondary", "VolumeEquation", sWhere);
                    string strCalcTW = myCruiseDB.getValFromDB("CalcTopwood", "VolumeEquation", sWhere);
                    if (string.IsNullOrEmpty(strMtopp)) mtopp = 0.0f;
                    else mtopp = float.Parse(strMtopp);
                    if (string.IsNullOrEmpty(strMtops)) mtops = 0.0f;
                    else mtops = float.Parse(strMtops);
                    string prod = spnProd.SelectedItem.ToString();
                    //if no equation exist in the VolumeEquation table for the species and prod, get default equation fom volume library
                    if (string.IsNullOrEmpty(voleq))
                    {
                        StringBuilder forstc = new StringBuilder(256);
                        StringBuilder distc = new StringBuilder(256);
                        StringBuilder prodc = new StringBuilder(256);
                        StringBuilder voleqc = new StringBuilder(256);
                        sWhere = " WHERE Species = '" + spec + "' AND FIAcode > 0";
                        string sFIAcode = myCruiseDB.getValFromDB("FIAcode", "TreeDefaultValue", sWhere);
                        if(string.IsNullOrEmpty(sFIAcode))
                        {
                            int Num;
                            bool isInt = int.TryParse(spec, out Num);
                            if (isInt) sFIAcode = spec;
                            else sFIAcode = "999";
                        }
                        if (!string.IsNullOrEmpty(sFIAcode))
                        {
                            int ispec = int.Parse(sFIAcode);
                            forstc.Append(sForest);
                            distc.Append(sDist);
                            prodc.Append(spnProd.SelectedItem.ToString());
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
                            Toast.MakeText(this, "There is no volume equation for " + spec, ToastLength.Long).Show();
                        }
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
                        float dbhob = float.Parse(txtDBH.Text);
                        float totht = float.Parse(txtTotalHT.Text);
                        //CalcVolFromVollib(ref regn, ref forst, voleq, ref dbhob, ref totht, ref totcu, ref sawcu, ref mcu4, ref scrbbf, ref intlbf, ref errflg);
                        GetVolFromVollib(ref regn, ref forst, voleq, ref dbhob, ref totht, ref mtopp, ref mtops, prod, ref totcu, ref sawcu, ref mcu4, ref scrbbf, ref intlbf, ref errflg);
                        if (errflg == 0)
                        {
                            fCUFT = sawcu;
                            fPrimVol = sawcu;
                            if (strCalcTW == "1")
                            { 
                                fSecnVol = mcu4 - sawcu;
                                if (fSecnVol < 0.0) fSecnVol = 0.0f;
                            }
                            //return fPrimVol and fSecnVol in CCF
                            fPrimVol = fPrimVol / 100.0f;
                            fSecnVol = fSecnVol / 100.0f;
                        }
                        else
                        {
                            string errmsg = "Volume library error: ";
                            if (errflg == 1) errmsg = "No volume equation match";
                            else if (errflg == 2) errmsg += "No form class";
                            else if (errflg == 3) errmsg += "DBH less than one";
                            else if (errflg == 4) errmsg += "Tree height less than 4.5";
                            else if (errflg == 5) errmsg += "D2H is out of bounds";
                            else if (errflg == 6) errmsg += "No species match";
                            else if (errflg == 7) errmsg += "Illegal primary product log height (Ht1prd)";
                            else if (errflg == 8) errmsg += "Illegal secondary product log height (Ht2prd)";
                            else if (errflg == 9) errmsg += "Upper stem measurements required";
                            else if (errflg == 10) errmsg += "Illegal upper stem height (UPSHT1)";
                            else if (errflg == 11) errmsg += "Unable to fit profile given dbh, merch ht and top dia";
                            else if (errflg == 12) errmsg += "Tree has more than 20 logs";
                            else if (errflg == 13) errmsg += "Top diameter greater than DBH inside bark";
                            else if (errflg == 14) errmsg += "The bark equation for the VOLEQ does not exist or yields a negative DBHIB";
                            Toast.MakeText(this, errmsg, ToastLength.Long).Show();
                        }
                    }
                }
            }
        }
    }
}