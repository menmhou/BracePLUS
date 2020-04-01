using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using BracePLUS.Droid;
using BracePLUS.Services;

[assembly: Xamarin.Forms.Dependency(typeof(MessageAndroid))]
namespace BracePLUS.Droid
{
    public class MessageAndroid : IMessage
    {
        public void LongAlert(string message)
        {
            new Thread(new ThreadStart(() =>
            {
                Toast.MakeText(Application.Context, message, ToastLength.Long).Show();
            })).Start();
        }

        public void ShortAlert(string message)
        {
            new Thread(new ThreadStart(() =>
            {
                Toast.MakeText(Application.Context, message, ToastLength.Short).Show();
            })).Start();
        }
    }
}