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

namespace AddonTree_Volume
{
    [Activity(Label = "TallyTreeActivity")]
    public class TallyTreeActivity : Activity
    {
        string sCruiseFile, sAddvolFile;
        string sCutUnit, sSaleName, sSaleNum, sTallySpec, sTallyProd;
        MyDatabase myCruiseDB, myAddvolDB;
        Spinner spnSpec, spnProd;
        TextView tvCruiseFile, tvSaleName, tvSaleNum, tvCutUnit;
        Button btnSaveTally, btnExitTally;
        RadioButton rbPlus, rbMinus;
        List<string> SpList = new List<string>();
        List<string> SpPrdList = new List<string>();
        GridView lvDBHclassTemp;


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.TallyTree);

            //get the variable from previous screen
            sCruiseFile = Intent.GetStringExtra("cruiseFile") ?? string.Empty;
            if (!string.IsNullOrEmpty(sCruiseFile))
            { myCruiseDB = new MyDatabase(sCruiseFile); }
            sAddvolFile = Intent.GetStringExtra("addvolFile") ?? string.Empty;
            if (!string.IsNullOrEmpty(sAddvolFile))
            { myAddvolDB = new MyDatabase(sAddvolFile); }
            sSaleName = Intent.GetStringExtra("SaleName") ?? string.Empty;
            sSaleNum = Intent.GetStringExtra("SaleNum") ?? string.Empty;
            sCutUnit = Intent.GetStringExtra("CutUnit") ?? string.Empty;
            sTallySpec = Intent.GetStringExtra("TallySpecies") ?? string.Empty;
            sTallyProd = Intent.GetStringExtra("TallyProduct") ?? string.Empty;

            tvCruiseFile = FindViewById<TextView>(Resource.Id.tvCruisefileTally);
            tvSaleName = FindViewById<TextView>(Resource.Id.tvSaleNameTally);
            tvCutUnit = FindViewById<TextView>(Resource.Id.tvCutUnitTally);
            tvSaleNum = FindViewById<TextView>(Resource.Id.tvSaleNumTally);
            spnSpec = FindViewById<Spinner>(Resource.Id.spnSpeciesTally);
            spnProd = FindViewById<Spinner>(Resource.Id.spnprodTally);
            rbMinus= FindViewById<RadioButton>(Resource.Id.radioButtonMinus);
            rbPlus = FindViewById<RadioButton>(Resource.Id.radioButtonPlus);
            
            CreateTallySpList();
            var adapterSpec = new ArrayAdapter<string>(this, Resource.Layout.spinner_layout, SpList);
            spnSpec.Adapter = adapterSpec;
            spnSpec.ItemSelected += SpinnerSpec_ItemSelected;

            CreateTallySpPrdList(SpList[0]);
            var adapterProd = new ArrayAdapter<string>(this, Resource.Layout.spinner_layout, SpPrdList);
            spnProd.Adapter = adapterProd;
            spnProd.ItemSelected += SpinnerProd_ItemSelected;

            //display Cruise info
            tvCruiseFile.Text = "Cruise File : " + sCruiseFile.Substring(sCruiseFile.LastIndexOf("/") + 1);
            tvSaleName.Text = "SaleName: " + sSaleName;
            tvSaleNum.Text = "SaleNumber: " + sSaleNum;
            tvCutUnit.Text = "CuttingUnit: " + sCutUnit;

