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
    [Activity(Label = "ContractSpeciesSetUp")]
    public class ContractSpeciesActivity : Activity
    {
        string sCruiseFile, sCruiseFileName, sOrStatement;
        EditText txtConSp;
        TextView tvCruiseFileName;
        Button btnOk, btnCancel;
        MyDatabase myCruiseDB;
        ListView lvTemp;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.ContractSpecies);

            sCruiseFile = Intent.GetStringExtra("cruiseFile") ?? string.Empty;
            sOrStatement = Intent.GetStringExtra("OrStatement") ?? string.Empty;
            if (!string.IsNullOrEmpty(sCruiseFile))
            {
                myCruiseDB = new MyDatabase(sCruiseFile);
            }

            tvCruiseFileName = FindViewById<TextView>(Resource.Id.tvFileNameConSp);
            txtConSp = FindViewById<EditText>(Resource.Id.txtConSpecies);
            btnOk = FindViewById<Button>(Resource.Id.btnConSpOk);
            //not sure why the button cancel is using lowercase calcel
            btnCancel = FindViewById<Button>(Resource.Id.btnConSpCancel);

            int position = sCruiseFile.LastIndexOf('/');
            sCruiseFileName = sCruiseFile.Substring(position + 1);
            tvCruiseFileName.Text = sCruiseFileName;

            btnOk.Click += BtnOk_Click;
            btnCancel.Click += BtnCancel_Click;
            GetTreeDefaultValueView(sOrStatement);
            // get ListView object instance from resource and add ItemClick, EventHandler.
            lvTemp = FindViewById<ListView>(Resource.Id.lvConSpTemp);
            lvTemp.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(ListView_ItemClick);
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
            //Toast.MakeText(this, "Row clicked!", ToastLength.Long).Show();
            //try to reset the check box by click on the row, but not working!!!
            if (e.View.FindViewById<CheckBox>(Resource.Id.cbConSpSelectShow).Checked == false)
            {
                e.View.FindViewById<CheckBox>(Resource.Id.cbConSpSelectShow).Checked = true;
                //Toast.MakeText(this, "Checked!", ToastLength.Long).Show();
            }
            else
            {
                e.View.FindViewById<CheckBox>(Resource.Id.cbConSpSelectShow).Checked = false;
                //Toast.MakeText(this, "UNChecked!", ToastLength.Long).Show();
            }

        }
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent();
            SetResult(Result.Canceled, intent);
            base.Finish();
        }
        private void BtnOk_Click(object sender, EventArgs e)
        {
            var vb = (Vibrator)Android.App.Application.Context.GetSystemService(Android.App.Application.VibratorService);
            string sCN;
            bool Selected = false;
            for (int i = 0; i < lvTemp.Count; i++)
            {
                var v = lvTemp.GetChildAt(i);
                if(v!=null)
                {
                    TextView tvCN = (TextView) v.FindViewById(Resource.Id.tvConSpIdShow);
                    sCN = tvCN.Text.ToString();
                    CheckBox cbSelect = (CheckBox) v.FindViewById(Resource.Id.cbConSpSelectShow);
                    if(cbSelect.Checked == true)
                    {
                        
                        //first check there is input for contract species
                        if(string.IsNullOrEmpty(txtConSp.Text.ToString()))
                        {
                            Toast.MakeText(this, "Please enter a Contract Species", ToastLength.Long).Show();
                            vb.Vibrate(VibrationEffect.CreateOneShot(200, VibrationEffect.DefaultAmplitude));

                            txtConSp.RequestFocus();
                            return;
                        }
                        //Toast.MakeText(this, "Updating TreeDefaultValues for contract species: " + sCN, ToastLength.Long).Show();
                        //update database TreeDefaultvalue for ContractSpecies
                        myCruiseDB.UpdateContractSpecies(sCN, txtConSp.Text.ToString());
                        Selected = true;
                    }
                }
            }

            // put the String to pass back into an Intent and close this activity
            Intent intent = new Intent();
            if(Selected == true)
            {
                intent.PutExtra(Intent.ExtraText, txtConSp.Text.ToString());
                SetResult(Result.Ok, intent);
            }
            else
            {
                Toast.MakeText(this, "Please select species for the Contract Species", ToastLength.Long).Show();
                vb.Vibrate(VibrationEffect.CreateOneShot(200, VibrationEffect.DefaultAmplitude));
                return;
                //SetResult(Result.Canceled, intent);
            }
            
            base.Finish();
        }
        protected void GetTreeDefaultValueView(string strOr)
        {
            if (sCruiseFile.Length > 0)
            {
                Android.Database.ICursor icTemp = myCruiseDB.GetTreeDefaultValueCursor(strOr);
                if (icTemp != null && icTemp.MoveToFirst())
                {
                    //icTemp.MoveToFirst();
                    ListView lvTemp = FindViewById<ListView>(Resource.Id.lvConSpTemp);
                    string[] from = new string[] { "_id", "Species", "PrimaryProduct", "FIAcode", "ContractSpecies" };
                    int[] to = new int[] {
                    Resource.Id.tvConSpIdShow,
                    Resource.Id.tvConSpSpeciesShow,
                    Resource.Id.tvConSpProdShow,
                    Resource.Id.tvConSpFIACodeShow,
                    Resource.Id.tvConSpConSpeciesShow
                    };
                    // creating a SimpleCursorAdapter to fill ListView object.
                    SimpleCursorAdapter scaTemp = new SimpleCursorAdapter(this, Resource.Layout.consp_record, icTemp, from, to);
                    lvTemp.Adapter = scaTemp;
                }
                else
                {
                    ListView lvTemp = FindViewById<ListView>(Resource.Id.lvConSpTemp);
                    lvTemp.Adapter = null;
                }
            }
        }
    }
}