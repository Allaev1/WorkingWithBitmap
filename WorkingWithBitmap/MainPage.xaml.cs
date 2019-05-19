using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media.Imaging;

namespace WorkingWithBitmap
{
    public sealed partial class MainPage : Page
    {
        #region Declarations
        WriteableBitmap writableBitmap; //Выбранная фотография
        WriteableBitmap editedBitmap; //Выбранная фотография с изменёными длиной и шириной
        StorageFile imageFile; //Содержит выбранную фотографию 
        #endregion

        #region Constructors
        public MainPage()
        {
            this.InitializeComponent();
        }
        #endregion

        #region Events handlers
        private async void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            // получаем файл фотографии
            if ((imageFile = await PickImage()) == null) return;

            using (var stream = await imageFile.OpenStreamForReadAsync())
            {
                writableBitmap = await BitmapFactory.FromStream(stream);

                ImageHolderEx.Source = writableBitmap;
            }

            X_Slider.Maximum = writableBitmap.PixelWidth;
            Y_Slider.Maximum = writableBitmap.PixelHeight;
        }

        private async void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            using (var stream = await imageFile.OpenStreamForReadAsync())
            {
                WriteableBitmap writableBitmap = await BitmapFactory.FromStream(stream);

                ImageHolderEx.Source = writableBitmap;
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            StorageFile fileToReplace = await GetImageInFile(editedBitmap);

            await imageFile.CopyAndReplaceAsync(fileToReplace);
        }

        private async void BtnSaveAs_Click(object sender, RoutedEventArgs e)
        { 
            StorageFile newFileImage = await GetImageInFile(editedBitmap);

            await SaveFile(newFileImage);
        }

        private void X_Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (writableBitmap == null) return;

            Slider xSlider = sender as Slider;

            int newWidth = Convert.ToInt16(xSlider.Value);

            using (writableBitmap.GetBitmapContext(ReadWriteMode.ReadWrite))
            {
                editedBitmap = writableBitmap.Resize(newWidth, writableBitmap.PixelHeight, WriteableBitmapExtensions.Interpolation.NearestNeighbor);
            }

            ImageHolderEx.Source = editedBitmap;
        }

        private void Y_Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (writableBitmap == null) return;

            Slider ySlider = sender as Slider;

            int newHeight = Convert.ToInt16(ySlider.Value);

            using (writableBitmap.GetBitmapContext(ReadWriteMode.ReadWrite))
            {
                editedBitmap = writableBitmap.Resize(writableBitmap.PixelWidth, newHeight, WriteableBitmapExtensions.Interpolation.Bilinear);
            }

            ImageHolderEx.Source = editedBitmap;
        }
        #endregion

        #region private functions
        private async Task<StorageFile> PickImage()
        {
            FileOpenPicker picker = new FileOpenPicker();

            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            return await picker.PickSingleFileAsync();
        }

        /// <summary>
        /// Возвращает файл в который записывается фотография
        /// </summary>
        /// <param name="bitmapForSave">
        /// Фотография которую нужно записать 
        /// </param>
        /// <returns>
        /// Файл с записанной в ней фотографией
        /// </returns>
        private async Task<StorageFile> GetImageInFile(WriteableBitmap bitmapForSave)
        {
            StorageFile imageFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(Guid.NewGuid().ToString());

            //Открываем поток файла в который мы хотим записать изменённую фотографию
            //https://docs.microsoft.com/en-gb/windows/uwp/audio-video-camera/imaging#save-a-softwarebitmap-to-a-file-with-bitmapencoder
            using (IRandomAccessStream stream = await imageFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

                //Создаем экземпляр на основе writableBitmap
                //https://docs.microsoft.com/en-gb/windows/uwp/audio-video-camera/imaging#create-a-softwarebitmap-from-a-writeablebitmap
                SoftwareBitmap softwareBitmap = SoftwareBitmap.CreateCopyFromBuffer
                    (bitmapForSave.PixelBuffer,
                    BitmapPixelFormat.Bgra8,
                    bitmapForSave.PixelWidth,
                    bitmapForSave.PixelHeight,
                    BitmapAlphaMode.Ignore);

                //encoder.SetSoftwareBitmap(softwareBitmap);
                Stream pixelStream = bitmapForSave.PixelBuffer.AsStream();
                byte[] pixels = new byte[pixelStream.Length];
                await pixelStream.ReadAsync(pixels, 0, pixels.Length);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)bitmapForSave.PixelWidth, (uint)bitmapForSave.PixelHeight, 96.0, 96.0, pixels);

                await encoder.FlushAsync();
            }

            return imageFile;
        }

        /// <summary>
        /// Сохраняет файл через FileSavePicker
        /// </summary>
        /// <param name="fileForSave">
        /// Файл который нужно сохранить
        /// </param>
        private async Task SaveFile(StorageFile fileForSave)
        {
            FileSavePicker fileSavePicker = new FileSavePicker();
            fileSavePicker.SuggestedSaveFile = fileForSave;
            fileSavePicker.FileTypeChoices.Add("", new List<string>() { ".jpg", ".jpeg", ".png" });
            fileSavePicker.SuggestedStartLocation = PickerLocationId.Desktop;
            fileSavePicker.SuggestedFileName = "hello";

            await fileSavePicker.PickSaveFileAsync();
        }
        #endregion

    }
}
