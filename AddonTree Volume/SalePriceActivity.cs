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
    [Activity(Label = "Sale Price")]
    public class SalePriceActivity : Activity
    {
        List<string> SpecList = new List<string>() { "Select Spec", "Custom" };
        List<string> ProdList = new List<string>() { "Select Prod", "01", "02","New" };
        string sCruiseFile, sAddvolFile, sCruiseFileName;
        string sUserInit, sSaleName, sSaleNum, sId;
        Spinner spnSpec, spnProd;
        EditText txtPrice, txtSpecies, txtProd;
        TextView tvSaleName, tvSaleNum, tvCruiseFileName;
        Button btnSave, btnExit, btnDelete, btnClear;
        MyDatabase myCruiseDB, myAddvolDB;
        //bool ConSpec = false;
        //int ConSpecCnt = 0;

        //Boolean MoreSpList = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.SalePrice);
            sAddvolFile = Intent.GetStringExtra("addvolFile") ?? string.Empty;
            if (!string.IsNullOrEmpty(sAddvolFile))
            { myAddvolDB = new MyDatabase(sAddvolFile); }
            sCruiseFile = Intent.GetStringExtra("cruiseFile") ?? string.Empty;
            if (!string.IsNullOrEmpty(sCruiseFile))
            {
                myCruiseDB = new MyDatabase(sCruiseFile);
            }
            CreateProdList();
            //CreateCruiseSpList("Species", "Tree");
            //changed to the following
            CreateCruiseSpList("ContractSpecies", "TreeDefaultValue");

            sSaleName = Intent.GetStringExtra("SaleName") ?? string.Empty;
            sSaleNum = Intent.GetStringExtra("SaleNum") ?? string.Empty;
            sUserInit = Intent.GetStringExtra("UserInit") ?? string.Empty;

            tvCruiseFileName = FindViewById<TextView>(Resource.Id.tvFileNameSP);
            tvSaleName = FindViewById<TextView>(Resource.Id.tvSaleNameSP);
            tvSaleNum = FindViewById<TextView>(Resource.Id.tvSaleNumSP);
            spnSpec = FindViewById<Spinner>(Resource.Id.spnSpecSP);
            txtSpecies = FindViewById<EditText>(Resource.Id.txtSpeciesSP);
            spnProd = FindViewById<Spinner>(Resource.Id.spnProdSP);
            txtProd = FindViewById<EditText>(Resource.Id.txtProdSP);
            txtPrice = FindViewById<EditText>(Resource.Id.txtPriceSP);
            btnDelete = FindViewById<Button>(Resource.Id.btnDeleteSP);
            btnExit = FindViewById<Button>(Resource.Id.btnExitSP);
            btnSave = FindViewById<Button>(Resource.Id.btnSaveSP);
            btnClear = FindViewById<Button>(Resource.Id.btnClearSP);

            int position = sCruiseFile.LastIndexOf('/');
            sCruiseFileName = sCruiseFile.Substring(position + 1);
            tvCruiseFileName.Text = sCruiseFileName;
            tvSaleName.Text = "Sale Name: " + sSaleName;
            tvSaleNum.Text = "Sale Number: " + sSaleNum;

            txtPrice.AfterTextChanged += TxtPrice_AfterTextChanged;

            var adapterSpec = new ArrayAdapter<string>(this, Resource.Layout.spinner_layout, SpecList);
            spnSpec.Adapter = adapterSpec;
            spnSpec.ItemSelected += SpinnerSpec_ItemSelected;
            var adapterProd = new ArrayAdapter<string>(this, Resource.Layout.spinner_layout, ProdList);
            spnProd.Adapter = adapterProd;
            spnProd.ItemSelected += SpinnerProd_ItemSelected;

            btnExit.Click += BtnExit_Click;
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;
            btnClear.Click += BtnClear_Click;

            GetSalePriceView();
            // get ListView object instance from resource and add ItemClick, EventHandler.
            ListView lvTemp = FindViewById<ListView>(Resource.Id.lvSPTemp);
            lvTemp.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(ListView_ItemClick);
        }
        private void TxtPrice_AfterTextChanged(object sender, Android.Text.AfterTextChangedEventArgs e)
        {
            var text = e.Editable.ToString();
            txtPrice.AfterTextChanged -= TxtPrice_AfterTextChanged;
            var formatedText = DBHwithOneDecimal(text);
            txtPrice.Text = formatedText;
            txtPrice.SetSelection(formatedText.Length);
            txtPrice.AfterTextChanged += TxtPrice_AfterTextChanged;
        }
        private static string DBHwithOneDecimal(string text)
        {
            var numbers = Regex.Replace(text, @"\.?", "");
            if (numbers.Length == 0)
                return numbers;
            //string strNum = numbers;
            int intNum = int.Parse(numbers);
            numbers = intNum.ToString();
            if (numbers.Length == 1)
                return string.Format(".0{0}", numbers.ToString());
            if (numbers.Length == 2)
                return string.Format(".{0}", numbers.ToString());

            int strLen = numbers.Length;
            return string.Format("{0}.{1}", numbers.Substring(0, strLen - 2), numbers.Substring(strLen - 2));
        }
        private void BtnExit_Click(object sender, EventArgs e)
        {
            var inputManager = (Android.Views.InputMethods.InputMethodManager)GetSystemService(InputMethodService);
            inputManager.HideSoftInputFromWindow(btnExit.WindowToken, Android.Views.InputMethods.HideSoftInputFlags.None);
            base.Finish();
        }
        private void BtnSave_Click(object sender, EventArgs e)
        {
            string sSpec, sProd, sPrice;
            sSpec = null;
            sProd = null;
            sPrice = null;
            if (txtSpecies.Text.ToString().Length > 0)
            {
                sSpec = txtSpecies.Text.ToString();
                SpecList.Remove("Custom");
                SpecList.Add(txtSpecies.Text.ToString());
                txtSpecies.Text = "";
                SpecList.Add("Custom");
                var adapterSpec = new ArrayAdapter<string>(this, Resource.Layout.spinner_layout, SpecList);
                spnSpec.Adapter = adapterSpec;
                ShowTxtSpec(false);
            }
            else
            {
                if (spnSpec.SelectedItemPosition > 0) sSpec = spnSpec.SelectedItem.ToString();
            }
            if(string.IsNullOrEmpty(sSpec))
            {
                Toast.MakeText(this, "Please select a species from the list...", ToastLength.Short).Show();
                spnSpec.RequestFocus();
                return;
            }
            if(txtProd.Text.ToString().Length>0)
            {
                sProd = txtProd.Text.ToString();
                ProdList.Remove("New");
                ProdList.Add(txtProd.Text.ToString());
                txtProd.Text = "";
                ProdList.Add("New");
                var adapterProd = new ArrayAdapter<string>(this, Resource.Layout.spinner_layout, ProdList);
                spnProd.Adapter = adapterProd;
                ShowTxtProd(false);
            }
            else
            {
                if (spnProd.SelectedItemPosition > 0) sProd = spnProd.SelectedItem.ToString();
            }
            if (string.IsNullOrEmpty(sProd))
            {
                Toast.MakeText(this, "Please select a prod from the list...", ToastLength.Short).Show();
                spnProd.RequestFocus();
                return;
            }
            if (txtPrice.Text.ToString().Length > 0)
            {
                sPrice = txtPrice.Text.ToString();
            }
            else
            {
                Toast.MakeText(this, "Please enter a value for price...", ToastLength.Short).Show();
                txtPrice.RequestFocus();
                return;
            }
            if(string.IsNullOrEmpty(sId))
            {
                if (myAddvolDB.SalePriceExist(sSpec, sProd))
                {
                    Toast.MakeText(this, "The SalePrice already has a record for Species " + sSpec + " and Prod " + sProd, ToastLength.Short).Show();
                    return;
                }
                else
                    myAddvolDB.InsertSalePrice(sSpec, sProd, sPrice, sUserInit);
            }
            else
            {
                myAddvolDB.UpdateSalePrice(sId, sSpec, sProd, sPrice, sUserInit);
            }
            Toast.MakeText(this, myAddvolDB.Message, ToastLength.Short).Show();
            GetSalePriceView();
            clearRecord();
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(sId))
                clearRecord();
            else
                DeleteAlert();
        }
        private void BtnClear_Click(object sender, EventArgs e)
        {
            clearRecord();
        }
        private void DeleteAlert()
        {
            Android.App.AlertDialog.Builder dialog = new AlertDialog.Builder(this);
            AlertDialog alert = dialog.Create();

            alert.SetTitle("Delete Sale Price Record");
            alert.SetMessage("Do you want to delete the Sale Price for " + spnSpec.SelectedItem + " Prod = " + spnProd.SelectedItem + "? ");
            alert.SetIcon(Resource.Drawable.info);
            alert.SetButton("YES", (c, ev) =>
            {
                // Ok button click, then continue to delete record
                if(!string.IsNullOrEmpty(sAddvolFile)&& !string.IsNullOrEmpty(sId))
                {myAddvolDB.DeleteSalePrice(int.Parse(sId)); }
                //clear fields
                {
                    sId = "";
                    txtSpecies.Text = "";
                    spnSpec.SetSelection(0);
                    txtProd.Text = "";
                    spnProd.SetSelection(0);
                    txtPrice.Text = "";
                    GetSalePriceView();
                }
            });
            alert.SetButton2("NO", (c, ev) => {
            });
            alert.Show();
        }
        private void ShowTxtSpec(bool show)
        {
            LinearLayout.LayoutParams hideParams = new LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.WrapContent, 0.0f);
            LinearLayout.LayoutParams displayParams = new LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.WrapContent, 1.0f);
            if(show==true)
            {
                spnSpec.LayoutParameters = hideParams;
                //spnSpec.Visibility = Android.Views.ViewStates.Gone;
                txtSpecies.LayoutParameters = displayParams;
                //txtSpecies.Visibility = Android.Views.ViewStates.Visible;
            }
            else
            {
                spnSpec.LayoutParameters = displayParams;
                txtSpecies.LayoutParameters = hideParams;
            }
        }
        private void ShowTxtProd(bool show)
        {
            LinearLayout.LayoutParams hideParams = new LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.WrapContent, 0.0f);
            LinearLayout.LayoutParams displayParams = new LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.WrapContent, 1.0f);
            if (show == true)
            {
                spnProd.LayoutParameters = hideParams;
                //spnSpec.Visibility = Android.Views.ViewStates.Gone;
                txtProd.LayoutParameters = displayParams;
                //txtSpecies.Visibility = Android.Views.ViewStates.Visible;
            }
            else
            {
                spnProd.LayoutParameters = displayParams;
                txtProd.LayoutParameters = hideParams;
            }
        }
        private void CreateProdList()
        {
            ProdList = new List<string>() { "Select Prod" };
            List<string> ProdListTemp = new List<string>() { };
            List<string> ProdListTemp2 = new List<string>() { };
            string dbColumn = "PrimaryProduct";
            string dbTable = "TreeDefaultValue";
            string prodcode;
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
                            prodcode = cProdList.GetString(0);
                            ProdListTemp.Add(prodcode);
                        } while (cProdList.MoveToNext());
                    }
                }
                dbColumn = "SecondaryProduct";
                dbTable = "SampleGroup";
                Android.Database.ICursor cProdList1 = myCruiseDB.lvQueryCruiseDB(dbColumn, dbTable);
                if (cProdList1 != null)
                {
                    if (cProdList1.MoveToFirst())
                    {
                        do
                        {
                            prodcode = cProdList1.GetString(0);
                            ProdListTemp.Add(prodcode);
                        } while (cProdList1.MoveToNext());
                    }
                }
            }
            dbColumn = "Prod";
            dbTable = "SalePrice";
            Android.Database.ICursor cProdList2 = myAddvolDB.lvQueryCruiseDB(dbColumn, dbTable);
            if (cProdList2 != null)
            {
                if (cProdList2.MoveToFirst())
                {
                    do
                    {
                        prodcode = cProdList2.GetString(0);
                        ProdListTemp.Add(prodcode);
                    } while (cProdList2.MoveToNext());
                }
            }
            dbColumn = "Prod";
            dbTable = "AddTree";
            Android.Database.ICursor cProdList3 = myAddvolDB.lvQueryCruiseDB(dbColumn, dbTable);
            if (cProdList3 != null)
            {
                if (cProdList3.MoveToFirst())
                {
                    do
                    {
                        prodcode = cProdList3.GetString(0);
                        ProdListTemp.Add(prodcode);
                    } while (cProdList3.MoveToNext());
                }
            }
            ProdListTemp.Sort();
            ProdListTemp2 = ProdListTemp.Distinct().ToList();
            ProdList.AddRange(ProdListTemp2);
            ProdList.Add("New");
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
        private void SpinnerProd_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            string s1 = ProdList[e.Position].ToString();

            if (s1 == "New")
            {
                ShowTxtProd(true);
                spnProd.SetSelection(0);
                txtProd.RequestFocus();
            }
        }
        private void SpinnerSpec_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            string s1 = SpecList[e.Position].ToString();
            if (s1 == "More")
            {
                CreateCruiseSpList("Species", "TreeDefaultValue");
                var adapterSpecMore = new ArrayAdapter<string>(this, Resource.Layout.spinner_layout, SpecList);
                spnSpec.Adapter = adapterSpecMore;
                spnSpec.PerformClick();
                //MoreSpList = true;
            }
            else if (s1 == "Custom")
            {
                List<string> SpecList1 = new List<string>() { };
                List<string> SpecList2 = new List<string>() { };
                List<string> SpecList3 = new List<string>() { };
                string spec;
                //get species list from AddTree
                Android.Database.ICursor CruiseSp1 = myAddvolDB.lvQueryCruiseDB("Species", "AddTree");
                if (CruiseSp1 != null)
                {
                    if (CruiseSp1.MoveToFirst())
                    {
                        do
                        {
                            spec = CruiseSp1.GetString(CruiseSp1.GetColumnIndex("Species"));
                            SpecList1.Add(spec);
                        } while (CruiseSp1.MoveToNext());
                    }
                }
                //get species from cruisefile Tree
                Android.Database.ICursor CruiseSp = myCruiseDB.lvQueryCruiseDB("Species", "Tree");
                if (CruiseSp != null)
                {
                    if (CruiseSp.MoveToFirst())
                    {
                        do
                        {
                            spec = CruiseSp.GetString(CruiseSp.GetColumnIndex("Species"));
                            SpecList2.Add(spec);
                        } while (CruiseSp.MoveToNext());
                    }
                }
                SpecList3 = SpecList1.Except(SpecList2).ToList();
                string strSpec="";
                foreach (string value in SpecList3)
                {
                    strSpec = strSpec + "'"+value+"',";
                }
                string OrClause = "";
                if(strSpec.Length>0)
                {
                    //remove the last comma
                    //strSpec.TrimEnd(',');
                    strSpec = strSpec.Substring(0, strSpec.Length - 1);
                    OrClause = OrClause + "OR Species IN (" + strSpec + ")";
                }
                //ShowTxtSpec(true);
                //spnSpec.SetSelection(0);
                //txtSpecies.RequestFocus();
                var intent = new Intent(this, typeof(ContractSpeciesActivity));
                intent.PutExtra("cruiseFile", sCruiseFile);
                intent.PutExtra("OrStatement", OrClause);
                int requestCode = 1001;
                StartActivityForResult(intent, requestCode);
            }
        }
        private void CreateCruiseSpList(string dbColumn, string dbTable)
        {
            SpecList = new List<string>() { "Select Spec" };
            List<string> SpecListTemp = new List<string>() { };
            List<string> SpecListTemp2 = new List<string>() { };
            string spec;
            //comment out on 2020/03/13
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
            if (!string.IsNullOrEmpty(sCruiseFile))
            {
                //comment out 2020/03/13
                //Android.Database.ICursor CruiseSp = myCruiseDB.lvQueryCruiseDB(dbColumn, dbTable);
                //if (CruiseSp != null)
                //{
                //    if (CruiseSp.MoveToFirst())
                //    {
                //        do
                //        {
                //            spec = CruiseSp.GetString(CruiseSp.GetColumnIndex("Species"));
                //            SpecListTemp.Add(spec);
                //        } while (CruiseSp.MoveToNext());
                //    }
                //}
                Android.Database.ICursor CruiseConSp = myCruiseDB.lvQueryCruiseDB("ContractSpecies", "TreeDefaultValue");
                if (CruiseConSp != null)
                {
                    if (CruiseConSp.MoveToFirst())
                    {
                        do
                        {
                            spec = CruiseConSp.GetString(CruiseConSp.GetColumnIndex("ContractSpecies"));
                            spec= spec.Trim();
                            //if contract species is set in cruise file, list them on top of the list
                            if (!string.IsNullOrEmpty(spec)) SpecList.Add(spec);
                        } while (CruiseConSp.MoveToNext());
                    }
                    //remove null from the SpecList
                    //SpecList.Remove("");
                    //SpecList.Remove(" ");
                }
            }
            //if(SpecList.Count()>1)
            //{
            //    ConSpec = true;
            //    ConSpecCnt = SpecList.Count() - 1;
            //}
            //else
            //{
            //    ConSpec = false;
            //    ConSpecCnt = 0;
            //}
            SpecListTemp.Remove("");
            SpecListTemp.Remove(" ");
            SpecListTemp.Sort();
            SpecListTemp2 = SpecListTemp.Distinct().ToList();
            SpecList.AddRange(SpecListTemp2);
            SpecList = SpecList.Distinct().ToList();
            if (string.IsNullOrEmpty(sCruiseFile)) SpecList.Add("Custom");
            else if (dbTable.ToUpper() == "TREE")
            { SpecList.Add("More"); }
            else if (dbTable == "TreeDefaultValue")
            { SpecList.Add("Custom"); }

        }
        protected void GetSalePriceView()
        {
            //if (myAddonDB.DatabaseAvailable)
            if (sAddvolFile.Length > 0)
            {
                Android.Database.ICursor icTemp = myAddvolDB.GetSalePriceCur();
                if (icTemp != null && icTemp.MoveToFirst())
                {
                    //icTemp.MoveToFirst();
                    ListView lvTemp = FindViewById<ListView>(Resource.Id.lvSPTemp);
                    string[] from = new string[] { "_id", "Species", "Prod", "Price" };
                    int[] to = new int[] {
                    Resource.Id.tvSpIdShow,
                    Resource.Id.tvSpSpeciesShow,
                    Resource.Id.tvSpProdShow,
                    Resource.Id.tvSpPriceShow
                    };
                    // creating a SimpleCursorAdapter to fill ListView object.
                    SimpleCursorAdapter scaTemp = new SimpleCursorAdapter(this, Resource.Layout.sp_record, icTemp, from, to, 0);
                    lvTemp.Adapter = scaTemp;
                }
                else
                {
                    ListView lvTemp = FindViewById<ListView>(Resource.Id.lvSPTemp);
                    lvTemp.Adapter = null;
                }
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
            TextView tvIdShow = e.View.FindViewById<TextView>(Resource.Id.tvSpIdShow);
            TextView tvSpeciesShow = e.View.FindViewById<TextView>(Resource.Id.tvSpSpeciesShow);
            TextView tvProdShow = e.View.FindViewById<TextView>(Resource.Id.tvSpProdShow);
            TextView tvPriceShow = e.View.FindViewById<TextView>(Resource.Id.tvSpPriceShow);

            //display the items on the fields to modify
            string sSpecies, sPrice, sProd;
            sId = tvIdShow.Text;
            sSpecies = tvSpeciesShow.Text;
            sPrice = tvPriceShow.Text;
            int intPrice = (int)(float.Parse(sPrice) * 100.0);
            sPrice = intPrice.ToString();
            sProd = tvProdShow.Text;
            SetSpinnerSelection(spnSpec, SpecList, sSpecies);
            SetSpinnerSelection(spnProd, ProdList, sProd);
            txtPrice.Text = sPrice;
            
        }
        private void clearRecord()
        {
            sId = null;
            txtSpecies.Text = null;
            txtProd.Text = null;
            txtPrice.Text = null;
            ShowTxtProd(false);
            ShowTxtSpec(false);
            spnSpec.SetSelection(0);
            spnProd.SetSelection(0);
        }

        //
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            CreateCruiseSpList("ContractSpecies", "TreeDefaultValue");
            var adapterSpec = new ArrayAdapter<string>(this, Resource.Layout.spinner_layout, SpecList);
            spnSpec.Adapter = adapterSpec;
            if (requestCode == 1001)
            {
                //CreateCruiseSpList("ContractSpecies", "TreeDefaultValue");
                if (resultCode == Result.Ok)
                {
                    string sConSp = data.GetStringExtra(Intent.ExtraText) ?? string.Empty;
                    //txtSpecies.Text = sConSp;
                    //ShowTxtSpec(true);
                    //spnSpec.SetSelection(0);
                    SetSpinnerSelection(spnSpec, SpecList, sConSp);
                }
                else if (resultCode == Result.Canceled)
                {
                    spnSpec.SetSelection(0);
                }
                
                
            }
        }
    }
}