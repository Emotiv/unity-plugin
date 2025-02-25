using System;
using Newtonsoft.Json.Linq;

namespace EmotivUnityPlugin
{
    public struct MentalStateModel
    {
        public int recordingTime;
        public float[] percent;

        public MentalStateModel(JObject jsonObject)
        {
            recordingTime = (int)jsonObject["recordingTime"];
            percent = jsonObject["percent"].ToObject<float[]>();
        }
    }
}