            btnSaveTally = FindViewById<Button>(Resource.Id.buttonSaveTally);
            btnSaveTally.Click += ButtonSaveTally_Click;
            btnExitTally = FindViewById<Button>(Resource.Id.buttonExitTally);
            btnExitTally.Click += ButtonExitTally_Click;
            //display DBH class
            if (!string.IsNullOrEmpty(sTallySpec))
            {
                //set spinner for tally species and product
                SetSpinnerSelection(spnSpec, SpList, sTallySpec);
                SetSpinnerSelection(spnProd, SpPrdList, sTallyProd);
                //display the DBH class tally for the species and prod
                GetDBHclassCursorView(sTallySpec, sTallyProd);
            }
            else GetDBHclassCursorView(SpList[0], SpPrdList[0]);
            lvDBHclassTemp = FindViewById<GridView>(Resource.Id.listViewDBHclass);
            lvDBHclassTemp.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(ListView_ItemClick);

        }

        void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            TextView tvTrCnt;
            tvTrCnt = e.View.FindViewById<TextView>(Resource.Id.tvTreeCountShow);
            int Counter = int.Parse(tvTrCnt.Text);
            if (rbMinus.Checked)
            {
                if (Counter > 0) Counter -= 1;
            }
            else Counter += 1;
            e.View.FindViewById<TextView>(Resource.Id.tvTreeCountShow).Text = Counter.ToString();
        }
        //create species list for species spinner
        private void CreateTallySpList()
        {
            string spec;
            Android.Database.ICursor TallySp = myAddvolDB.GetTallySpecList();
            if (TallySp != null)
            {
                if (TallySp.MoveToFirst())
                {
                    do
                    {
                        spec = TallySp.GetString(TallySp.GetColumnIndex("Species"));
                        SpList.Add(spec);
                    } while (TallySp.MoveToNext());
                }
            }
        }
        //create tally species prod list
        private void CreateTallySpPrdList(string spec)
        {
            string prod;
            Android.Database.ICursor TallySpPrd = myAddvolDB.GetTallySpecProdList(spec);
            if (TallySpPrd != null)
            {
                if (TallySpPrd.MoveToFirst())
                {
                    do
                    {
                        prod = TallySpPrd.GetString(TallySpPrd.GetColumnIndex("Product"));
                        SpPrdList.Add(prod);
                    } while (TallySpPrd.MoveToNext());
                }
            }
        }
        //
        private void SpinnerSpec_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            SaveTallyTrees();
            string s1 = SpList[e.Position].ToString();
            CreateTallySpPrdList(s1);
            string sProd = spnProd.SelectedItem.ToString();
            GetDBHclassCursorView(s1, sProd);
        }
        private void SpinnerProd_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            SaveTallyTrees();
            string s1 = SpPrdList[e.Position].ToString();
            string sSpec = spnSpec.SelectedItem.ToString();
            GetDBHclassCursorView(sSpec, s1);
        }
        //
        private void ButtonSaveTally_Click(object sender, EventArgs e)
        {
            SaveTallyTrees();
            Toast.MakeText(this, "Tally trees saved", ToastLength.Short).Show();
        }
        private void SaveTallyTrees()
        {
            for (int i = 0; i < lvDBHclassTemp.Count; i++)
            {
                var v = lvDBHclassTemp.GetChildAt(i);
                if (v != null)
                {
                    TextView tvTrCnt = (TextView)v.FindViewById(Resource.Id.tvTreeCountShow);
                    //comment out the if to allow save the tally back to zero from mistake tally
                    //if (int.Parse(tvTrCnt.Text) > 0)
                    //{
                        TextView tvCN = (TextView)v.FindViewById(Resource.Id.tvIdShow);
                        myAddvolDB.UpdateTallyTreeCount2(tvCN.Text, tvTrCnt.Text);
                    //}
                }
            }
        }
        private void ButtonExitTally_Click(object sender, EventArgs e)
        {
            SaveTallyTrees();
            //var inputManager = (Android.Views.InputMethods.InputMethodManager)GetSystemService(InputMethodService);
            //inputManager.HideSoftInputFromWindow(buttonNext.WindowToken, Android.Views.InputMethods.HideSoftInputFlags.None);
            base.Finish();
        }
        private void ButtonTallyPlus_Click(object sender, EventArgs e)
        {
            TextView tvTallyTreCnt = FindViewById<TextView>(Resource.Id.tvTreeCountShow);
            int counter = int.Parse(tvTallyTreCnt.Text);
            counter += 1;
            tvTallyTreCnt.Text = counter.ToString();
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
        private void GetDBHclassCursorView(string spec, string prod)
        {
            if (sCruiseFile.Length > 0)
            {
                Android.Database.ICursor icTemp2 = myAddvolDB.GetTallyDBHclass(spec, prod);
                if (icTemp2 != null)
                {
                    icTemp2.MoveToFirst();
                    GridView lvRegTemp = FindViewById<GridView>(Resource.Id.listViewDBHclass);
                    string[] from = new string[] { "_id", "DBHclass", "TreeCount" };
                    int[] to = new int[] {
                    Resource.Id.tvIdShow,
                    Resource.Id.tvDBHclassShow,
                    Resource.Id.tvTreeCountShow
                    };
                    // creating a SimpleCursorAdapter to fill ListView object.
                    SimpleCursorAdapter scaTemp = new SimpleCursorAdapter(this, Resource.Layout.DBHclassRecord, icTemp2, from, to, 0);
                    lvRegTemp.Adapter = scaTemp;
                }
            }
        }

    }
}