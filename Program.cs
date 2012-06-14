using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using CommandLine;
using CommandLine.Text;
using Amazon.Runtime;

namespace windows_s3_uploader
{
  class CommandLineOptions
  {
    [Option(null, "bucket", Required = true, HelpText = "The bucket where the file will be uploaded")]
    public string Bucket { get; set; }

    [Option(null, "file", Required = true, HelpText = "The file to upload")]
    public string File { get; set; }

    [Option(null, "key", Required = true, HelpText = "The key of the object to create in S3. %unix_timestamp% will be replaced with the unix timestamp")]
    public string Key { get; set; }

    [Option(null, "accessKey", Required = true, HelpText = "AWS Access Key")]
    public string AccessKey { get; set; }

    [Option(null, "secretKey", Required = true, HelpText = "AWS Secret Key")]
    public string SecretKey { get; set; }

    [Option(null, "public", HelpText = "Set public read acl")]
    public bool IsPublic { get; set; }

    [Option(null, "progress", HelpText = "Display upload progress")]
    public bool ShouldShowProgress { get; set; }

    [HelpOption(null, "help", HelpText = "Display this screen")]
    public string GetUsage()
    {
      var help = new HelpText("Windows S3 Uploader");
      help.Copyright = new CopyrightInfo("Palomino Labs, Inc.", 2012);
      help.AddPreOptionsLine("Command options:");
      help.AddOptions(this);
      return help;
    }
  }

  class Program
  {
    public static void Main(string[] args)
    {
      var options = new CommandLineOptions();
      var parser = new CommandLineParser();
      if (!parser.ParseArguments(args, options))
      {
        Console.WriteLine(options.GetUsage());
        Environment.Exit(1);
        return;
      }

      var timestamp = Math.Round((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);

      try
      {
        var s3Client = new AmazonS3Client(options.AccessKey, options.SecretKey);
        var transferUtility = new TransferUtility(s3Client);
        var transferRequest = new TransferUtilityUploadRequest()
          .WithBucketName(options.Bucket)
          .WithFilePath(options.File)
          .WithKey(options.Key.Replace("%unix_timestamp%", timestamp.ToString()));


        if (options.IsPublic)
        {
          transferRequest.WithCannedACL(S3CannedACL.PublicRead);
        }

        if (options.ShouldShowProgress)
        {
          Console.WriteLine();
          transferRequest.WithSubscriber(new EventHandler<UploadProgressArgs>((obj, progress) =>
          {
            Console.Write("\r{0}% complete. Uploaded {1} / {2} bytes.                    ", progress.PercentDone, progress.TransferredBytes, progress.TotalBytes);
          }));
        }

        transferUtility.Upload(transferRequest);

        Console.WriteLine();
        Console.WriteLine("Done!");
      }
      catch (Exception e)
      {
        Console.WriteLine("Error uploading: {0}", e.Message);
        Environment.Exit(1);
      }
    }

  }
}