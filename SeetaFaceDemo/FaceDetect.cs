using System;
using System.IO;
using SeetaFace6Sharp;
using SkiaSharp;

namespace SeetaFaceDemo;

public class FaceDetect
{
    private readonly DirectoryInfo _directoryInfo;

    public FaceDetect(string path)
    {
        _directoryInfo = new DirectoryInfo(path);
    }

    public bool IsMatch(byte[] buffer, out string? matchedFileName)
    {
        try
        {
            var enumerable = _directoryInfo.EnumerateFiles();
            foreach (var file in enumerable)
            {
                using var source = SKBitmap.Decode(file.FullName).ToFaceImage();
                var ms = new MemoryStream(buffer);
                using var dst = SKBitmap.Decode(ms).ToFaceImage();
                var r = FaceRecognizer(source, dst);
                if (!r)
                {
                    continue;
                }

                matchedFileName = file.Name;
                return true;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            ViewFaceInnerLog();
        }

        matchedFileName = null;
        return false;
    }

    public bool HasFace(byte[] buffer)
    {
        using var source = SKBitmap.Decode(buffer).ToFaceImage();
        using var faceDetector = new FaceDetector();
        var info = faceDetector.Detect(source);
        return info is not null && info.Length >= 1;
    }

    public void ViewFaceInnerLog()
    {
        GlobalConfig.SetLog((msg) => { Console.WriteLine("ViewFaceLog:" + msg); });
    }

    private bool FaceRecognizer(FaceImage source, FaceImage dst)
    {
        try
        {
            //检测人脸信息
            using var faceDetector = new FaceDetector();
            var infos0 = faceDetector.Detect(source);
            var infos1 = faceDetector.Detect(dst);
            //标记人脸位置
            using var faceMark = new FaceLandmarker();

            if (infos0 is null || infos1 is null)
            {
                return false;
            }

            if (infos0.Length < 1 || infos1.Length < 1)
            {
                return false;
            }

            var points0 = faceMark.Mark(source, infos0[0]);
            var points1 = faceMark.Mark(dst, infos1[0]);

            //提取特征值
            using var faceRecognizer = new FaceRecognizer();

            var data0 = faceRecognizer.Extract(source, points0);
            var data1 = faceRecognizer.Extract(dst, points1);
            //对比特征值
            return faceRecognizer.IsSelf(data0, data1);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            ViewFaceInnerLog();
        }

        return false;
    }
}