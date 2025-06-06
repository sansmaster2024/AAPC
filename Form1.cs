namespace WinFormsApp1
{
    using GDMiniJSON;
    using System;
    using System.CodeDom;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading;
    using static System.Net.Mime.MediaTypeNames;
    using static System.Net.WebRequestMethods;
    using File = File;



    public partial class Form1 : Form
    {

        public void leaveWithComment(String comment)
        {
            Console.Write(comment);
            Console.ReadLine();
            Environment.Exit(0);
        }
        public static string ReadAllText(string path, Encoding encoding = null)
        {
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }
            return File.ReadAllText(path, encoding);
        }
        public float int32ToFloat(object a)
        {
            return float.Parse(a.ToString());
        }
        public double int32ToDouble(object a)
        {
            return Double.Parse(a.ToString());
        }
        public decimal int32ToDecimal(object a)
        {
            return decimal.Parse(a.ToString());
        }
        public void makeResult(string origin, string text)
        {
            File.WriteAllText(origin.Replace(".adofai", "_pathData.adofai"), text);
        }
        string reservedMessage = "";
        public void ReserveMessage(string message)
        {
            reservedMessage+=message+"\n";
        }
        public Form1()
        {

            //InitializeComponent();
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;
            openFileDialog.Title = "pathData adofai";
            string path = "";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                path = openFileDialog.FileName;
            }
            else
            {
                leaveWithComment("파일 위치를 지정하십시오.");
                return;
            }
            Console.Write("bpm 변경 반올림 자리 수(높을 수록 싱크가 잘맞습니다) :");
            int maxAcc = 3;
            try
            {
                maxAcc = (int)Double.Parse(Console.ReadLine());
                if (maxAcc > 50)
                {
                    maxAcc = 5;
                    ReserveMessage("반올림 자리 수가 50을 초과하여 5로 수정되었습니다.");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                ReserveMessage("반올림 자리 수를 인식할 수 없어 기본값 3으로 수정되었습니다.");

            }
            maxAcc = (int)Math.Pow(10, maxAcc);
            

            var json = Json.Deserialize(ReadAllText(path)) as Dictionary<string, object>;
            if (json.ContainsKey("pathData"))
            {
                leaveWithComment("해당 맵은 이미 pathData 맵입니다.");
            }
            var actions = (List<object>)json["actions"];
            decimal curBpm = int32ToDecimal((((Dictionary<string, object>)json["settings"])["bpm"]));
            decimal save = curBpm;
            decimal offset = 0;
            Dictionary<int, decimal> bpmChangeMap = new Dictionary<int, decimal>();
            List<int> twirlChangeMap = new List<int>();
            for (int i = 0; i < actions.Count; i++)
            {
                Dictionary<string, object> action = (Dictionary<string, object>)actions[i];
                string eventType = (string)action["eventType"];
                if (eventType == "SetSpeed")
                {
                    if (action["speedType"].ToString().ToLower().Trim() == "Multiplier".ToLower()) curBpm *= int32ToDecimal(action["bpmMultiplier"]);
                    else curBpm = int32ToDecimal(action["beatsPerMinute"]);

                    bpmChangeMap.Add((int)action["floor"], curBpm);

                }
                else if (eventType == "Twirl")
                {
                    twirlChangeMap.Add((int)action["floor"]);
                }
            }

            for (int i = actions.Count - 1; i >= 0; i--)
            {
                Dictionary<string, object> action = (Dictionary<string, object>)actions[i];
                string eventType = (string)action["eventType"];
                if (eventType == "SetSpeed")
                {
                    actions.RemoveAt(i);
                }

            }

            List<object> angles = new List<object>();
            angles.Add(0);
            angles.AddRange((List<object>)json["angleData"]);
            List<object> safeAngles = new List<object>();
            safeAngles.Add(0);
            safeAngles.AddRange((List<object>)json["angleData"]);
            decimal[] bpms = new decimal[angles.Count];
            bool[] twirls = new bool[angles.Count];
            curBpm = save;
            //decimal first = fixAngle(int32ToDecimal(angles[0]));
            //decimal angleFirst = getNearestAngle(first);
            //angles[0] = angleFirst;

            //offset = ((first / 180) * (60 / curBpm) - ((angleFirst / 180) * (60 / curBpm)))*1000000000;



            bool curTwirl = true;
            for (int i = 0; i < angles.Count; i++)
            {
                if (bpmChangeMap.ContainsKey(i))
                {
                    curBpm = bpmChangeMap[i];
                }

                bpms[i] = curBpm;

                if (twirlChangeMap.Contains(i))
                {
                    curTwirl = !curTwirl;
                }
                twirls[i] = curTwirl;
            }
            curBpm = bpms[0];
            string map = "";
            Dictionary<int, decimal> newBpms = new Dictionary<int, decimal>();
            List<decimal> newAngles = new List<decimal>();
            decimal lastBpm = curBpm;
            for (int i = 0; i < angles.Count - 1; i++)
            {
                decimal now = fixAngle(int32ToDecimal(angles[i]));
                decimal Anow = fixAngle(int32ToDecimal(safeAngles[i]));

                decimal next = now;
                decimal Anext = Anow;
                if (i + 1 < angles.Count)
                {
                    next = fixAngle(int32ToDecimal(angles[i + 1]));
                    Anext = fixAngle(int32ToDecimal(safeAngles[i + 1]));
                }
                bool isMidspin = next == 999;
                if (now == 999) continue;
                if (isMidspin)
                {
                    i++;
                    next = now;
                    Anext = Anow;
                    if (i + 1 < angles.Count)
                    {
                        next = fixAngle(int32ToDecimal(angles[i + 1]));
                        Anext = fixAngle(int32ToDecimal(safeAngles[i + 1]));
                    }
                }

                decimal angle = getCurrentAngle(now, next, twirls[i], isMidspin);
                decimal Aangle = getCurrentAngle(Anow, Anext, twirls[i], isMidspin);

                decimal time = (Aangle / 180) * (60 / bpms[i]) * 1000000000 + offset;


                decimal fixedAngle = getNearestAngle(angle);

                if (angle != 360 && (fixedAngle == 360)) fixedAngle = 345;
                if (angle != 0 && (fixedAngle == 0)) fixedAngle = 15;

                curBpm = (decimal)Math.Round(timeAngleToBpm(time / 1000000000, fixedAngle)*maxAcc)/maxAcc;



                if (isMidspin) map += "!";
                int b = (int)fixAngle(getArcCurrentAngle(now, twirls[i], isMidspin, fixedAngle));



                if (angles.Count > i + 1) angles[i + 1] = (double)b;
                var a = convertToChar(b);
                if (a == "SANS")
                {
                    leaveWithComment(now.ToString() + " : " + fixedAngle.ToString());
                }
                map += a;
                if (curBpm != lastBpm)
                {
                    lastBpm = curBpm;
                    newBpms.Add(i, curBpm);
                }


                offset = time - (fixedAngle / 180) * (60 / curBpm) * 1000000000;
                if (angle != fixedAngle)
                {
                    Console.WriteLine(i.ToString() + " 오차 : " + angle.ToString() + " : " + fixedAngle.ToString() + " : " + (offset).ToString());
                }








            }
            var e = newBpms.Keys.ToList<int>();
            for (int i = 0; i < e.Count; i++)
            {
                Dictionary<string, object> d = new Dictionary<string, object>();
                d.Add("floor", e[i]);
                d.Add("eventType", "SetSpeed");
                d.Add("beatsPerMinute", newBpms[e[i]]);
                actions.Add(d);
            }
            json.Remove("angleData");
            json.Add("pathData", map);

            ((Dictionary<string, object>)json["settings"])["legacySpriteTiles"] = true;
            json["actions"] = actions;
            makeResult(path, Json.Serialize(json)); ;
            Console.WriteLine("예약된 메시지 : "+reservedMessage);
            leaveWithComment("와!");
        }
        public static decimal timeAngleToBpm(decimal time, decimal angle)
        {
            //double time = (angle/180) *(60/bpm)

            return (angle / 3) / time;
        }
        public static string convertToChar(int angle)
        {
            switch (angle)
            {
                case 0:
                    return "R";

                case 45:
                    return "E";

                case 60:
                    return "T";

                case 90:
                    return "U";

                case 135:
                    return "Q";

                case 150:
                    return "H";

                case 180:
                    return "L";

                case 240:
                    return "F";

                case 270:
                    return "D";

                case 225:
                    return "Z";

                case 330:
                    return "M";

                case 315:
                    return "C";

                case 30:
                    return "J";

                case 120:
                    return "G";

                case 210:
                    return "N";

                case 300:
                    return "B";

                case 15:
                    return "p";

                case 75:
                    return "o";

                case 105:
                    return "q";

                case 165:
                    return "W";

                case 195:
                    return "x";

                case 255:
                    return "V";

                case 285:
                    return "Y";

                case 345:
                    return "A";
                case 999:
                    return "!";
                case -999:
                    return "!";

            }
            Console.WriteLine("error : " + angle.ToString());
            return "SANS";

        }
        public static decimal getCurrentAngle(decimal thisTile, decimal nextTile, Boolean isTwirl, Boolean isMidspin)
        {
            decimal angle = (nextTile - thisTile);
            angle += (isMidspin) ? 360 : 540;
            angle %= 360;
            if (isTwirl) angle = 360 - angle;
            if (angle == 0) angle = 360;
            return angle;
        }
        public static decimal getArcCurrentAngle(decimal thisTile, Boolean isTwirl, Boolean isMidspin, decimal currentAngle)
        {
            if (currentAngle == 360) currentAngle = 0;
            if (isTwirl) currentAngle = 360 - currentAngle;
            currentAngle -= (isMidspin) ? 360 : 540;

            return (currentAngle + thisTile) % 360;
        }
        public static decimal getNearestAngle(decimal angle)
        {
            return (decimal)Math.Round(fixAngle(angle) / 15) * 15;
        }
        /*public static int getNearestAngle(decimal thisTile, double nextTile, Boolean isTwirl, Boolean isMidspin)
        {
            if (nextTile == 999) return 999;

            double  originalAngle = fixAngle(getCurrentAngle(thisTile, nextTile, isTwirl, isMidspin));
            double distance = -1;
            int value = -1;
            double localDistance = 0;
            for (int i = 0; i < 360; i += 15)
            {
                localDistance = Math.Abs(originalAngle - fixAngle(getCurrentAngle(thisTile, i, isTwirl, isMidspin)));
                if (distance == -1)
                {
                    distance = localDistance;
                    value = i;
                    continue;
                }
                else
                {
                    if (localDistance < distance)
                    {
                        distance = localDistance;
                        value = i;
                        continue;
                    }
                }
            }
            



            return value;
        }*/
        public static decimal fixAngle(decimal raw)
        {
            if (Math.Abs(raw) == 999) return 999;
            if (Math.Abs(raw) > 360)
            {
                raw = raw % 360;
            }
            if (raw < 0)
            {
                return 360 + raw;
            }

            return raw;
        }
        public static double bpmTimeToAngle(double bpm, double time)
        {
            return time / 1000000000 * (3 * bpm);
        }



    }

}