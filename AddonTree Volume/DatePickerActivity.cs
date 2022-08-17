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
    [Activity(Label = "DatePickerActivity", Theme = "@style/dpTheme")]
    public class DatePickerActivity : Activity
    {
        string sInspectionDate; // = DateTime.Today.ToString("yyyy-MM-dd");
        DatePicker datePicker;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.ReportDatePicker);
            datePicker = FindViewById<DatePicker>(Resource.Id.datePicker1);
            Button okPicker = FindViewById<Button>(Resource.Id.DatePickerOk);
            //sInspectionDate = datePicker.DateTime.ToString("yyyy-MM-dd");
            datePicker.DateChanged += DatePicker_DateChanged;
            okPicker.Click += BtnOk_Click;
        }

        private void DatePicker_DateChanged(object sender, DatePicker.DateChangedEventArgs e)
        {
            if(datePicker.DateTime>DateTime.Today)
            {
                Toast.MakeText(this, "Future date is not allowed for Inspection Date!", ToastLength.Long).Show();
                datePicker.DateTime = DateTime.Today;
            }
            else
                sInspectionDate = datePicker.DateTime.ToString("yyyy-MM-dd");
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            //Toast.MakeText(this, "InspectionDate: " + sInspectionDate, ToastLength.Long).Show();
            Intent intent = new Intent();
            intent.PutExtra("InspectionDate", sInspectionDate);
            SetResult(Result.Ok, intent);
            base.Finish();
        }
    }
}