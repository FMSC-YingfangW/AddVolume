using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Android.App;
using Android.Widget;
using Android.OS;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android;

using Android.Graphics;
using Android.Util;
using Java.IO;
using Environment = Android.OS.Environment;

namespace AddonTree_Volume
{
    [Activity(Label = "Add Volume", MainLauncher = true, Icon = "@drawable/AddVol")]
    public class MainActivity : Activity
    {
        string myCruiseFile;
        string myAddonFile;
        string myaddvolFolder;
        Button button3;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            Button button1 = FindViewById<Button>(Resource.Id.Button1);
            button1.Click += Button_Click;
            Button button2 = FindViewById<Button>(Resource.Id.Button2);
            button2.Click += Button2_Click;
            button3 = FindViewById<Button>(Resource.Id.Button3);
            button3.Click += Button3_Click;
        }

        private async void Button3_Click(object sender, EventArgs e)
        {
            SimpleFileDialog fileDialog = new SimpleFileDialog(this, SimpleFileDialog.FileSelectionMode.OpenCruise);
            string path = await fileDialog.GetFileOrDirectoryAsync(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath);
            if (!string.IsNullOrEmpty(path))
            {
                //Use path
                myaddvolFolder = path;
                button3.Text = path;
                var intent = new Intent(this, typeof(AddTreeActivity));
                intent.PutExtra("myCruiseFile", path);
                StartActivity(intent);
            }
            //base.Finish();
            //System.Environment.Exit(0);
        }
        //New Sale button
        private async void Button_Click(object sender, EventArgs e)
        {
            SimpleFileDialog fileDialog = new SimpleFileDialog(this, SimpleFileDialog.FileSelectionMode.OpenCruise);
            string path = await fileDialog.GetFileOrDirectoryAsync(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/Documents");
            path = path.Replace(".CRUISE", ".cruise");
            if (!string.IsNullOrEmpty(path) && path.ToLower().Contains(".cruise"))
            {
                //Use path
                var intent = new Intent(this, typeof(AddTreeActivity));
                intent.PutExtra("myCruiseFile", path);
                StartActivity(intent);
            }
            else
            {
                //SelectCruise_Alert();
                Alert_Continue(); 
            }
        }
        //Open Existing Sale button
        private async void Button2_Click(object sender, EventArgs e)
        {
            SimpleFileDialog fileDialog = new SimpleFileDialog(this, SimpleFileDialog.FileSelectionMode.OpenAddvol);
            string path = await fileDialog.GetFileOrDirectoryAsync(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/Documents");
            if (!string.IsNullOrEmpty(path) && path.ToLower().Contains(".addvol"))
            {
                //Use path
                var intent = new Intent(this, typeof(AddTreeActivity));
                intent.PutExtra("myAddonFile", path);
                StartActivity(intent);
            }
            else
                Toast.MakeText(this, "You did not select a .addvol file!!! ", ToastLength.Long).Show();
        }

        private void Alert_Setup(string filetype)
        {
            Android.App.AlertDialog.Builder dialog = new AlertDialog.Builder(this);
            AlertDialog alert = dialog.Create();
            int resultCode = 5555;
            if (filetype == "cruise")
            {
                resultCode = 5555;
                alert.SetTitle("Select cruise file(.cruise)");
                alert.SetMessage("Please select a cruise file (.cruise) for the local volume table.");
            }
            else if(filetype=="addvol")
            {
                resultCode = 8888;
                alert.SetTitle("Select Addon Tree file(.addvol)");
                alert.SetMessage("Please select a addvol file (.addvol) to continue adding trees.");
            }
            alert.SetIcon(Resource.Drawable.info);
            alert.SetButton("OK", (c, ev) =>
            {
                // Ok button click task  
                Intent i = new Intent(Intent.ActionGetContent);
                StartActivityForResult(i, resultCode);
            });
            alert.SetButton2("CANCEL", (c, ev) => 
            {
                //user select Cancel to Open cruise file, then get option to continue without a cruise file
                if (filetype == "cruise")
                {
                    Alert_Continue();
                    //SelectCruise_Alert();
                }                
            });
            alert.Show();
        }
        private void SelectCruise_Alert()
        {
            Android.App.AlertDialog.Builder dialog = new AlertDialog.Builder(this);
            AlertDialog alert = dialog.Create();

            alert.SetTitle("Select cruise file");
            alert.SetMessage("Please select a cruise file (.cruise) to continue!");
            alert.SetIcon(Resource.Drawable.info);
            alert.SetButton("Ok", (c, ev) => {
                myCruiseFile = "";
            });
            alert.Show();
        }
        private void Alert_Continue()
        {
            Android.App.AlertDialog.Builder dialog = new AlertDialog.Builder(this);
            AlertDialog alert = dialog.Create();

            alert.SetTitle("Continue without a cruise file?");
            alert.SetMessage("You didn't select a cruise file. You can continue to enter trees and add the cruise file later. Do you want to continue?");
            alert.SetIcon(Resource.Drawable.info);
            alert.SetButton("YES", (c, ev) =>
            {
                // Ok button click, then continue to tree enter screen
                var intent = new Intent(this, typeof(AddTreeActivity));
                intent.PutExtra("myCruiseFile", myCruiseFile);
                StartActivity(intent);
            });
            alert.SetButton2("NO", (c, ev) =>
            {
                myCruiseFile = "";
            });
            alert.Show();
            //Toast.MakeText(this, "Please select a cruise file to continue! ", ToastLength.Long).Show();
        }
        private async void ChooseFolder()
        {
            SimpleFileDialog fileDialog = new SimpleFileDialog(this, SimpleFileDialog.FileSelectionMode.FolderChoose);
            string path = await fileDialog.GetFileOrDirectoryAsync(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath);
            if (!string.IsNullOrEmpty(path))
            {
                //Use path
                myaddvolFolder = path;
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == 5555 || requestCode == 8888)
            {
                if (resultCode == Result.Ok)
                {
                    var path = data.Data;
                    //Toast.MakeText(ApplicationContext, path.ToString(), ToastLength.Long).Show();
                    if(requestCode == 5555 && !path.ToString().ToLower().Contains(".cruise"))
                    {
                        //the selected file is not a cruise file, please reselect file
                        string filetype = "cruise";
                        Alert_Setup(filetype);
                    }
                    else if (requestCode == 8888 && !path.ToString().ToLower().Contains(".addvol"))
                    {
                        //the selected file is not a addon tree file, please reselect file
                        string filetype = "addvol";
                        Alert_Setup(filetype);
                    }
                    else
                    {
                        if (requestCode == 5555) myCruiseFile = path.ToString().Substring(7);
                        if (requestCode == 8888) myAddonFile = path.ToString().Substring(7);
                        //add codes here to continue to enter tree screen
                        //pass the Cruise file and addon file to AddTree form
                        var intent = new Intent(this, typeof(AddTreeActivity));
                        intent.PutExtra("myCruiseFile", myCruiseFile);
                        intent.PutExtra("myAddonFile", myAddonFile);
                        //clear the file name variables
                        myCruiseFile = "";
                        myAddonFile = "";
                        StartActivity(intent);
                    }
                }
                else if (resultCode == Result.Canceled)
                {
                    //continue to enter tree screen without a cruise file
                    if (requestCode == 5555)
                    {
                        Alert_Continue();
                    }
                    else
                    {
                        myAddonFile = "";
                    }
                }
            }
        }

    }

}

