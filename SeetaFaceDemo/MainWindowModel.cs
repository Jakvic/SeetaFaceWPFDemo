using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace SeetaFaceDemo;

public class MainWindowModel : ObservableObject
{
    private VideoCaptureDevice? _device;
    private FilterInfo? _selectedItem;
    private readonly FaceDetect _faceDetect;
    private readonly string _faceAsset;

    public MainWindowModel()
    {
        Refresh();

        RefreshCommand = new RelayCommand(Refresh);
        CaptureCommand = new RelayCommand(Capture);
        _faceAsset = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "FaceAsset");
        if (!Directory.Exists(_faceAsset))
        {
            Directory.CreateDirectory(_faceAsset);
        }

        _faceDetect = new(_faceAsset);
    }

    private void Capture()
    {
        if (ImageSource is null)
        {
            Message = "Image is null!";
            return;
        }

        BitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(ImageSource));
        var fileName = Path.Combine(_faceAsset, DateTime.Now.ToString("yyyy_MM_dd HH_mm_ss") + ".png");
        using var fileStream = new FileStream(fileName, FileMode.Create);
        encoder.Save(fileStream);
    }

    public FilterInfo? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                Stop();
                if (value is null)
                {
                    return;
                }

                _device = new VideoCaptureDevice(value.MonikerString);
                _device.NewFrame -= OnNewFrame;
                _device.NewFrame += OnNewFrame;
                _device.Start();
            }
        }
    }

    private BitmapImage? _imageSource;

    public BitmapImage? ImageSource
    {
        get => _imageSource;
        set => SetProperty(ref _imageSource, value);
    }

    public ObservableCollection<FilterInfo> Items { get; set; } = new();

    private string? _message;

    public string? Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public RelayCommand RefreshCommand { get; set; }
    public RelayCommand CaptureCommand { get; set; }
    public event EventHandler? OnCloseRequestEvent;

    protected virtual void OnCloseRequest()
    {
        OnCloseRequestEvent.Invoke(this, EventArgs.Empty);
    }

    private void OnNewFrame(object sender, NewFrameEventArgs args)
    {
        try
        {
            var img = (Bitmap)args.Frame.Clone();
            var ms = new MemoryStream();
            img.Save(ms, ImageFormat.Bmp);
            ms.Seek(0, SeekOrigin.Begin);
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.EndInit();

            bi.Freeze();
            ImageSource = bi;

            Message = _faceDetect.IsMatch(ms.GetBuffer(), out _) ? " SUCCESS!" : "FAILED!";
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private void Refresh()
    {
        var filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);

        if (filterInfoCollection == null)
        {
            throw new ArgumentNullException(nameof(filterInfoCollection));
        }

        Stop();
        Items.Clear();
        foreach (FilterInfo o in filterInfoCollection)
        {
            Items.Add(o);
        }

        SelectedItem = Items.FirstOrDefault();
    }

    public void Stop()
    {
        try
        {
            if (_device is null)
            {
                return;
            }

            _device.SignalToStop();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}