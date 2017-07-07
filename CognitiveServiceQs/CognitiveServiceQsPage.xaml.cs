using System;
using System.Threading.Tasks;
using System.Linq;
using Acr.UserDialogs;
using Microsoft.ProjectOxford.Vision;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using Xamarin.Forms;
using Microsoft.ProjectOxford.Vision.Contract;

namespace CognitiveServiceQs
{
    public partial class CognitiveServiceQsPage : ContentPage
    {
        readonly VisionServiceClient client;

        public CognitiveServiceQsPage()
        {
            InitializeComponent();

            client = new VisionServiceClient(
                "d05b34c9fbdc4e88b223a4ba1c2a37d1",
                "https://westcentralus.api.cognitive.microsoft.com/vision/v1.0");

            btnPickImage.Clicked += PickImage;
            btnTakePhoto.Clicked += TakePhoto;
        }

        async void TakePhoto(object sender, EventArgs e)
        {
            var result = await TryRequestPermision("The app needs to access your camera to take photo.", "You didn't grant permission for using Camera.");

            if (!result) {
                return;
            }

            var initialized = await CrossMedia.Current.Initialize();

            if (!initialized || !CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsTakePhotoSupported)
            {
                return;
            }

            var options = new StoreCameraMediaOptions
            {
                CompressionQuality = 70,
                AllowCropping = true,
                DefaultCamera = CameraDevice.Rear,
                SaveToAlbum = true
            };
            var photoResult = await CrossMedia.Current.TakePhotoAsync(options);

            ProcessImage(photoResult);
        }

        async void PickImage(object sender, EventArgs e)
        {
            var result = await TryRequestPermision("The app needs to access your gallery to pick photo.", "You didn't grant permission for accessing gallery.");

            if (!result)
            {
                return;
            }

            var initialized = await CrossMedia.Current.Initialize();

            if (!initialized || !CrossMedia.Current.IsPickPhotoSupported)
            {
                return;
            }

            var options = new PickMediaOptions
            {
                CompressionQuality = 70
            };
            var photoResult = await CrossMedia.Current.PickPhotoAsync(options);

            ProcessImage(photoResult);
        }

        async void ProcessImage(MediaFile mediaFile)
        {
            imgPicked.Source = mediaFile.Path;

            UserDialogs.Instance.ShowLoading("Processing image with Azure Cognitive Services");

            var file = await PCLStorage.FileSystem.Current.GetFileFromPathAsync(mediaFile.Path);

            using (var stream = await file.OpenAsync(PCLStorage.FileAccess.Read))
            {
                var result = await client.RecognizeTextAsync(stream)
                                         .ContinueWith((Task<OcrResults> t) =>
                                {
                                    if (t.IsFaulted || t.IsCanceled)
                                    {
                                        return "ERROR";
                                    }

                                    var text = t.Result.Regions
                                                .Select(
                                                    r => string.Join("\n", r.Lines.Select(
                                                        l => string.Join(" ", l.Words.Select(w => w.Text)))));

                                    return string.Join("\n\n", text);
                                })
                               .ConfigureAwait(false);

                Device.BeginInvokeOnMainThread(() =>
                {
                    txtResult.Text = result;

                    UserDialogs.Instance.HideLoading();
                });
            }

        }

        async Task<bool> TryRequestPermision(string v1, string v2)
        {
            var result = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Camera);

            if (result != PermissionStatus.Granted)
            {
                var shouldShow = await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(Permission.Camera);

                if (shouldShow)
                {
                    await UserDialogs.Instance.AlertAsync(v1, "Request Permission", "Grant");
                }

                var permissions = await CrossPermissions.Current.RequestPermissionsAsync(Permission.Camera);

                result = permissions[Permission.Camera];
            }

            if (result != PermissionStatus.Granted)
            {
                await UserDialogs.Instance.AlertAsync(v2, "Request Permission Failed", "Dismiss");

                return false;
            }

            return true;
        }

    }
}
