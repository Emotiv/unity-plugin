using System;
using Newtonsoft.Json.Linq;

namespace EmotivUnityPlugin
{
    public struct MentalStateModel
    {
        public float totalDuration;
        public float overload; // duration of overload (burnout) state
        public float disengaged;
        public float flow;
        public float intense;
        public float moderate;
        public float optimal;

        public MentalStateModel(JObject jsonObject)
        {
            if (jsonObject == null || jsonObject.Count == 0)
            {
                totalDuration = 0;
                overload = 0;
                disengaged = 0;
                flow = 0;
                intense = 0;
                moderate = 0;
                optimal = 0;
                return;
            }

            totalDuration = (float)jsonObject["totalDuration"];
            overload = 0;
            disengaged = 0;
            flow = 0;
            intense = 0;
            moderate = 0;
            optimal = 0;

            JArray states = (JArray)jsonObject["states"];
            foreach (JObject state in states)
            {
                string stateName = (string)state["state"];
                float duration = (float)state["duration"];

                switch (stateName)
                {
                    case "burnout":
                        overload = duration;
                        break;
                    case "disengaged":
                        disengaged = duration;
                        break;
                    case "flow":
                        flow = duration;
                        break;
                    case "intense":
                        intense = duration;
                        break;
                    case "moderate":
                        moderate = duration;
                        break;
                    case "optimal":
                        optimal = duration;
                        break;
                }
            }
        }

        // create to string
        public string ToString()
        {
            return "TotalDuration: " + totalDuration + "\n" +
                "Overload: " + overload + "\n" +
                "Disengaged: " + disengaged + "\n" +
                "Flow: " + flow + "\n" +
                "Intense: " + intense + "\n" +
                "Moderate: " + moderate + "\n" +
                "Optimal: " + optimal + "\n";
        }

        public float[] GetPercentages()
        {
            float[] percentages = new float[6];
            if (totalDuration > 0)
            {
                percentages[0] = (disengaged / totalDuration);
                percentages[1] = (moderate / totalDuration);
                percentages[2] = (flow / totalDuration);
                percentages[3] = (optimal / totalDuration);
                percentages[4] = (intense / totalDuration);
                percentages[5] = (overload / totalDuration);

                // Round to two decimal places
                for (int i = 0; i < percentages.Length; i++)
                {
                    percentages[i] = (float)Math.Round(percentages[i], 2);
                }

                // Adjust to ensure the total is 1.0
                float total = 0;
                for (int i = 0; i < percentages.Length; i++)
                {
                    total += percentages[i];
                }

                if (total != 1.0f)
                {
                    float difference = 1.0f - total;
                    percentages[0] += difference; // Adjust the first element to make the total 1.0
                }
            }
            return percentages;
        }
    }
}
