namespace CognitiveBot
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Runtime.InteropServices.ComTypes;
    using System.Threading.Tasks;

    using Microsoft.ProjectOxford.Emotion;
    using Microsoft.ProjectOxford.Emotion.Contract;
    using Microsoft.ProjectOxford.Face;
    using Microsoft.ProjectOxford.Vision;
    using Microsoft.ProjectOxford.Vision.Contract;

    using Face = Microsoft.ProjectOxford.Face.Contract.Face;

    public class CognitiveServiceHelper
    {
        #region constants

        private static readonly Lazy<IFaceServiceClient> FaceServiceFactory = new Lazy<IFaceServiceClient>(() => new FaceServiceClient(ConfigurationManager.AppSettings["FaceApi"]));

        private static readonly Lazy<IVisionServiceClient> VisionServiceFactory = new Lazy<IVisionServiceClient>(() => new VisionServiceClient(ConfigurationManager.AppSettings["VisionApi"]));

        private static readonly Lazy<EmotionServiceClient> EmotionServiceFactory = new Lazy<EmotionServiceClient>(() => new EmotionServiceClient(ConfigurationManager.AppSettings["EmotionApi"]));

        #endregion

        #region properties

        public static IFaceServiceClient FaceService => FaceServiceFactory.Value;

        public static IVisionServiceClient VisionService => VisionServiceFactory.Value;

        public static EmotionServiceClient EmotionService => EmotionServiceFactory.Value;

        #endregion

        public static async Task<Emotion[]> RecognizeEmotionsAsync(string imageUrl)
        {
            return await EmotionService.RecognizeAsync(imageUrl);
        }

        public static async Task<Face[]> DetectFacesAsync(string imageUrl, bool returnId, bool returnLandmarks, params FaceAttributeType[] attributes)
        {
            return await FaceService.DetectAsync(imageUrl, returnId, returnLandmarks, attributes);
        }

        public static async Task<AnalysisResult> AnalyzeImageAsync(string imageUrl)
        {
            return await VisionService.DescribeAsync(imageUrl, 2);
        }

        public static async Task<OcrResults> RecognizeImageTextAsync(string imageUrl)
        {
            return await VisionService.RecognizeTextAsync(imageUrl);
        }
    }
}