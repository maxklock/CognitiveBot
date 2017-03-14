namespace CognitiveBot
{
    using System.Linq;
    using System.Text;

    using Microsoft.ProjectOxford.Emotion.Contract;
    using Microsoft.ProjectOxford.Vision.Contract;

    using Face = Microsoft.ProjectOxford.Face.Contract.Face;

    public static class MessageCreator
    {
        #region methods

        public static string GetEmotionsText(Emotion[] emotions)
        {
            switch (emotions.Length)
            {
                case 0:
                    return $"I can't recognize a face. Try a new image.";
                case 1:
                    return GetEmotionText(emotions[0]).Replace("\n", "  \n");
                default:
                    var builder = new StringBuilder();
                    builder.AppendLine("From left to right:");
                    var i = 0;
                    foreach (var emotion in emotions.OrderBy(f => f.FaceRectangle.Left))
                    {
                        builder.AppendLine($"Face {++i}");
                        builder.AppendLine(GetEmotionText(emotion));
                    }
                    return builder.ToString().Replace("\n", "  \n");
            }
        }

        public static string GetFacesCountText(Face[] faces)
        {
            switch (faces.Length)
            {
                case 0:
                    return "I think there are no persons.";
                case 1:
                    return "I think there is one person.";
                default:
                    return $"I think there are {faces.Length} persons.";
            }
        }

        public static string GetFacesText(Face[] faces)
        {
            switch (faces.Length)
            {
                case 0:
                    return $"I can't recognize a face. Try a new image.";
                case 1:
                    return GetFaceText(faces[0]);
                default:
                    var builder = new StringBuilder();
                    builder.AppendLine("From left to right:");
                    foreach (var face in faces.OrderBy(f => f.FaceRectangle.Left))
                    {
                        builder.AppendLine(GetFaceText(face));
                    }
                    return builder.ToString().Replace("\n", "  \n");
            }
        }

        public static string GetImageDescription(AnalysisResult result)
        {
            var builder = new StringBuilder();
            if (result.Description.Captions.Length == 0)
            {
                return "I'm not able to create a description.";
            }
            foreach (var caption in result.Description.Captions.OrderByDescending(c => c.Confidence))
            {
                builder.AppendLine(caption.Text);
                builder.AppendLine("Or");
            }
            builder.Remove(builder.Length - 4, 3);
            return "    " + builder.ToString().Replace("\n", "  \n    ");
        }

        public static string GetImageText(OcrResults result)
        {
            var builder = new StringBuilder();
            if (result.Regions.Length == 0 || result.Regions.All(r => r.Lines.Length == 0))
            {
                return "I found no text on the image.";
            }
            foreach (var region in result.Regions)
            {
                builder.AppendLine(
                    region.Lines.Aggregate(string.Empty, (res, line) => res + line.Words.Aggregate(string.Empty, (sentance, word) => sentance + " " + word.Text).Substring(1) + "\n") + "\n");
            }
            return "    " + builder.ToString().Replace("\n", "  \n    ");
        }

        private static string GetEmotionText(Emotion emotion, int maxScores = 5)
        {
            var builder = new StringBuilder();
            var scores = emotion.Scores.GetType().GetProperties().Select(
                info => new
                {
                    info.Name,
                    Value = (float)info.GetValue(emotion.Scores)
                }).Where(score => score.Value > 0.005f).OrderByDescending(score => score.Value).Take(maxScores);
            foreach (var score in scores)
            {
                builder.AppendLine($"`{score.Name} ({score.Value:0.00})`\n");
            }
            return builder.ToString();
        }

        private static string GetFaceText(Face face)
        {
            string gender;
            switch (face.FaceAttributes.Gender)
            {
                case "male":
                    gender = "man";
                    break;
                case "female":
                    gender = "woman";
                    break;
                default:
                    gender = "person";
                    break;
            }
            return $"`A {face.FaceAttributes.Age} years old {gender}`";
        }

        #endregion
    }
}