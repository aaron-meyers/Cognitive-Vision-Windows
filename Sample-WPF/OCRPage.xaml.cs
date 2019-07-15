//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
//
// Microsoft Cognitive Services (formerly Project Oxford): https://www.microsoft.com/cognitive-services
//
// Microsoft Cognitive Services (formerly Project Oxford) GitHub:
// https://github.com/Microsoft/Cognitive-Vision-Windows
//
// Copyright (c) Microsoft Corporation
// All rights reserved.
//
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

// -----------------------------------------------------------------------
// KEY SAMPLE CODE STARTS HERE
// Use the following namespace for ComputerVisionClient.
// -----------------------------------------------------------------------
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
// -----------------------------------------------------------------------
// KEY SAMPLE CODE ENDS HERE
// -----------------------------------------------------------------------

namespace VisionAPI_WPF_Samples
{
    /// <summary>
    /// Interaction logic for OCRPage.xaml.
    /// </summary>
    public partial class OCRPage : ImageScenarioPage
    {
        const bool DetectOrientation = true;

        public OCRPage()
        {
            InitializeComponent();
            this.PreviewImage = _imagePreview;
            this.URLTextBox = _urlTextBox;
            this.languageComboBox.ItemsSource = RecognizeLanguage.SupportedForOCR;
        }

        /// <summary>
        /// Uploads the image to Cognitive Services and performs OCR.
        /// </summary>
        /// <param name="imageFilePath">The image file path.</param>
        /// <param name="language">The language code to recognize.</param>
        /// <returns>Awaitable OCR result.</returns>
        private async Task<OcrResult> UploadAndRecognizeImageAsync(string imageFilePath, OcrLanguages language)
        {
            // -----------------------------------------------------------------------
            // KEY SAMPLE CODE STARTS HERE
            // -----------------------------------------------------------------------

            //
            // Create Cognitive Services Vision API Service client.
            //
            using (var client = new ComputerVisionClient(Credentials) { Endpoint = Endpoint })
            {
                Log("ComputerVisionClient is created");

                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    //
                    // Upload an image and perform OCR.
                    //
                    Log("Calling ComputerVisionClient.RecognizePrintedTextInStreamAsync()...");
                    OcrResult ocrResult = await client.RecognizePrintedTextInStreamAsync(DetectOrientation, imageFileStream, language);
                    return ocrResult;
                }
            }

            // -----------------------------------------------------------------------
            // KEY SAMPLE CODE ENDS HERE
            // -----------------------------------------------------------------------
        }

        /// <summary>
        /// Sends a URL to Cognitive Services and performs OCR.
        /// </summary>
        /// <param name="imageUrl">The image URL for which to perform recognition.</param>
        /// <param name="language">The language code to recognize.</param>
        /// <returns>Awaitable OCR result.</returns>
        private async Task<OcrResult> RecognizeUrlAsync(string imageUrl, OcrLanguages language)
        {
            // -----------------------------------------------------------------------
            // KEY SAMPLE CODE STARTS HERE
            // -----------------------------------------------------------------------

            //
            // Create Cognitive Services Vision API Service client.
            //
            using (var client = new ComputerVisionClient(Credentials) { Endpoint = Endpoint })
            {
                Log("ComputerVisionClient is created");

                //
                // Perform OCR on the given URL.
                //
                Log("Calling ComputerVisionClient.RecognizePrintedTextAsync()...");
                OcrResult ocrResult = await client.RecognizePrintedTextAsync(DetectOrientation, imageUrl, language);
                return ocrResult;
            }

            // -----------------------------------------------------------------------
            // KEY SAMPLE CODE ENDS HERE
            // -----------------------------------------------------------------------
        }

        /// <summary>
        /// Perform the work for this scenario.
        /// </summary>
        /// <param name="imageUri">The URI of the image to run against the scenario.</param>
        /// <param name="upload">Upload the image to Cognitive Services if [true]; submit the Uri as a remote URL if [false].</param>
        /// <returns>Awaitable OCR result.</returns>
        protected override async Task DoWorkAsync(Uri imageUri, bool upload)
        {
            _status.Text = "Performing OCR...";

            OcrLanguages languageCode = (languageComboBox.SelectedItem as RecognizeLanguage).OcrEnum;

            //
            // Either upload an image, or supply a URL.
            //
            OcrResult ocrResult;
            if (upload)
            {
                ocrResult = await UploadAndRecognizeImageAsync(imageUri.LocalPath, languageCode);
            }
            else
            {
                ocrResult = await RecognizeUrlAsync(imageUri.AbsoluteUri, languageCode);
            }
            _status.Text = "OCR Done";

            //
            // Log analysis result in the log window.
            //
            Log("");
            Log("OCR Result:");
            LogOcrResults(ocrResult);

            if (ocrResult != null && ocrResult.Regions != null)
            {
                _imageCanvas.Height = ocrResult.Regions.Max(r =>
                {
                    var rect = ToBoundingRect(r.BoundingBox);
                    return rect.Y + rect.H;
                });

                foreach (var item in ocrResult.Regions)
                {
                    AddRectangle(item.BoundingBox, Brushes.Red);

                    foreach (var line in item.Lines)
                    {
                        AddRectangle(line.BoundingBox, Brushes.Green);

                        foreach (var word in line.Words)
                        {
                            AddRectangle(word.BoundingBox, Brushes.Blue);

                            var bounds = ToBoundingRect(word.BoundingBox);
                            var textBlock = new TextBlock
                            {
                                Width = bounds.W,
                                Height = bounds.H,
                                Foreground = Brushes.Blue,
                                Text = word.Text,
                                FontSize = 22,
                            };
                            Canvas.SetLeft(textBlock, bounds.X + 36);
                            Canvas.SetTop(textBlock, bounds.Y + 2);
                            _imageCanvas.Children.Add(textBlock);
                        }
                    }
                }
            }
        }

        private void AddRectangle(string boundingBox, Brush strokeBrush)
        {
            var bounds = ToBoundingRect(boundingBox);
            var rect = new Rectangle
            {
                Width = bounds.W,
                Height = bounds.H,
                Stroke = strokeBrush,
                StrokeThickness = 1,
            };
            _imageCanvas.Children.Add(rect);
            Canvas.SetLeft(rect, bounds.X);
            Canvas.SetTop(rect, bounds.Y);
        }

        private static BoundingRect ToBoundingRect(string s)
        {
            var a = s.Split(',').Select(v => int.Parse(v)).ToArray();
            return new BoundingRect(a[0], a[1], a[2], a[3]);
        }
    }
}
