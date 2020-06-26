using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Vision.V1;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Task8_ImageRecognition.Models;

namespace Task8_ImageRecognition.Controllers
{
    [Route("api/ImageRecognition")]
    [ApiController]
    public class ImageRecognitionController : ControllerBase
    {
        private IHostingEnvironment _env;

        public ImageRecognitionController(IHostingEnvironment env)
        {
            _env = env;
        }

        [HttpPost]
        [Route("ScanImage")]
        public string ScanImage(IFormFile file)
        {
            List<Result> resultList = new List<Result>();
            var dir = _env.ContentRootPath;
            string path = Path.Combine(dir, file.FileName);
            using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                file.CopyTo(fileStream);
            }

            // Instantiates a client
            var client = ImageAnnotatorClient.Create();
            // Load the image file into memory
            var image = Image.FromFile(path);
            // Performs label detection on the image file
            var response = client.DetectLabels(image);

            //Add all related labels to a list
            List<string> labels = new List<string>();
            foreach (var annotation in response)
            {
                if (annotation.Description != null)
                {
                    labels.Add(annotation.Description);
                }
            }

            //Add all scanned texts to a list
            List<string> texts = new List<string>();
            var response2 = client.DetectText(image);
            foreach (var text in response2)
            {
                if (text.Description != null)
                {
                    texts.Add(text.Description);
                }
            }

            Result result = new Result();
            result.Label = "";
            result.Text = "";

            //Validate if image is a receipt
            int isReceipt = 0;
            for (int i = 0; i < labels.Count(); i++)
            {
                if (labels.ElementAt(i).Contains("Receipt"))
                {
                    result.Label = "This is a receipt.";
                    isReceipt = 1;
                }
            }

            if (isReceipt == 0)
            {
                for (int j = 0; j < labels.Count(); j++)
                {
                    if (j == 0)
                    {
                        result.Label += labels.ElementAt(j);
                    }
                    else
                    {
                        result.Label += ", " + labels.ElementAt(j);
                    }

                }
            }


            //Validate if there is a total price
            int isTotal = 0;
            for (int i = 1; i < texts.Count(); i++)
            {
                if (texts.ElementAt(i).Contains("Total"))
                {
                    result.Text = "The total cost is " + texts.ElementAt(i + 1) + ".";
                    isTotal = 1;
                }
            }

            if (isTotal == 0)
            {
                for (int j = 1; j < texts.Count(); j++)
                {
                    if (j == 1)
                    {
                        result.Text += texts.ElementAt(j);
                    }
                    else
                    {
                        result.Text += ", " + texts.ElementAt(j);
                    }
                    result.Text += texts.ElementAt(j) + ", ";
                }
            }


            //Check if texts and labels are empty
            if (result.Label.Equals(""))
                result.Label = "No related labels found.";
            if (result.Text.Equals(""))
                result.Text = "No texts scanned.";

            Console.WriteLine("Result Label:" + result.Label);
            Console.WriteLine("Result Text:" + result.Text);

            string output = JsonConvert.SerializeObject(result);

            return output;
        }
    }
}